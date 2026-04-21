using System.Collections.Generic;
using MarkdownEditor.Models;

namespace MarkdownEditor.Services
{
    public static class MarkdownSnippetProvider
    {
        public static IReadOnlyList<MarkdownSnippet> GetSnippets()
        {
            return
            [
                new("Titre H1", "Titre de niveau 1", "# Titre principal\n"),
                new("Titre H2", "Titre de niveau 2", "## Sous-titre\n"),
                new("Titre H3", "Titre de niveau 3", "### Section\n"),
                new("Gras", "Texte en gras", "**texte en gras**"),
                new("Italique", "Texte en italique", "*texte en italique*"),
                new("Gras + Italique", "Texte gras et italique", "***texte gras italique***"),
                new("Barré", "Texte barré", "~~texte barré~~"),
                new("Liste à puces", "Liste non ordonnée",
                    "- Élément 1\n- Élément 2\n- Élément 3\n"),
                new("Liste numérotée", "Liste ordonnée",
                    "1. Premier\n2. Deuxième\n3. Troisième\n"),
                new("Checklist", "Liste de tâches",
                    "- [x] Tâche terminée\n- [ ] Tâche à faire\n- [ ] Autre tâche\n"),
                new("Lien", "Hyperlien", "[Texte du lien](https://example.com)"),
                new("Image", "Image embarquée", "![Texte alternatif](https://example.com/image.png)"),
                new("Citation", "Bloc de citation",
                    "> Ceci est une citation.\n> Elle peut contenir plusieurs lignes.\n"),
                new("Code inline", "Code dans le texte", "`code inline`"),
                new("Bloc de code", "Bloc de code avec coloration",
                    "```csharp\npublic class Exemple\n{\n    public void Method()\n    {\n        Console.WriteLine(\"Hello\");\n    }\n}\n```\n"),
                new("Tableau", "Tableau Markdown",
                    "| Colonne 1 | Colonne 2 | Colonne 3 |\n|-----------|-----------|----------|\n| Cellule   | Cellule   | Cellule  |\n| Cellule   | Cellule   | Cellule  |\n"),
                new("Séparateur", "Ligne horizontale", "\n---\n"),
                new("Bloc Mermaid", "Diagramme Mermaid",
                    "```mermaid\ngraph TD\n    A[Début] --> B[Fin]\n```\n"),
                new("Note / Admonition", "Bloc de note importante",
                    "> **Note :** Ceci est une remarque importante.\n"),
            ];
        }
    }
}
