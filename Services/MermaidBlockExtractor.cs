using System;
using System.Text.RegularExpressions;

namespace MarkdownEditor.Services
{
    /// <summary>
    /// Represents a Mermaid code block found inside a Markdown document.
    /// All offsets are character positions (0-based) within the full Markdown string.
    /// </summary>
    public sealed class MermaidBlock
    {
        /// <summary>Start offset of the opening fence line (e.g. the backtick characters).</summary>
        public int BlockStart { get; }
        /// <summary>Start offset of the first line of Mermaid content (after the opening fence line including its newline).</summary>
        public int ContentStart { get; }
        /// <summary>End offset of the last character of Mermaid content (exclusive; equals the start of the closing fence).</summary>
        public int ContentEnd { get; }
        /// <summary>End offset after the closing fence line (exclusive; suitable for replacement).</summary>
        public int BlockEnd { get; }
        /// <summary>The raw Mermaid source code (between the fences, not including fences).</summary>
        public string Content { get; }
        /// <summary>Diagram type detected from the content.</summary>
        public MermaidDiagramType DiagramType { get; }

        internal MermaidBlock(int blockStart, int contentStart, int contentEnd, int blockEnd, string content)
        {
            BlockStart   = blockStart;
            ContentStart = contentStart;
            ContentEnd   = contentEnd;
            BlockEnd     = blockEnd;
            Content      = content;
            DiagramType  = MermaidDiagramTypeDetector.Detect(content);
        }
    }

    /// <summary>
    /// Utilities for locating and replacing Mermaid fenced blocks inside Markdown text.
    /// </summary>
    public static class MermaidBlockExtractor
    {
        // Matches: optional leading spaces (0-3), 3+ backticks or tildes, optional space, "mermaid" as a whole word
        private static readonly Regex s_fencePattern = new Regex(
            @"^(?<indent> {0,3})(?<fence>`{3,}|~{3,})[ \t]*mermaid[ \t]*$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        /// <summary>
        /// Finds the Mermaid code block that contains <paramref name="caretOffset"/>
        /// and returns a <see cref="MermaidBlock"/> with full position metadata, or
        /// <c>null</c> when the caret is not inside any Mermaid block.
        /// </summary>
        public static MermaidBlock? TryExtract(string markdown, int caretOffset)
        {
            if (string.IsNullOrEmpty(markdown)) return null;
            if (caretOffset < 0 || caretOffset > markdown.Length) return null;

            foreach (Match openMatch in s_fencePattern.Matches(markdown))
            {
                var fenceChars = openMatch.Groups["fence"].Value[0]; // ` or ~
                var fenceLen   = openMatch.Groups["fence"].Length;

                // Find end of opening fence line
                int openLineEnd = openMatch.Index + openMatch.Length;
                // Skip past the newline after the opening fence
                int contentStart = openLineEnd;
                if (contentStart < markdown.Length && markdown[contentStart] == '\r') contentStart++;
                if (contentStart < markdown.Length && markdown[contentStart] == '\n') contentStart++;

                // Build closing fence pattern: same fence char, at least same count
                var closingPattern = new string(fenceChars, fenceLen);

                // Search for the closing fence from contentStart
                int searchPos = contentStart;
                int contentEnd = -1;
                int blockEnd   = -1;

                while (searchPos < markdown.Length)
                {
                    int nl = markdown.IndexOf('\n', searchPos);
                    int lineStart = searchPos;
                    int lineEnd   = nl < 0 ? markdown.Length : nl + 1;
                    string line   = nl < 0 ? markdown.Substring(lineStart) : markdown.Substring(lineStart, nl - lineStart).TrimEnd('\r');

                    if (nl >= 0) searchPos = nl + 1;
                    else         searchPos = markdown.Length;

                    // Check if this line is a closing fence (0-3 optional leading spaces, then >= fenceLen fence chars, then nothing)
                    var trimmedLine = line.TrimStart(' ');
                    int leadingSpaces = line.Length - trimmedLine.Length;
                    if (leadingSpaces <= 3
                        && trimmedLine.Length >= fenceLen
                        && trimmedLine.TrimEnd(fenceChars).Length == 0
                        && trimmedLine.Length >= fenceLen)
                    {
                        contentEnd = lineStart;
                        blockEnd   = nl < 0 ? markdown.Length : nl + 1;
                        break;
                    }
                }

                if (contentEnd < 0) continue; // unclosed fence — skip

                int blockStart   = openMatch.Index;
                int blockFullEnd = blockEnd;

                // Is the caret inside this block (inclusive of fence lines)?
                if (caretOffset >= blockStart && caretOffset <= blockFullEnd)
                {
                    string content = markdown.Substring(contentStart, contentEnd - contentStart);
                    return new MermaidBlock(blockStart, contentStart, contentEnd, blockFullEnd, content);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a new Markdown string where the content of <paramref name="block"/>
        /// has been replaced with <paramref name="newContent"/>.
        /// The enclosing fence lines are preserved unchanged.
        /// </summary>
        public static string ReplaceBlockContent(string markdown, MermaidBlock block, string newContent)
        {
            if (markdown is null) throw new ArgumentNullException(nameof(markdown));
            if (block     is null) throw new ArgumentNullException(nameof(block));
            if (newContent is null) throw new ArgumentNullException(nameof(newContent));

            // Ensure the new content ends with a newline so the closing fence is on its own line
            if (!newContent.EndsWith("\n", StringComparison.Ordinal))
                newContent += "\n";

            return markdown.Substring(0, block.ContentStart)
                 + newContent
                 + markdown.Substring(block.ContentEnd);
        }
    }
}
