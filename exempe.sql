/* =================================================================================================
   CALCUL DGS — MONTANTS ASSURÉS / NON ASSURÉS
   Plafond légal : 100 000 EUR par holder (CA/SA) — règles spécifiques SD / SC
   -------------------------------------------------------------------------------------------------
   PRÉREQUIS : à exécuter dans la MÊME SESSION que le script de chargement daily
               (#Daily_Accounting doit exister). NE PAS insérer de GO dans ce script.
   SOURCES   : #Daily_Accounting                    (compta quotidienne agrégée)
               [OCPR_EUC].[BLUE].[TB_DGS_SRC_NEW]   (DGS mensuel : détail des holders par dossier)
   SORTIES   : #DGS_Result        grain (DOSSIER, AccountType, TypeCompteComptable)
                                  -> pour JOIN sur #Daily_Accounting
               #DGS_Holder_Audit  détail par holder (audit + comparaison mensuelle)
   -------------------------------------------------------------------------------------------------
   RÈGLES IMPLÉMENTÉES
     CA / SA : plafond 100k par HOLDER, tous dossiers confondus.
               Priorité d'allocation : comptes PERSONNELS (CA Capital, CA Interest, SA Capital,
               SA Interest) PUIS parts de comptes JOINTS (même ordre).
               Comptes joints = parts ÉGALES entre holders.
               Pro-rata à l'intérieur d'une même classe de priorité.
               Le montant assuré d'un dossier = somme des parts assurées de ses holders
               (la capacité augmente donc avec le nombre de holders).
     SD      : plafond 100k PAR DOSSIER (Capital avant Interest), SANS consommer la capacité
               100k des holders (garantie séparée).
     SC      : ratio mensuel par dossier = SUM(MT_INSURED)/SUM(MT_ELIGIBLE) sur les lignes
               ISIN <> '' (calculé sur la SOMME du dossier, pas record par record),
               réappliqué aux soldes daily du dossier.
   HYPOTHÈSES (à valider — points isolés et modifiables) :
     - 1 seul LEAD_RELATION_AFF par dossier (MAX utilisé à l'agrégation, étape 2).
     - Dossier présent en daily mais ABSENT du mensuel DGS (nouveau dossier / nouvelle
       relation affaire) -> traité comme compte PERSONNEL du LEAD comptable, donc max 100k,
       et intégré dans le calcul de capacité de ce holder (étape 3, fallback).
     - Dossier référencé dans le mensuel DGS mais SANS solde daily -> ne consomme AUCUNE
       capacité holder (le calcul part exclusivement des soldes daily).
     - Dossier SC sans lignes ISIN dans le mensuel -> fallback sur le ratio GLOBAL par type
       (Capital/Interest, tous dossiers SC confondus) ; 0 seulement si le mensuel est vide en ISIN
       pour ce type (étape 7).
     - CD_TYP_MONTANT supposé contenir 'Capital'/'Interest' (même convention que
       TypeCompteComptable daily) — sinon, ajuster le mapping dans l'étape 7.
   ================================================================================================= */

SET NOCOUNT ON;

DECLARE @CAP decimal(38,6) = 100000.0;
DECLARE @ClosingPeriod date;

IF OBJECT_ID('tempdb..#Daily_Accounting') IS NULL
BEGIN
    RAISERROR('#Daily_Accounting absente : exécuter d''abord le script de chargement daily dans la même session.', 16, 1);
    RETURN;
END;

SELECT @ClosingPeriod = MAX(DT_CLOSING_PERIOD)
FROM [OCPR_EUC].[BLUE].[TB_DGS_SRC_NEW];
-- >>> Pour forcer la période, remplacer par : SET @ClosingPeriod = 'YYYY-MM-DD';


/* ===================================================================
   0. STAGING MENSUEL DGS — une seule passe sur la table source
   =================================================================== */
IF OBJECT_ID('tempdb..#DGS_Month') IS NOT NULL DROP TABLE #DGS_Month;

SELECT C_DOSSIER, RELATION_AFF, CD_TYP_MONTANT, ISIN, MT_INSURED, MT_ELIGIBLE
INTO #DGS_Month
FROM [OCPR_EUC].[BLUE].[TB_DGS_SRC_NEW]
WHERE DT_CLOSING_PERIOD = @ClosingPeriod
  AND C_DOSSIER IS NOT NULL
  AND C_DOSSIER <> '';

CREATE CLUSTERED INDEX IX_DGS_Month ON #DGS_Month (C_DOSSIER, RELATION_AFF);


