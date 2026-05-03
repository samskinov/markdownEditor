using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MarkdownEditor.Services
{
    /// <summary>
    /// Provides utilities for extracting Mermaid blocks from Markdown text
    /// and building expert AI prompts to fix their syntax.
    /// </summary>
    public static class MermaidPromptService
    {
        // =========================================================================
        // Block extraction
        // =========================================================================

        /// <summary>
        /// Finds the Mermaid code block that contains <paramref name="caretOffset"/>
        /// inside <paramref name="markdown"/>. Returns null when not inside any block.
        /// </summary>
        public static string? ExtractMermaidBlockAtOffset(string markdown, int caretOffset)
            => MermaidBlockExtractor.TryExtract(markdown, caretOffset)?.Content;

        // =========================================================================
        // Prompt builder — public API
        // =========================================================================

        /// <summary>Auto-detects diagram type and builds the repair prompt.</summary>
        public static string BuildFixPrompt(string mermaidCode)
        {
            var diagramType = MermaidDiagramTypeDetector.Detect(mermaidCode);
            return BuildFixPrompt(mermaidCode, diagramType);
        }

        /// <summary>Builds the AI repair prompt for a known diagram type.</summary>
        public static string BuildFixPrompt(string mermaidCode, MermaidDiagramType diagramType)
        {
            var sb = new StringBuilder(8192);
            AppendHeader(sb, diagramType);
            AppendInput(sb, mermaidCode);
            AppendUniversalRules(sb);
            AppendTypeSpecificRules(sb, diagramType);
            AppendCommonErrors(sb, diagramType);
            AppendFooter(sb);
            return sb.ToString();
        }

        // =========================================================================
        // Response parsing
        // =========================================================================

        private static readonly Regex s_markerRegex = new Regex(
            @"<<<MERMAID>>>(.*?)<<<END>>>",
            RegexOptions.Singleline | RegexOptions.Compiled);

        /// <summary>
        /// Extracts the corrected Mermaid code from an AI response.
        /// Primary: <<<MERMAID>>> markers. Fallback: strip ```mermaid fences.
        /// Returns null when nothing usable is found.
        /// </summary>
        public static string? ParseResponse(string aiResponse)
        {
            if (string.IsNullOrWhiteSpace(aiResponse)) return null;

            var m = s_markerRegex.Match(aiResponse);
            if (m.Success)
                return m.Groups[1].Value.Trim('\r', '\n');

            var stripped = aiResponse.Trim();
            if (stripped.StartsWith("```mermaid", StringComparison.OrdinalIgnoreCase))
            {
                var firstNl = stripped.IndexOf('\n');
                if (firstNl >= 0)
                {
                    stripped = stripped.Substring(firstNl + 1);
                    if (stripped.EndsWith("```", StringComparison.Ordinal))
                        stripped = stripped.Substring(0, stripped.Length - 3);
                    return stripped.Trim('\r', '\n');
                }
            }

            return aiResponse.Trim();
        }

        // =========================================================================
        // Prompt sections
        // =========================================================================

        private static void AppendHeader(StringBuilder sb, MermaidDiagramType diagramType)
        {
            sb.AppendLine("================================================================");
            sb.AppendLine("  MERMAID DIAGRAM SYNTAX FIXER -- EXPERT AI PROMPT");
            sb.AppendLine("================================================================");
            sb.AppendLine();
            sb.AppendLine("ROLE:");
            sb.AppendLine("You are a Mermaid diagram syntax expert with deep knowledge of");
            sb.AppendLine("every diagram type supported by Mermaid v11. You fix broken");
            sb.AppendLine("Mermaid code while preserving its intent and structure.");
            sb.AppendLine();
            sb.AppendLine("DETECTED DIAGRAM TYPE: " + MermaidDiagramTypeDetector.ToDisplayName(diagramType));
            sb.AppendLine();
            sb.AppendLine("TASK:");
            sb.AppendLine("Analyze and fix ALL syntax errors in the Mermaid code provided");
            sb.AppendLine("in the INPUT section below. Apply every applicable rule from");
            sb.AppendLine("the RULES section, focusing on the detected diagram type above.");
            sb.AppendLine();
            sb.AppendLine("OUTPUT FORMAT (MANDATORY):");
            sb.AppendLine("Wrap the corrected Mermaid code between the two markers below,");
            sb.AppendLine("exactly as shown -- no other text outside the markers:");
            sb.AppendLine();
            sb.AppendLine("<<<MERMAID>>>");
            sb.AppendLine("<corrected mermaid code here>");
            sb.AppendLine("<<<END>>>");
            sb.AppendLine();
            sb.AppendLine("Rules for the output:");
            sb.AppendLine("  - Do NOT add ```mermaid fences inside the markers.");
            sb.AppendLine("  - Do NOT add explanations, commentary, or surrounding text.");
            sb.AppendLine("  - Preserve the original diagram semantics and all node labels.");
            sb.AppendLine("  - Preserve the original diagram type declaration.");
            sb.AppendLine();
        }

        private static void AppendInput(StringBuilder sb, string mermaidCode)
        {
            sb.AppendLine("================================================================");
            sb.AppendLine("  INPUT");
            sb.AppendLine("================================================================");
            sb.AppendLine();
            sb.AppendLine(mermaidCode);
            sb.AppendLine();
            sb.AppendLine("================================================================");
            sb.AppendLine("  END OF INPUT");
            sb.AppendLine("================================================================");
            sb.AppendLine();
        }

        // =========================================================================
        // Universal rules
        // =========================================================================

        private static void AppendUniversalRules(StringBuilder sb)
        {
            sb.AppendLine("================================================================");
            sb.AppendLine("  UNIVERSAL RULES (apply to ALL diagram types)");
            sb.AppendLine("================================================================");
            sb.AppendLine();
            sb.AppendLine("U1. QUOTE LABELS WITH SPECIAL CHARACTERS");
            sb.AppendLine("    Wrap any label, edge text, or title in double-quotes when it");
            sb.AppendLine("    contains ANY of:  ( ) { } [ ] < > & | ; : , . # @ ! ? /");
            sb.AppendLine("                      \\ % ^ = + * ~ ` '  (apostrophe/single-quote)");
            sb.AppendLine("    WRONG:  A[Process (step 1)]");
            sb.AppendLine("    RIGHT:  A[\"Process (step 1)\"]");
            sb.AppendLine("    WRONG:  --> |ok (confirmed)| B");
            sb.AppendLine("    RIGHT:  --> |\"ok (confirmed)\"| B");
            sb.AppendLine();
            sb.AppendLine("U2. USE ONLY STRAIGHT ASCII DOUBLE-QUOTES");
            sb.AppendLine("    Never use curly/smart quotes or backticks. Use only: \"  (ASCII 0x22)");
            sb.AppendLine();
            sb.AppendLine("U3. ESCAPE QUOTES INSIDE QUOTED LABELS");
            sb.AppendLine("    Use #quot; for a literal double-quote inside a label:");
            sb.AppendLine("    RIGHT:  A[\"He said #quot;hello#quot;\"]");
            sb.AppendLine();
            sb.AppendLine("U4. ACCENTED / NON-ASCII CHARACTERS IN LABELS");
            sb.AppendLine("    Always wrap labels containing accents or non-ASCII in quotes.");
            sb.AppendLine("    RIGHT:  A[\"Verification d'etat\"]");
            sb.AppendLine();
            sb.AppendLine("U5. COMMENTS");
            sb.AppendLine("    Use %% for comments. Never use // or #.");
            sb.AppendLine("    RIGHT:  %% This is a comment");
            sb.AppendLine();
            sb.AppendLine("U6. INDENTATION");
            sb.AppendLine("    Use 4-space indentation inside blocks. Do NOT mix tabs and spaces.");
            sb.AppendLine();
            sb.AppendLine("U7. ONE STATEMENT PER LINE");
            sb.AppendLine("    Do not put multiple arrows or declarations on the same line.");
            sb.AppendLine();
            sb.AppendLine("U8. ACCESSIBILITY METADATA (optional)");
            sb.AppendLine("    accTitle: My Diagram Title");
            sb.AppendLine("    accDescr: A short description for screen-readers.");
            sb.AppendLine();
            sb.AppendLine("U9. FRONT-MATTER / INIT DIRECTIVE (optional)");
            sb.AppendLine("    ---");
            sb.AppendLine("    title: My Title");
            sb.AppendLine("    config:");
            sb.AppendLine("      theme: forest");
            sb.AppendLine("    ---");
            sb.AppendLine("    OR inline on its own line before the diagram type:");
            sb.AppendLine("    %%{init: {\"theme\": \"dark\"}}%%");
            sb.AppendLine();
            sb.AppendLine("U10. PIPE CHARACTER IN LABELS");
            sb.AppendLine("     Use &#124; inside a quoted label to represent | without");
            sb.AppendLine("     conflicting with edge-label pipe syntax:");
            sb.AppendLine("     RIGHT:  A[\"A&#124;B\"]");
            sb.AppendLine();
        }

        // =========================================================================
        // Type-specific rules (cached per type)
        // =========================================================================

        private static readonly Dictionary<MermaidDiagramType, Lazy<string>> s_typeRulesCache
            = new Dictionary<MermaidDiagramType, Lazy<string>>();

        private static string GetTypeRules(MermaidDiagramType type)
        {
            lock (s_typeRulesCache)
            {
                if (!s_typeRulesCache.TryGetValue(type, out var lazy))
                {
                    lazy = new Lazy<string>(() => BuildTypeRules(type));
                    s_typeRulesCache[type] = lazy;
                }
                return lazy.Value;
            }
        }

        private static void AppendTypeSpecificRules(StringBuilder sb, MermaidDiagramType type)
        {
            var rules = GetTypeRules(type);
            if (rules.Length > 0)
            {
                sb.AppendLine("================================================================");
                sb.AppendLine("  DIAGRAM-SPECIFIC RULES: " + MermaidDiagramTypeDetector.ToDisplayName(type).ToUpperInvariant());
                sb.AppendLine("================================================================");
                sb.AppendLine();
                sb.Append(rules);
            }
        }

        private static string BuildTypeRules(MermaidDiagramType type)
        {
            var sb = new StringBuilder(2048);
            switch (type)
            {
                case MermaidDiagramType.Flowchart:
                case MermaidDiagramType.Graph:
                    AppendFlowchartRules(sb); break;
                case MermaidDiagramType.Sequence:
                    AppendSequenceRules(sb); break;
                case MermaidDiagramType.Class:
                    AppendClassRules(sb); break;
                case MermaidDiagramType.State:
                    AppendStateRules(sb); break;
                case MermaidDiagramType.Er:
                    AppendErRules(sb); break;
                case MermaidDiagramType.Gantt:
                    AppendGanttRules(sb); break;
                case MermaidDiagramType.Pie:
                    AppendPieRules(sb); break;
                case MermaidDiagramType.Mindmap:
                    AppendMindmapRules(sb); break;
                case MermaidDiagramType.Timeline:
                    AppendTimelineRules(sb); break;
                case MermaidDiagramType.GitGraph:
                    AppendGitGraphRules(sb); break;
                case MermaidDiagramType.Quadrant:
                    AppendQuadrantRules(sb); break;
                case MermaidDiagramType.XyChart:
                    AppendXyChartRules(sb); break;
                case MermaidDiagramType.Block:
                    AppendBlockRules(sb); break;
                case MermaidDiagramType.Requirement:
                    AppendRequirementRules(sb); break;
                case MermaidDiagramType.C4:
                    AppendC4Rules(sb); break;
                case MermaidDiagramType.Packet:
                    AppendPacketRules(sb); break;
                case MermaidDiagramType.Architecture:
                    AppendArchitectureRules(sb); break;
                case MermaidDiagramType.Journey:
                    AppendJourneyRules(sb); break;
                case MermaidDiagramType.Sankey:
                    AppendSankeyRules(sb); break;
                case MermaidDiagramType.Kanban:
                    AppendKanbanRules(sb); break;
                case MermaidDiagramType.Radar:
                    AppendRadarRules(sb); break;
                case MermaidDiagramType.Treemap:
                    AppendTreemapRules(sb); break;
            }
            return sb.ToString();
        }

        // =========================================================================
        // Type-specific rule sections
        // =========================================================================

        private static void AppendFlowchartRules(StringBuilder sb)
        {
            sb.AppendLine("F1. DECLARATION");
            sb.AppendLine("    flowchart TD|LR|BT|RL|TB");
            sb.AppendLine("    graph TD|LR|BT|RL|TB   (legacy alias; prefer flowchart)");
            sb.AppendLine();
            sb.AppendLine("F2. NODE IDs");
            sb.AppendLine("    Must start with a letter or underscore.");
            sb.AppendLine("    Must contain only [A-Za-z0-9_-]. No spaces, dots, slashes.");
            sb.AppendLine("    WRONG:  1Step   my node   step.1");
            sb.AppendLine("    RIGHT:  step1   myNode    step_1");
            sb.AppendLine();
            sb.AppendLine("F3. RESERVED KEYWORDS -- never use as node IDs:");
            sb.AppendLine("    end, graph, flowchart, subgraph, direction, classDef,");
            sb.AppendLine("    class, style, click, call, note, default");
            sb.AppendLine("    Rename: endNode, endState, endStep, etc.");
            sb.AppendLine();
            sb.AppendLine("F4. NODE SHAPES");
            sb.AppendLine("    [rectangle]        (round edges)       ((circle))");
            sb.AppendLine("    {rhombus}          {{hexagon}}         [/parallelogram/]");
            sb.AppendLine("    [\\anti-parallel\\]  ([stadium])         [[subroutine]]");
            sb.AppendLine("    [(cylinder)]       >asymmetric]");
            sb.AppendLine("    Trapezoid:         [/label\\]   [\\label/]");
            sb.AppendLine("    New shape API:     A@{ shape: diamond, label: \"text\" }");
            sb.AppendLine();
            sb.AppendLine("F5. ARROW TYPES");
            sb.AppendLine("    -->   ---   -.->  ==>   --o   --x");
            sb.AppendLine("    <-->  o--o  x--x  ~~~   <==>  ~~>");
            sb.AppendLine();
            sb.AppendLine("F6. ARROW LABELS");
            sb.AppendLine("    A -->|label| B   OR   A -- label --> B");
            sb.AppendLine("    Special chars: -->|\"label (v2)\"|");
            sb.AppendLine();
            sb.AppendLine("F7. SUBGRAPH");
            sb.AppendLine("    subgraph id [\"Optional title\"]");
            sb.AppendLine("        direction LR");
            sb.AppendLine("        A --> B");
            sb.AppendLine("    end");
            sb.AppendLine("    Every subgraph MUST be closed by 'end'.");
            sb.AppendLine();
            sb.AppendLine("F8. STYLE & CLASSDEFS");
            sb.AppendLine("    classDef className fill:#f9f,stroke:#333,stroke-width:2px;");
            sb.AppendLine("    class nodeA,nodeB className");
            sb.AppendLine("    style nodeA fill:#bbf,stroke:#00f");
            sb.AppendLine();
            sb.AppendLine("F9. LINK STYLE");
            sb.AppendLine("    linkStyle 0 stroke:#ff0000,stroke-width:2px");
            sb.AppendLine("    linkStyle 0,1,2 stroke:green");
            sb.AppendLine("    Index 0 = first arrow defined in the diagram.");
            sb.AppendLine();
            sb.AppendLine("F10. CLICK EVENTS");
            sb.AppendLine("    click nodeId href \"https://url\" _blank");
            sb.AppendLine("    click nodeId call functionName()");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (Flowchart) --------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  flowchart LR");
            sb.AppendLine("    1Start[Start (init)] --> end[Done]");
            sb.AppendLine("    end --> A|result (ok)|B");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  flowchart LR");
            sb.AppendLine("    startNode[\"Start (init)\"] --> doneNode[\"Done\"]");
            sb.AppendLine("    doneNode -->|\"result (ok)\"| B");
            sb.AppendLine();
        }

        private static void AppendSequenceRules(StringBuilder sb)
        {
            sb.AppendLine("S1. DECLARATION:  sequenceDiagram");
            sb.AppendLine();
            sb.AppendLine("S2. PARTICIPANTS");
            sb.AppendLine("    participant AliceAlias as \"Alice Label\"");
            sb.AppendLine("    actor BobAlias as \"Bob Label\"");
            sb.AppendLine("    Declare all participants before using them.");
            sb.AppendLine();
            sb.AppendLine("S3. MESSAGE ARROW SYNTAX");
            sb.AppendLine("    Form: Sender ArrowType Receiver: Message text");
            sb.AppendLine("    The colon (:) between receiver and message is MANDATORY.");
            sb.AppendLine("    Arrow types:");
            sb.AppendLine("      A->>B    solid arrowhead (async request)");
            sb.AppendLine("      A->B     solid open arrow");
            sb.AppendLine("      A-->B    dashed open arrow");
            sb.AppendLine("      A-->>B   dashed arrowhead (async reply)");
            sb.AppendLine("      A-xB     solid cross (error / terminate)");
            sb.AppendLine("      A-)B     solid open dot (fire-and-forget)");
            sb.AppendLine("    There is no <<>> bidirectional arrow; use two messages.");
            sb.AppendLine("    WRONG:  A->>B message   (missing colon)");
            sb.AppendLine("    RIGHT:  A->>B: message");
            sb.AppendLine();
            sb.AppendLine("S4. ACTIVATION");
            sb.AppendLine("    activate A  /  deactivate A");
            sb.AppendLine("    Shorthand: A->>+B: msg  /  B-->>-A: reply");
            sb.AppendLine();
            sb.AppendLine("S5. NOTES");
            sb.AppendLine("    Note right of A: text");
            sb.AppendLine("    Note left of B: text");
            sb.AppendLine("    Note over A,B: spanning text");
            sb.AppendLine();
            sb.AppendLine("S6. GROUPING BLOCKS -- all must be closed with 'end'");
            sb.AppendLine("    loop \"condition\" ... end");
            sb.AppendLine("    alt \"condition\" ... else \"other\" ... end");
            sb.AppendLine("    opt \"optional\" ... end");
            sb.AppendLine("    par \"parallel\" ... and \"branch 2\" ... end");
            sb.AppendLine("    critical \"critical\" ... option \"opt\" ... end");
            sb.AppendLine("    break \"break\" ... end");
            sb.AppendLine("    rect rgb(255,0,0) ... end");
            sb.AppendLine();
            sb.AppendLine("S7. AUTONUMBER:  autonumber  (on its own line after declaration)");
            sb.AppendLine();
            sb.AppendLine("S8. CREATE / DESTROY");
            sb.AppendLine("    create participant C");
            sb.AppendLine("    A->>C: hi");
            sb.AppendLine("    destroy C");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (Sequence) ---------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  sequenceDiagram");
            sb.AppendLine("    A->>B hello");
            sb.AppendLine("    B-->A reply (ok)");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  sequenceDiagram");
            sb.AppendLine("    A->>B: hello");
            sb.AppendLine("    B-->>A: \"reply (ok)\"");
            sb.AppendLine();
        }

        private static void AppendClassRules(StringBuilder sb)
        {
            sb.AppendLine("C1. DECLARATION:  classDiagram");
            sb.AppendLine();
            sb.AppendLine("C2. CLASS DEFINITION");
            sb.AppendLine("    class Animal {");
            sb.AppendLine("        +String name");
            sb.AppendLine("        #int age");
            sb.AppendLine("        -bool active");
            sb.AppendLine("        +eat() void");
            sb.AppendLine("        #run(speed int) bool");
            sb.AppendLine("    }");
            sb.AppendLine("    Visibility: + public  - private  # protected  ~ package");
            sb.AppendLine("    Classifiers: $ static  * abstract");
            sb.AppendLine("    Methods MUST have parens: +eat() void");
            sb.AppendLine("    Attributes must NOT have parens: +String name");
            sb.AppendLine();
            sb.AppendLine("C3. RELATIONSHIPS");
            sb.AppendLine("    Animal <|-- Dog      inheritance");
            sb.AppendLine("    Animal *-- Leg       composition");
            sb.AppendLine("    Animal o-- Habitat   aggregation");
            sb.AppendLine("    A --> B              association");
            sb.AppendLine("    A -- B               link");
            sb.AppendLine("    A ..> B              dependency");
            sb.AppendLine("    A ..|> B             realization");
            sb.AppendLine("    With multiplicity: Animal \"1\" --> \"*\" Dog : owns");
            sb.AppendLine();
            sb.AppendLine("C4. NAMESPACE");
            sb.AppendLine("    namespace Zoo {");
            sb.AppendLine("        class Animal");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("C5. ANNOTATIONS");
            sb.AppendLine("    <<interface>>  <<abstract>>  <<service>>  <<enumeration>>");
            sb.AppendLine("    Place inside the class body or directly before the class name.");
            sb.AppendLine();
            sb.AppendLine("C6. NOTES");
            sb.AppendLine("    note for MyClass \"A note about this class\"");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (Class) ------------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  classDiagram");
            sb.AppendLine("    class Car {");
            sb.AppendLine("        +String brand");
            sb.AppendLine("        +start void");
            sb.AppendLine("    }");
            sb.AppendLine("    Car <-- Engine");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  classDiagram");
            sb.AppendLine("    class Car {");
            sb.AppendLine("        +String brand");
            sb.AppendLine("        +start() void");
            sb.AppendLine("    }");
            sb.AppendLine("    Car *-- Engine");
            sb.AppendLine();
        }

        private static void AppendStateRules(StringBuilder sb)
        {
            sb.AppendLine("T1. DECLARATION:  stateDiagram-v2   (always prefer v2)");
            sb.AppendLine();
            sb.AppendLine("T2. TRANSITIONS");
            sb.AppendLine("    [*] --> Idle             initial transition");
            sb.AppendLine("    Idle --> Running : start  with label");
            sb.AppendLine("    Running --> [*]           final transition");
            sb.AppendLine();
            sb.AppendLine("T3. COMPOSITE / NESTED STATES");
            sb.AppendLine("    state \"Compound Label\" as CS {");
            sb.AppendLine("        State1 --> State2");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("T4. FORK / JOIN");
            sb.AppendLine("    state fork_st <<fork>>");
            sb.AppendLine("    state join_st <<join>>");
            sb.AppendLine("    [*] --> fork_st");
            sb.AppendLine("    fork_st --> A");
            sb.AppendLine("    A --> join_st");
            sb.AppendLine();
            sb.AppendLine("T5. CONCURRENT STATES");
            sb.AppendLine("    state Compound {");
            sb.AppendLine("        StateA");
            sb.AppendLine("        --");
            sb.AppendLine("        StateB");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("T6. NOTES");
            sb.AppendLine("    note right of StateA");
            sb.AppendLine("        text here");
            sb.AppendLine("    end note");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (State) ------------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  stateDiagram");
            sb.AppendLine("    [*] -> Idle");
            sb.AppendLine("    Idle -> Running");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  stateDiagram-v2");
            sb.AppendLine("    [*] --> Idle");
            sb.AppendLine("    Idle --> Running");
            sb.AppendLine("    Running --> [*]");
            sb.AppendLine();
        }

        private static void AppendErRules(StringBuilder sb)
        {
            sb.AppendLine("E1. DECLARATION:  erDiagram");
            sb.AppendLine();
            sb.AppendLine("E2. ENTITY BLOCK");
            sb.AppendLine("    CUSTOMER {");
            sb.AppendLine("        string  name");
            sb.AppendLine("        int     id    PK");
            sb.AppendLine("        int     ordId FK");
            sb.AppendLine("        bool    active");
            sb.AppendLine("    }");
            sb.AppendLine("    Attribute types: string int float boolean date datetime");
            sb.AppendLine("    Key types: PK FK UK");
            sb.AppendLine();
            sb.AppendLine("E3. RELATIONSHIPS");
            sb.AppendLine("    CUSTOMER ||--o{ ORDER : \"places\"");
            sb.AppendLine("    Cardinality symbols:");
            sb.AppendLine("      ||  exactly one      |o  zero or one");
            sb.AppendLine("      }|  one or more      }o  zero or more");
            sb.AppendLine("    Relationship label MUST be quoted when it contains spaces.");
            sb.AppendLine("    WRONG:  A 1--N B : owns");
            sb.AppendLine("    RIGHT:  A ||--o{ B : \"owns\"");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (ER) ---------------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  erDiagram");
            sb.AppendLine("    CUSTOMER 1--N ORDER : places");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  erDiagram");
            sb.AppendLine("    CUSTOMER ||--o{ ORDER : \"places\"");
            sb.AppendLine();
        }

        private static void AppendGanttRules(StringBuilder sb)
        {
            sb.AppendLine("G1. MANDATORY first lines after 'gantt':");
            sb.AppendLine("    gantt");
            sb.AppendLine("    dateFormat YYYY-MM-DD");
            sb.AppendLine();
            sb.AppendLine("G2. OPTIONAL HEADER (before sections):");
            sb.AppendLine("    title My Project Plan");
            sb.AppendLine("    axisFormat %Y-%m-%d");
            sb.AppendLine("    tickInterval 1week");
            sb.AppendLine("    todayMarker off");
            sb.AppendLine("    excludes weekends");
            sb.AppendLine();
            sb.AppendLine("G3. SECTIONS AND TASKS");
            sb.AppendLine("    section Phase 1");
            sb.AppendLine("    Task A      : taskA, 2024-01-01, 7d");
            sb.AppendLine("    Task B      : crit, taskB, after taskA, 3d");
            sb.AppendLine("    Task C      : done, taskC, 2024-01-08, 2d");
            sb.AppendLine("    Milestone   : milestone, ms1, 2024-01-11, 0d");
            sb.AppendLine("    Duration formats: 1d  2w  3h  or ISO date");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (Gantt) ------------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  gantt");
            sb.AppendLine("    title My Plan");
            sb.AppendLine("    Task A : 2024-01-01, 7d");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  gantt");
            sb.AppendLine("    dateFormat YYYY-MM-DD");
            sb.AppendLine("    title My Plan");
            sb.AppendLine("    section Phase 1");
            sb.AppendLine("    Task A : taskA, 2024-01-01, 7d");
            sb.AppendLine();
        }

        private static void AppendPieRules(StringBuilder sb)
        {
            sb.AppendLine("P1. DECLARATION:  pie  or  pie showData");
            sb.AppendLine("P2. Optional:  title \"My Pie Chart\"");
            sb.AppendLine("P3. Slices (label MUST be quoted; value must be a positive number):");
            sb.AppendLine("    \"Slice A\" : 40");
            sb.AppendLine("    \"Slice B\" : 30.5");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (Pie) --------------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  pie");
            sb.AppendLine("    Cats : 40");
            sb.AppendLine("    Dogs : 60");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  pie showData");
            sb.AppendLine("    title Pet Distribution");
            sb.AppendLine("    \"Cats\" : 40");
            sb.AppendLine("    \"Dogs\" : 60");
            sb.AppendLine();
        }

        private static void AppendMindmapRules(StringBuilder sb)
        {
            sb.AppendLine("M1. DECLARATION:  mindmap");
            sb.AppendLine("M2. Hierarchy by indentation (consistent spaces or tabs).");
            sb.AppendLine("M3. Root is the first non-empty indented line after 'mindmap'.");
            sb.AppendLine("M4. Node shapes:");
            sb.AppendLine("    (round)  ((cloud))  [square]  ))bang((  )cloud(  {{hex}}");
            sb.AppendLine("M5. Icons:  ::icon(fa fa-book)");
            sb.AppendLine("M6. Classes:  :::className");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (Mindmap) ----------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  mindmap");
            sb.AppendLine("  Root");
            sb.AppendLine("    child A");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  mindmap");
            sb.AppendLine("    root((Root))");
            sb.AppendLine("        child A");
            sb.AppendLine();
        }

        private static void AppendTimelineRules(StringBuilder sb)
        {
            sb.AppendLine("TL1. DECLARATION:  timeline");
            sb.AppendLine("TL2. Optional:  title My Timeline");
            sb.AppendLine("TL3. Sections group periods:  section Section Name");
            sb.AppendLine("TL4. Period with events (colon-separated):");
            sb.AppendLine("    2020 : Event A : Event B");
            sb.AppendLine("    2021 : Event C");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (Timeline) ---------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  timeline");
            sb.AppendLine("    2020 -- First year");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  timeline");
            sb.AppendLine("    title History");
            sb.AppendLine("    2020 : Founded");
            sb.AppendLine("    2021 : First product : Series A");
            sb.AppendLine();
        }

        private static void AppendGitGraphRules(StringBuilder sb)
        {
            sb.AppendLine("GG1. DECLARATION:  gitGraph");
            sb.AppendLine("GG2. Valid commands:");
            sb.AppendLine("     commit");
            sb.AppendLine("     commit id: \"v1.0\" type: HIGHLIGHT tag: \"release\"");
            sb.AppendLine("     branch dev");
            sb.AppendLine("     checkout dev");
            sb.AppendLine("     merge main");
            sb.AppendLine("     cherry-pick id: \"abc\"");
            sb.AppendLine("GG3. Commit types: NORMAL  REVERSE  HIGHLIGHT");
            sb.AppendLine("GG4. Optional layout:  gitGraph LR");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (GitGraph) ---------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  gitGraph");
            sb.AppendLine("    commit \"initial\"");
            sb.AppendLine("    branch feature");
            sb.AppendLine("    commit \"feat\"");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  gitGraph");
            sb.AppendLine("    commit id: \"initial\"");
            sb.AppendLine("    branch feature");
            sb.AppendLine("    checkout feature");
            sb.AppendLine("    commit id: \"feat\"");
            sb.AppendLine("    checkout main");
            sb.AppendLine("    merge feature");
            sb.AppendLine();
        }

        private static void AppendQuadrantRules(StringBuilder sb)
        {
            sb.AppendLine("Q1. DECLARATION:  quadrantChart");
            sb.AppendLine("Q2. Optional:  title My Chart");
            sb.AppendLine("Q3. Axes:");
            sb.AppendLine("    x-axis \"Low label\" --> \"High label\"");
            sb.AppendLine("    y-axis \"Low label\" --> \"High label\"");
            sb.AppendLine("Q4. Quadrant labels:");
            sb.AppendLine("    quadrant-1 \"Q1\"   quadrant-2 \"Q2\"");
            sb.AppendLine("    quadrant-3 \"Q3\"   quadrant-4 \"Q4\"");
            sb.AppendLine("Q5. Data points (x and y between 0 and 1):");
            sb.AppendLine("    PointName: [0.45, 0.72]");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (Quadrant) ---------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  quadrantChart");
            sb.AppendLine("    x-axis Low High");
            sb.AppendLine("    A: 0.3 0.8");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  quadrantChart");
            sb.AppendLine("    x-axis \"Low\" --> \"High\"");
            sb.AppendLine("    y-axis \"Low\" --> \"High\"");
            sb.AppendLine("    A: [0.3, 0.8]");
            sb.AppendLine();
        }

        private static void AppendXyChartRules(StringBuilder sb)
        {
            sb.AppendLine("XY1. DECLARATION:  xychart-beta");
            sb.AppendLine("XY2. title \"Chart Title\"");
            sb.AppendLine("XY3. x-axis [\"Jan\", \"Feb\", \"Mar\"]   or   x-axis 0 --> 100");
            sb.AppendLine("XY4. y-axis \"Value\" 0 --> 100");
            sb.AppendLine("XY5. bar [val1, val2, val3]");
            sb.AppendLine("XY6. line [val1, val2, val3]");
            sb.AppendLine("     Data arrays must have the same count as x-axis categories.");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (XY Chart) ---------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  xychart-beta");
            sb.AppendLine("    x-axis Jan Feb Mar");
            sb.AppendLine("    bar 10 20 30");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  xychart-beta");
            sb.AppendLine("    title \"Monthly Sales\"");
            sb.AppendLine("    x-axis [\"Jan\", \"Feb\", \"Mar\"]");
            sb.AppendLine("    y-axis \"Sales\" 0 --> 50");
            sb.AppendLine("    bar [10, 20, 30]");
            sb.AppendLine();
        }

        private static void AppendBlockRules(StringBuilder sb)
        {
            sb.AppendLine("B1. DECLARATION:  block-beta");
            sb.AppendLine("B2. columns N  (set grid column count)");
            sb.AppendLine("B3. Block types:");
            sb.AppendLine("    A[\"rect\"]  A((\"circle\"))  A>\"point\"]  A{\"diamond\"}  space  space:2");
            sb.AppendLine("B4. Edges:  A --> B    A -- \"label\" --> B");
            sb.AppendLine("B5. Group blocks:");
            sb.AppendLine("    block:groupId[\"title\"]");
            sb.AppendLine("        A[\"a\"]  B[\"b\"]");
            sb.AppendLine("    end");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (Block) ------------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  block-beta");
            sb.AppendLine("    A --> B");
            sb.AppendLine("    block: grp");
            sb.AppendLine("      C D");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  block-beta");
            sb.AppendLine("    columns 3");
            sb.AppendLine("    A[\"A\"] --> B[\"B\"]");
            sb.AppendLine("    block:grp[\"Group\"]");
            sb.AppendLine("        C[\"C\"]  D[\"D\"]");
            sb.AppendLine("    end");
            sb.AppendLine();
        }

        private static void AppendRequirementRules(StringBuilder sb)
        {
            sb.AppendLine("R1. DECLARATION:  requirementDiagram");
            sb.AppendLine("R2. Requirement types:");
            sb.AppendLine("    requirement | functionalRequirement | interfaceRequirement");
            sb.AppendLine("    performanceRequirement | physicalRequirement | designConstraint");
            sb.AppendLine("R3. Requirement block:");
            sb.AppendLine("    requirement reqName {");
            sb.AppendLine("        id: R1");
            sb.AppendLine("        text: \"Description\"");
            sb.AppendLine("        risk: low | medium | high");
            sb.AppendLine("        verifymethod: analysis | inspection | test | demonstration");
            sb.AppendLine("    }");
            sb.AppendLine("R4. Element block:");
            sb.AppendLine("    element elemName {");
            sb.AppendLine("        type: \"theType\"");
            sb.AppendLine("        docref: \"someRef\"");
            sb.AppendLine("    }");
            sb.AppendLine("R5. Relationships:");
            sb.AppendLine("    src - satisfies -> dst");
            sb.AppendLine("    Types: satisfies traces refines copies contains derives verifies");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (Requirement) ------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  requirementDiagram");
            sb.AppendLine("    requirement req1 {");
            sb.AppendLine("        id: 1");
            sb.AppendLine("        text: Must work");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  requirementDiagram");
            sb.AppendLine("    requirement req1 {");
            sb.AppendLine("        id: R1");
            sb.AppendLine("        text: \"Must work correctly\"");
            sb.AppendLine("        risk: low");
            sb.AppendLine("        verifymethod: test");
            sb.AppendLine("    }");
            sb.AppendLine();
        }

        private static void AppendC4Rules(StringBuilder sb)
        {
            sb.AppendLine("C4_1. DECLARATION TYPES:");
            sb.AppendLine("      C4Context  C4Container  C4Component  C4Dynamic  C4Deployment");
            sb.AppendLine("C4_2. ELEMENTS:");
            sb.AppendLine("      Person(alias, \"label\", \"descr\")");
            sb.AppendLine("      System(alias, \"label\", \"descr\")");
            sb.AppendLine("      Container(alias, \"label\", \"tech\", \"descr\")");
            sb.AppendLine("      Component(alias, \"label\", \"tech\", \"descr\")");
            sb.AppendLine("C4_3. BOUNDARIES:");
            sb.AppendLine("      System_Boundary(alias, \"label\") { ... }");
            sb.AppendLine("      Container_Boundary(alias, \"label\") { ... }");
            sb.AppendLine("C4_4. RELATIONSHIPS:");
            sb.AppendLine("      Rel(from, to, \"label\")");
            sb.AppendLine("      BiRel(from, to, \"label\")");
            sb.AppendLine("      Rel_Back(from, to, \"label\")");
            sb.AppendLine("C4_5. UpdateLayoutConfig($c4ShapeInRow=\"3\", $c4BoundaryInRow=\"1\")");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (C4) ---------------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  C4Context");
            sb.AppendLine("    Person(user, User)");
            sb.AppendLine("    System(app, My App)");
            sb.AppendLine("    Rel(user, app, uses)");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  C4Context");
            sb.AppendLine("    title System Context");
            sb.AppendLine("    Person(user, \"User\", \"A system user\")");
            sb.AppendLine("    System(app, \"My App\", \"The main application\")");
            sb.AppendLine("    Rel(user, app, \"Uses\")");
            sb.AppendLine();
        }

        private static void AppendPacketRules(StringBuilder sb)
        {
            sb.AppendLine("PK1. DECLARATION:  packet-beta");
            sb.AppendLine("PK2. Fields defined by bit ranges (0-based, inclusive, contiguous):");
            sb.AppendLine("     0-7: \"Source Port\"");
            sb.AppendLine("     8-15: \"Dest Port\"");
            sb.AppendLine("     16-31: \"Sequence Number\"");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (Packet) -----------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  packet-beta");
            sb.AppendLine("    0-7 Src");
            sb.AppendLine("    8-15 Dst");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  packet-beta");
            sb.AppendLine("    0-7: \"Source Port\"");
            sb.AppendLine("    8-15: \"Dest Port\"");
            sb.AppendLine();
        }

        private static void AppendArchitectureRules(StringBuilder sb)
        {
            sb.AppendLine("AR1. DECLARATION:  architecture-beta");
            sb.AppendLine("AR2. Groups:  group groupId(icon)[\"label\"] in parentGroupId");
            sb.AppendLine("AR3. Services: service svcId(icon)[\"label\"] in groupId");
            sb.AppendLine("AR4. Edges:  svcA:R --> L:svcB  (directions: L R T B)");
            sb.AppendLine("     Both endpoints MUST have a direction suffix.");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (Architecture) -----------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  architecture-beta");
            sb.AppendLine("    service api[API]");
            sb.AppendLine("    service db[Database]");
            sb.AppendLine("    api --> db");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  architecture-beta");
            sb.AppendLine("    group backend(cloud)[\"Backend\"]");
            sb.AppendLine("    service api(server)[\"API\"] in backend");
            sb.AppendLine("    service db(database)[\"Database\"] in backend");
            sb.AppendLine("    api:R --> L:db");
            sb.AppendLine();
        }

        private static void AppendJourneyRules(StringBuilder sb)
        {
            sb.AppendLine("J1. DECLARATION:  journey");
            sb.AppendLine("J2. Optional:  title My Journey");
            sb.AppendLine("J3. Sections group tasks:  section Section Name");
            sb.AppendLine("J4. Tasks:");
            sb.AppendLine("    Task name: score: Actor1, Actor2");
            sb.AppendLine("    score is an integer 1 (worst) to 5 (best).");
            sb.AppendLine("    At least one actor is required per task.");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (Journey) ----------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  journey");
            sb.AppendLine("    Buy ticket: 3");
            sb.AppendLine("    Board train: 5");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  journey");
            sb.AppendLine("    title My Travel");
            sb.AppendLine("    section Departure");
            sb.AppendLine("    Buy ticket: 3: User");
            sb.AppendLine("    Board train: 5: User");
            sb.AppendLine();
        }

        private static void AppendSankeyRules(StringBuilder sb)
        {
            sb.AppendLine("SK1. DECLARATION:  sankey-beta");
            sb.AppendLine("SK2. Each line is a CSV row: source,target,value");
            sb.AppendLine("     Wrap source/target in quotes when they contain commas.");
            sb.AppendLine("     Value is a positive number (flow amount).");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (Sankey) -----------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  sankey-beta");
            sb.AppendLine("    A -> B: 10");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  sankey-beta");
            sb.AppendLine("    A,B,10");
            sb.AppendLine("    B,C,7");
            sb.AppendLine("    B,D,3");
            sb.AppendLine();
        }

        private static void AppendKanbanRules(StringBuilder sb)
        {
            sb.AppendLine("K1. DECLARATION:  kanban");
            sb.AppendLine("K2. Columns and items:");
            sb.AppendLine("    column-id[\"Column Title\"]");
            sb.AppendLine("        item-id[\"Item label\"]");
            sb.AppendLine("        @{ assigned: 'Alice', priority: 'Very High', ticket: 'T-1' }");
            sb.AppendLine("K3. Items can have @{ ... } metadata blocks.");
            sb.AppendLine("K4. Indent items under their column (4 spaces).");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (Kanban) -----------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  kanban");
            sb.AppendLine("  Todo");
            sb.AppendLine("    Task 1");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  kanban");
            sb.AppendLine("    todo[\"To Do\"]");
            sb.AppendLine("        t1[\"Task 1\"]");
            sb.AppendLine();
        }

        private static void AppendRadarRules(StringBuilder sb)
        {
            sb.AppendLine("RD1. DECLARATION:  radar-beta");
            sb.AppendLine("RD2. Optional:  title \"Radar Title\"");
            sb.AppendLine("RD3. Axes:  axis Speed, Strength, Agility");
            sb.AppendLine("RD4. Data series:");
            sb.AppendLine("     curve seriesName { value1, value2, value3 }");
            sb.AppendLine("     Value count must match axis count.");
            sb.AppendLine("RD5. Optional scale:  max 100");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (Radar) ------------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  radar-beta");
            sb.AppendLine("    axis A B C");
            sb.AppendLine("    Hero: 80 70 90");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  radar-beta");
            sb.AppendLine("    title \"Hero Stats\"");
            sb.AppendLine("    axis Attack, Defense, Speed");
            sb.AppendLine("    curve Hero { 80, 70, 90 }");
            sb.AppendLine();
        }

        private static void AppendTreemapRules(StringBuilder sb)
        {
            sb.AppendLine("TM1. DECLARATION:  treemap-beta");
            sb.AppendLine("TM2. Optional:  title \"Treemap Title\"");
            sb.AppendLine("TM3. Hierarchy by indentation:");
            sb.AppendLine("    Root");
            sb.AppendLine("        Branch A");
            sb.AppendLine("            Leaf 1: 30");
            sb.AppendLine("            Leaf 2: 20");
            sb.AppendLine("    Leaves have a numeric value; branches do not.");
            sb.AppendLine();
            sb.AppendLine("--- FEW-SHOT EXAMPLE (Treemap) ----------------------------------");
            sb.AppendLine("BEFORE (broken):");
            sb.AppendLine("  treemap-beta");
            sb.AppendLine("    A: 30");
            sb.AppendLine("    B: 20");
            sb.AppendLine();
            sb.AppendLine("AFTER (fixed):");
            sb.AppendLine("  treemap-beta");
            sb.AppendLine("    title \"Budget\"");
            sb.AppendLine("    Root");
            sb.AppendLine("        A: 30");
            sb.AppendLine("        B: 20");
            sb.AppendLine();
        }

        // =========================================================================
        // Common errors — filtered by diagram type
        // =========================================================================

        private static readonly Dictionary<MermaidDiagramType, HashSet<int>> s_typeErrors
            = new Dictionary<MermaidDiagramType, HashSet<int>>
        {
            { MermaidDiagramType.Sequence,   new HashSet<int> { 1,2,3,7,8,9,11,16,19,20 } },
            { MermaidDiagramType.Gantt,      new HashSet<int> { 1,2,5,8,12,16,19,20 } },
            { MermaidDiagramType.Er,         new HashSet<int> { 1,2,8,13,16,19,20 } },
            { MermaidDiagramType.Class,      new HashSet<int> { 1,2,8,14,16,19,20 } },
            { MermaidDiagramType.State,      new HashSet<int> { 1,2,7,8,9,16,17,19,20 } },
            { MermaidDiagramType.Pie,        new HashSet<int> { 2,8,15,16,19 } },
            { MermaidDiagramType.Mindmap,    new HashSet<int> { 1,2,4,6,8,16,19,20 } },
            { MermaidDiagramType.Sankey,     new HashSet<int> { 2,8,16,19 } },
        };

        private static bool ShouldIncludeError(MermaidDiagramType type, int errNum)
        {
            if (type == MermaidDiagramType.Unknown) return true;
            if (s_typeErrors.TryGetValue(type, out var set)) return set.Contains(errNum);
            // Default: include universal (1-10) + comments/indent (19-20)
            return errNum <= 10 || errNum >= 19;
        }

        private static void AppendCommonErrors(StringBuilder sb, MermaidDiagramType type)
        {
            sb.AppendLine("================================================================");
            sb.AppendLine("  MOST COMMON AI-GENERATED ERRORS");
            sb.AppendLine("================================================================");
            sb.AppendLine();

            var errors = new (int Num, string Text)[]
            {
                (1,  "ERR-01  Unquoted parentheses in node labels\n" +
                     "  BAD:  A[Step (optional)]        GOOD: A[\"Step (optional)\"]"),
                (2,  "ERR-02  Unquoted colons in node labels\n" +
                     "  BAD:  A[Status: active]         GOOD: A[\"Status: active\"]"),
                (3,  "ERR-03  Unquoted arrow labels with special characters\n" +
                     "  BAD:  -->|response (200)|       GOOD: -->|\"response (200)\"|"),
                (4,  "ERR-04  Using 'end' as a node ID (reserved keyword)\n" +
                     "  BAD:  end --> Next              GOOD: endNode --> Next"),
                (5,  "ERR-05  Node ID starting with a digit\n" +
                     "  BAD:  1A[First Step]            GOOD: stepA[\"First Step\"]"),
                (6,  "ERR-06  Spaces inside node IDs\n" +
                     "  BAD:  my node[label]            GOOD: myNode[\"label\"]"),
                (7,  "ERR-07  Missing 'end' to close subgraph, loop, or alt block\n" +
                     "  Each opened block MUST have a matching 'end'."),
                (8,  "ERR-08  Curly/smart quotes instead of straight ASCII quotes\n" +
                     "  BAD:  A[\u201cLabel\u201d]                GOOD: A[\"Label\"]"),
                (9,  "ERR-09  Wrong arrow syntax\n" +
                     "  BAD:  A -> B   A => B          GOOD: A --> B   A ==> B"),
                (10, "ERR-10  Unquoted pipe '|' in a node label\n" +
                     "  BAD:  A[A|B]                   GOOD: A[\"A&#124;B\"]"),
                (11, "ERR-11  sequenceDiagram message missing colon separator\n" +
                     "  BAD:  A->>B message            GOOD: A->>B: message"),
                (12, "ERR-12  gantt missing mandatory 'dateFormat' line\n" +
                     "  Every gantt diagram MUST include: dateFormat YYYY-MM-DD"),
                (13, "ERR-13  erDiagram relationship syntax error\n" +
                     "  BAD:  A 1--N B : owns          GOOD: A ||--o{ B : \"owns\""),
                (14, "ERR-14  classDiagram method/attribute errors\n" +
                     "  Methods need parens: +run() void   Attributes do not: +String name"),
                (15, "ERR-15  Unquoted label in pie chart\n" +
                     "  BAD:  Slice A : 40             GOOD: \"Slice A\" : 40"),
                (16, "ERR-16  Accented/non-ASCII characters without quotes\n" +
                     "  BAD:  A[V\u00e9rif]                 GOOD: A[\"V\u00e9rif\"]"),
                (17, "ERR-17  Using stateDiagram (v1) instead of stateDiagram-v2\n" +
                     "  Prefer: stateDiagram-v2"),
                (18, "ERR-18  subgraph title not quoted when it contains spaces\n" +
                     "  BAD:  subgraph My Group        GOOD: subgraph id [\"My Group\"]"),
                (19, "ERR-19  Using // or # for comments\n" +
                     "  BAD:  // comment   # comment   GOOD: %% comment"),
                (20, "ERR-20  Mixing tabs and spaces (indentation errors)\n" +
                     "  Use only 4 spaces per indent level throughout."),
            };

            foreach (var (num, text) in errors)
            {
                if (ShouldIncludeError(type, num))
                {
                    sb.AppendLine(text);
                    sb.AppendLine();
                }
            }
        }

        // =========================================================================
        // Footer
        // =========================================================================

        private static void AppendFooter(StringBuilder sb)
        {
            sb.AppendLine("================================================================");
            sb.AppendLine("REMINDER: Wrap ONLY the corrected Mermaid code between:");
            sb.AppendLine("<<<MERMAID>>>");
            sb.AppendLine("... your corrected code ...");
            sb.AppendLine("<<<END>>>");
            sb.AppendLine("No explanations. No fences. Preserve all node labels and intent.");
            sb.AppendLine("================================================================");
        }
    }
}
