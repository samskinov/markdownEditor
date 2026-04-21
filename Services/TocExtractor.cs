using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MarkdownEditor.Services
{
    public sealed class TocEntry
    {
        public int Level { get; }
        public string Title { get; }
        public int LineNumber { get; }

        public TocEntry(int level, string title, int lineNumber)
        {
            Level = level;
            Title = title;
            LineNumber = lineNumber;
        }
    }

    public static class TocExtractor
    {
        private static readonly Regex HeadingRegex = new Regex(
            @"^(?<hashes>#{1,6})\s+(?<title>.+)$",
            RegexOptions.Multiline | RegexOptions.Compiled);

        public static IReadOnlyList<TocEntry> Extract(string markdown)
        {
            var entries = new List<TocEntry>();
            if (string.IsNullOrEmpty(markdown)) return entries;

            var lineNumber = 1;
            var lines = markdown.Split('\n');

            foreach (var line in lines)
            {
                var trimmed = line.TrimStart();
                if (trimmed.Length > 0 && trimmed[0] == '#')
                {
                    int level = 0;
                    while (level < trimmed.Length && trimmed[level] == '#') level++;
                    if (level <= 6 && level < trimmed.Length && trimmed[level] == ' ')
                    {
                        var title = trimmed.Substring(level).Trim();
                        if (title.Length > 0)
                        {
                            entries.Add(new TocEntry(level, title, lineNumber));
                        }
                    }
                }
                lineNumber++;
            }

            return entries;
        }
    }
}