/* ===================================================================
   1. HOLDERS PAR DOSSIER
      Plusieurs RELATION_AFF pour un même C_DOSSIER = compte joint
   =================================================================== */
IF OBJECT_ID('tempdb..#Holders') IS NOT NULL DROP TABLE #Holders;

SELECT C_DOSSIER AS DOSSIER,
       RELATION_AFF,
       COUNT(*) OVER (PARTITION BY C_DOSSIER) AS NB_HOLDERS
INTO #Holders
FROM (
    SELECT DISTINCT C_DOSSIER, RELATION_AFF
    FROM #DGS_Month
    WHERE RELATION_AFF IS NOT NULL
      AND RELATION_AFF <> ''
) d;

CREATE CLUSTERED INDEX IX_Holders ON #Holders (DOSSIER, RELATION_AFF);


/* ===================================================================
   2. SOLDES DAILY AGRÉGÉS AU GRAIN DE CALCUL
   =================================================================== */
IF OBJECT_ID('tempdb..#Balances') IS NOT NULL DROP TABLE #Balances;

SELECT DOSSIER,
       AccountType,
       TypeCompteComptable,
       SUM(DAILY_BALANCE_EUR) AS BALANCE_EUR,
       MAX(LEAD_RELATION_AFF) AS LEAD_RELATION_AFF
INTO #Balances
FROM #Daily_Accounting
GROUP BY DOSSIER, AccountType, TypeCompteComptable
OPTION (MAXDOP 8);

CREATE CLUSTERED INDEX IX_Balances ON #Balances (DOSSIER, AccountType, TypeCompteComptable);


/* ===================================================================
   3. CA / SA — VENTILATION PAR HOLDER
      perso : 100 % du solde au holder | joint : solde / NB_HOLDERS
      PRIO  : 1..4 = perso (CA Cap, CA Int, SA Cap, SA Int)
              5..8 = joint (même ordre)
   =================================================================== */
IF OBJECT_ID('tempdb..#Comp') IS NOT NULL DROP TABLE #Comp;

SELECT h.RELATION_AFF,
       b.DOSSIER,
       b.AccountType,
       b.TypeCompteComptable,
       CAST(b.BALANCE_EUR / h.NB_HOLDERS AS decimal(38,6)) AS SHARE_EUR,
       CASE WHEN h.NB_HOLDERS > 1 THEN 1 ELSE 0 END AS IS_JOINT,
       CASE WHEN h.NB_HOLDERS > 1 THEN 4 ELSE 0 END
         + CASE b.AccountType WHEN 'CA' THEN 0 ELSE 2 END
         + CASE b.TypeCompteComptable WHEN 'Capital' THEN 1 ELSE 2 END AS PRIO
INTO #Comp
FROM #Balances b
INNER JOIN #Holders h
        ON h.DOSSIER = b.DOSSIER
WHERE b.AccountType IN ('CA', 'SA')

UNION ALL

