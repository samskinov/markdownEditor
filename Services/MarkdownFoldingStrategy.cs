using System;
using System.Collections.Generic;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Folding;

namespace MarkdownEditor.Services
{
    public class MarkdownFoldingStrategy
    {
        public void UpdateFoldings(FoldingManager manager, TextDocument document)
        {
            var (newFoldings, embedStartOffset) = CreateFoldings(document);
            manager.UpdateFoldings(newFoldings, -1);

            // Tag the embedded-images fold so its identity survives future updates.
            if (embedStartOffset >= 0)
            {
                foreach (var fold in manager.AllFoldings)
                {
                    if (fold.StartOffset == embedStartOffset)
                    {
                        fold.Tag = "embedded-images";
                        break;
                    }
                }
            }
        }

        private static (List<NewFolding> foldings, int embedStartOffset) CreateFoldings(TextDocument document)
        {
            var foldings = new List<NewFolding>();
            var headingStarts = new Stack<int>();
            var headingLevels = new Stack<int>();

            for (int i = 1; i <= document.LineCount; i++)
            {
                var line = document.GetLineByNumber(i);
                var text = document.GetText(line);

                if (string.IsNullOrWhiteSpace(text)) continue;

                int level = GetHeadingLevel(text);
                if (level <= 0) continue;

                while (headingLevels.Count > 0 && headingLevels.Peek() >= level)
                {
                    var startLine = headingStarts.Pop();
                    headingLevels.Pop();

                    if (startLine < line.LineNumber - 1)
                    {
                        var foldEnd = document.GetLineByNumber(line.LineNumber - 1);
                        var headingLine = document.GetLineByNumber(startLine);
                        var title = document.GetText(headingLine).TrimStart('#', ' ');
                        if (title.Length > 40) title = title.Substring(0, 40) + "…";
                        foldings.Add(new NewFolding(headingLine.Offset, foldEnd.Offset + foldEnd.Length)
                        {
                            Name = title
                        });
                    }
                }

                headingStarts.Push(i);
                headingLevels.Push(level);
            }

            while (headingStarts.Count > 0)
            {
                var startLine = headingStarts.Pop();
                headingLevels.Pop();

                if (startLine < document.LineCount)
                {
                    var lastLine = document.GetLineByNumber(document.LineCount);
                    var headingLine = document.GetLineByNumber(startLine);
                    var title = document.GetText(headingLine).TrimStart('#', ' ');
                    if (title.Length > 40) title = title.Substring(0, 40) + "…";
                    foldings.Add(new NewFolding(headingLine.Offset, lastLine.Offset + lastLine.Length)
                    {
                        Name = title
                    });
                }
            }

            for (int i = 1; i <= document.LineCount; i++)
            {
                var line = document.GetLineByNumber(i);
                var text = document.GetText(line);
                var trimmed = text.TrimStart();

                if (trimmed.StartsWith("```"))
                {
                    var startLine = line;
                    bool foundEnd = false;

                    for (int j = i + 1; j <= document.LineCount; j++)
                    {
                        var innerLine = document.GetLineByNumber(j);
                        var innerText = document.GetText(innerLine).TrimStart();
                        if (innerText.StartsWith("```"))
                        {
                            var lang = trimmed.Length > 3 ? trimmed.Substring(3).Trim() : "code";
                            foldings.Add(new NewFolding(startLine.Offset, innerLine.Offset + innerLine.Length)
                            {
                                Name = $">{lang}"
                            });
                            foundEnd = true;
                            i = j;
                            break;
                        }
                    }

                    if (!foundEnd) break;
                }
            }

            // ── Embedded-images block fold ─────────────────────────────────
            int embedStartOffset = -1;
            var docText = document.Text;
            var (images, blockStart, blockEnd) = EmbeddedImagesBlock.Parse(docText);

            if (blockStart >= 0 && blockEnd > 0 && blockEnd <= document.TextLength)
            {
                var startLine = document.GetLineByOffset(blockStart);
                var endLine   = document.GetLineByOffset(blockEnd - 1);

                // Approximate decoded size: base64 length × 0.75.
                long totalBytes = 0;
                foreach (var uri in images.Values)
                {
                    int comma = uri.IndexOf(',');
                    if (comma >= 0)
                        totalBytes += (long)((uri.Length - comma - 1) * 0.75);
                }

                int    count  = images.Count;
                string plural = count == 1 ? "" : "s";
                string szStr  = ImageEmbedder.FormatSize(totalBytes);
                string title  = $"\U0001F5BC  embedded-images  ({count} image{plural}, {szStr})";

                embedStartOffset = startLine.Offset;
                foldings.Add(new NewFolding(startLine.Offset, endLine.Offset + endLine.Length)
                {
                    Name          = title,
                    DefaultClosed = true
                });
            }

            foldings.Sort((a, b) => a.StartOffset.CompareTo(b.StartOffset));
            return (foldings, embedStartOffset);
        }

        private static int GetHeadingLevel(string line)
        {
            var trimmed = line.TrimStart();
            if (trimmed.Length == 0 || trimmed[0] != '#') return 0;
            int level = 0;
            while (level < trimmed.Length && trimmed[level] == '#') level++;
            if (level > 6) return 0;
            if (level < trimmed.Length && trimmed[level] != ' ') return 0;
            return level;
        }
    }
}