-- Fallback : dossier NOUVEAU (présent en daily, absent du mensuel DGS)
--   -> holder = LEAD_RELATION_AFF comptable, traité comme compte PERSONNEL (100 %)
--   -> participe au plafond 100k du holder (max 100k en l'absence de co-titulaires)
--   -> couvre aussi les NOUVELLES RELATION_AFF inconnues du mensuel
SELECT b.LEAD_RELATION_AFF,
       b.DOSSIER,
       b.AccountType,
       b.TypeCompteComptable,
       CAST(b.BALANCE_EUR AS decimal(38,6)),
       0,
       CASE b.AccountType WHEN 'CA' THEN 0 ELSE 2 END
         + CASE b.TypeCompteComptable WHEN 'Capital' THEN 1 ELSE 2 END
FROM #Balances b
WHERE b.AccountType IN ('CA', 'SA')
  AND b.LEAD_RELATION_AFF IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM #Holders h WHERE h.DOSSIER = b.DOSSIER)
OPTION (MAXDOP 8);

CREATE CLUSTERED INDEX IX_Comp ON #Comp (RELATION_AFF, PRIO);


/* ===================================================================
   4. CA / SA — PLAFOND 100k PAR HOLDER
      Cumul des classes par ordre de priorité ; la classe "charnière"
      est écrêtée, puis répartie au pro-rata à l'étape 5.
   =================================================================== */
IF OBJECT_ID('tempdb..#ClassInsured') IS NOT NULL DROP TABLE #ClassInsured;

SELECT RELATION_AFF,
       PRIO,
       CLASS_TOTAL,
       CASE
           WHEN @CAP - (CUM_TOTAL - CLASS_TOTAL) <= 0           THEN CAST(0 AS decimal(38,6))
           WHEN @CAP - (CUM_TOTAL - CLASS_TOTAL) >= CLASS_TOTAL THEN CLASS_TOTAL
           ELSE @CAP - (CUM_TOTAL - CLASS_TOTAL)
       END AS INSURED_CLASS
INTO #ClassInsured
FROM (
    SELECT RELATION_AFF,
           PRIO,
           CLASS_TOTAL,
           SUM(CLASS_TOTAL) OVER (PARTITION BY RELATION_AFF
                                  ORDER BY PRIO
                                  ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS CUM_TOTAL
    FROM (
        SELECT RELATION_AFF, PRIO, SUM(SHARE_EUR) AS CLASS_TOTAL
        FROM #Comp
        GROUP BY RELATION_AFF, PRIO
    ) g
) c;

CREATE CLUSTERED INDEX IX_ClassInsured ON #ClassInsured (RELATION_AFF, PRIO);


/* ===================================================================
   5. CA / SA — PART ASSURÉE PAR HOLDER (table d'audit)
      Pro-rata de la classe assurée sur chaque composante du holder.
   =================================================================== */
IF OBJECT_ID('tempdb..#DGS_Holder_Audit') IS NOT NULL DROP TABLE #DGS_Holder_Audit;

SELECT c.RELATION_AFF,
       c.DOSSIER,
       c.AccountType,
       c.TypeCompteComptable,
       c.IS_JOINT,
       CAST(c.SHARE_EUR AS decimal(24,4)) AS SHARE_EUR,
       CAST(CASE WHEN ci.CLASS_TOTAL > 0
                 THEN c.SHARE_EUR * ci.INSURED_CLASS / ci.CLASS_TOTAL
                 ELSE 0 END AS decimal(38,6)) AS INSURED_EUR
INTO #DGS_Holder_Audit
FROM #Comp c
INNER JOIN #ClassInsured ci
        ON ci.RELATION_AFF = c.RELATION_AFF
       AND ci.PRIO         = c.PRIO
OPTION (MAXDOP 8);

CREATE CLUSTERED INDEX IX_Audit ON #DGS_Holder_Audit (DOSSIER, AccountType, TypeCompteComptable);


/* ===================================================================
   6. SD — PLAFOND 100k PAR DOSSIER (Capital avant Interest)
      NB : ne consomme PAS la capacité 100k des holders (garantie séparée)
   =================================================================== */
IF OBJECT_ID('tempdb..#SD') IS NOT NULL DROP TABLE #SD;

SELECT DOSSIER,
       AccountType,
       TypeCompteComptable,
       CAST(BALANCE_EUR AS decimal(38,6)) AS BALANCE_EUR,
       CASE
           WHEN @CAP - (CUM - BALANCE_EUR) <= 0           THEN CAST(0 AS decimal(38,6))
           WHEN @CAP - (CUM - BALANCE_EUR) >= BALANCE_EUR THEN CAST(BALANCE_EUR AS decimal(38,6))
           ELSE @CAP - (CUM - BALANCE_EUR)
       END AS INSURED_EUR
INTO #SD
FROM (
    SELECT DOSSIER,
           AccountType,
           TypeCompteComptable,
           BALANCE_EUR,
           SUM(BALANCE_EUR) OVER (PARTITION BY DOSSIER
                                  ORDER BY CASE TypeCompteComptable WHEN 'Capital' THEN 1 ELSE 2 END
                                  ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS CUM
    FROM #Balances
    WHERE AccountType = 'SD'
) s;


/* ===================================================================
   7. SC — RATIO MENSUEL PAR (DOSSIER × TYPE) RÉAPPLIQUÉ AU DAILY (mêm0 grain que CA/SA)
      ratio du dossier = SUM(MT_INSURED)/SUM(MT_ELIGIBLE) par CD_TYP_MONTANT
      ('Capital'/'Interest' — même convention que TypeCompteComptable daily),
      calculé sur la SOMME du dossier (pas record par record), borné entre 0 et 1.
      FALLBACK : dossier SC sans lignes ISIN dans le mensuel -> ratio GLOBAL
      (tous dossiers SC confondus) du même type, lui-même borné entre 0 et 1.
   =================================================================== */
IF OBJECT_ID('tempdb..#SC_Ratio') IS NOT NULL DROP TABLE #SC_Ratio;

SELECT C_DOSSIER AS DOSSIER,
       CD_TYP_MONTANT AS TYPECOMPT,
       CAST(CASE
                WHEN SUM(MT_ELIGIBLE) <= 0               THEN 0
                WHEN SUM(MT_INSURED) >= SUM(MT_ELIGIBLE) THEN 1
                ELSE SUM(MT_INSURED) / SUM(MT_ELIGIBLE)
            END AS decimal(38,12)) AS SC_RATIO
INTO #SC_Ratio
FROM #DGS_Month
WHERE ISIN IS NOT NULL
  AND ISIN <> ''
GROUP BY C_DOSSIER, CD_TYP_MONTANT;

CREATE CLUSTERED INDEX IX_SC_Ratio ON #SC_Ratio (DOSSIER, TYPECOMPT);

-- Ratio global par type (fallback dossier sans ISIN)
IF OBJECT_ID('tempdb..#SC_Ratio_Global') IS NOT NULL DROP TABLE #SC_Ratio_Global;

SELECT CD_TYP_MONTANT AS TYPECOMPT,
       CAST(CASE
                WHEN SUM(MT_ELIGIBLE) <= 0               THEN 0
                WHEN SUM(MT_INSURED) >= SUM(MT_ELIGIBLE) THEN 1
                ELSE SUM(MT_INSURED) / SUM(MT_ELIGIBLE)
            END AS decimal(38,12)) AS SC_RATIO
INTO #SC_Ratio_Global
FROM #DGS_Month
WHERE ISIN IS NOT NULL
  AND ISIN <> ''
GROUP BY CD_TYP_MONTANT;

CREATE CLUSTERED INDEX IX_SC_Ratio_Global ON #SC_Ratio_Global (TYPECOMPT);

IF OBJECT_ID('tempdb..#SC') IS NOT NULL DROP TABLE #SC;

SELECT b.DOSSIER,
       b.AccountType,
       b.TypeCompteComptable,
       CAST(b.BALANCE_EUR AS decimal(38,6)) AS BALANCE_EUR,
       CAST(b.BALANCE_EUR
            * ISNULL(rd.SC_RATIO,
                     ISNULL(rg.SC_RATIO, 0))            -- 0 si mensuel totalement vide en ISIN pour ce type
            AS decimal(38,6)) AS INSURED_EUR
INTO #SC
FROM #Balances b
LEFT JOIN #SC_Ratio rd
       ON rd.DOSSIER   = b.DOSSIER
      AND rd.TYPECOMPT = b.TypeCompteComptable
LEFT JOIN #SC_Ratio_Global rg
       ON rg.TYPECOMPT = b.TypeCompteComptable
WHERE b.AccountType = 'SC';


/* ===================================================================
   8. RÉSULTAT FINAL — grain (DOSSIER × AccountType × TypeCompteComptable)
   =================================================================== */
IF OBJECT_ID('tempdb..#DGS_Result') IS NOT NULL DROP TABLE #DGS_Result;

SELECT DOSSIER,
       AccountType,
       TypeCompteComptable,
       CAST(BALANCE_EUR AS decimal(24,4))            AS DAILY_BALANCE_EUR,
       CAST(INSURED_EUR AS decimal(24,4))            AS INSURED_AMOUNT_EUR,
       CAST(BALANCE_EUR - INSURED_EUR AS decimal(24,4)) AS NON_INSURED_AMOUNT_EUR
INTO #DGS_Result
FROM (
    -- CA / SA : solde daily exact ; assuré = somme des parts holders, plafonné au solde
    SELECT b.DOSSIER,
           b.AccountType,
           b.TypeCompteComptable,
           CAST(b.BALANCE_EUR AS decimal(38,6)) AS BALANCE_EUR,
           CASE WHEN ISNULL(a.INSURED_EUR, 0) > b.BALANCE_EUR
                THEN CAST(b.BALANCE_EUR AS decimal(38,6))
                ELSE ISNULL(a.INSURED_EUR, 0)
           END AS INSURED_EUR
    FROM #Balances b
    LEFT JOIN (
        SELECT DOSSIER, AccountType, TypeCompteComptable, SUM(INSURED_EUR) AS INSURED_EUR
        FROM #DGS_Holder_Audit
        GROUP BY DOSSIER, AccountType, TypeCompteComptable
    ) a ON a.DOSSIER             = b.DOSSIER
       AND a.AccountType         = b.AccountType
       AND a.TypeCompteComptable = b.TypeCompteComptable
    WHERE b.AccountType IN ('CA', 'SA')

    UNION ALL
    SELECT DOSSIER, AccountType, TypeCompteComptable, BALANCE_EUR, INSURED_EUR FROM #SD
    UNION ALL
    SELECT DOSSIER, AccountType, TypeCompteComptable, BALANCE_EUR, INSURED_EUR FROM #SC
) u
OPTION (MAXDOP 8);

CREATE CLUSTERED INDEX IX_DGS_Result ON #DGS_Result (DOSSIER, TypeCompteComptable, AccountType);


/* ===================================================================
   9. UTILISATION — ASSOCIER LES MONTANTS À #Daily_Accounting
      Le plafond étant calculé au grain dossier, on redescend sur les
      lignes daily au PRO-RATA du solde de chaque ligne : la somme des
      lignes redonne exactement le total du dossier.
      Si RECID est ajouté à #Daily_Accounting, il est propagé tel quel.
      Pour une vue par LEAD comptable : GROUP BY d.LEAD_RELATION_AFF.
   =================================================================== */
/*
SELECT d.RECID,                  -- si la colonne a été ajoutée à #Daily_Accounting
       d.DOSSIER,
       d.BAC_ACCOUNT,
       d.PRCT_NO,
       d.TypeCompteComptable,
       d.AccountType,
       d.LEAD_RELATION_AFF,
       d.DAILY_BALANCE_EUR,
       CAST(r.INSURED_AMOUNT_EUR * d.DAILY_BALANCE_EUR
            / NULLIF(SUM(d.DAILY_BALANCE_EUR) OVER (PARTITION BY d.DOSSIER, d.TypeCompteComptable), 0)
            AS decimal(24,4)) AS INSURED_AMOUNT_EUR,
       CAST(r.NON_INSURED_AMOUNT_EUR * d.DAILY_BALANCE_EUR
            / NULLIF(SUM(d.DAILY_BALANCE_EUR) OVER (PARTITION BY d.DOSSIER, d.TypeCompteComptable), 0)
            AS decimal(24,4)) AS NON_INSURED_AMOUNT_EUR
FROM #Daily_Accounting d
INNER JOIN (
    SELECT DOSSIER, TypeCompteComptable,
           SUM(INSURED_AMOUNT_EUR)     AS INSURED_AMOUNT_EUR,
           SUM(NON_INSURED_AMOUNT_EUR) AS NON_INSURED_AMOUNT_EUR
    FROM #DGS_Result
    GROUP BY DOSSIER, TypeCompteComptable
) r ON r.DOSSIER             = d.DOSSIER
   AND r.TypeCompteComptable = d.TypeCompteComptable;
*/


/* ===================================================================
   10. CONTRÔLE — NOTRE CALCUL vs MONTANTS MENSUELS REÇUS (MT_INSURED)
   =================================================================== */
-- 10a. Par dossier (tous types confondus) — contrôle global
SELECT m.C_DOSSIER,
       SUM(m.MT_ELIGIBLE)           AS MT_ELIGIBLE_RECU,
       SUM(m.MT_INSURED)            AS MT_INSURED_RECU,
       ISNULL(r.MT_INSURED_CALC, 0) AS MT_INSURED_CALCULE,
       ISNULL(r.MT_INSURED_CALC, 0) - SUM(m.MT_INSURED) AS ECART
FROM #DGS_Month m
LEFT JOIN (
    SELECT DOSSIER, SUM(INSURED_AMOUNT_EUR) AS MT_INSURED_CALC
    FROM #DGS_Result
    GROUP BY DOSSIER
) r ON r.DOSSIER = m.C_DOSSIER
GROUP BY m.C_DOSSIER, r.MT_INSURED_CALC
ORDER BY ABS(ISNULL(r.MT_INSURED_CALC, 0) - SUM(m.MT_INSURED)) DESC;

-- 10b. Par holder (CA/SA uniquement — SD/SC étant calculés au grain dossier,
--      des écarts sont attendus ici sur les dossiers SD/SC)
SELECT m.C_DOSSIER,
       m.RELATION_AFF,
       SUM(m.MT_INSURED)            AS MT_INSURED_RECU,
       ISNULL(a.MT_INSURED_CALC, 0) AS MT_INSURED_CALCULE_CASA,
       ISNULL(a.MT_INSURED_CALC, 0) - SUM(m.MT_INSURED) AS ECART
FROM #DGS_Month m
LEFT JOIN (
    SELECT DOSSIER, RELATION_AFF, SUM(INSURED_EUR) AS MT_INSURED_CALC
    FROM #DGS_Holder_Audit
    GROUP BY DOSSIER, RELATION_AFF
) a ON a.DOSSIER      = m.C_DOSSIER
   AND a.RELATION_AFF = m.RELATION_AFF
GROUP BY m.C_DOSSIER, m.RELATION_AFF, a.MT_INSURED_CALC
ORDER BY ABS(ISNULL(a.MT_INSURED_CALC, 0) - SUM(m.MT_INSURED)) DESC;
