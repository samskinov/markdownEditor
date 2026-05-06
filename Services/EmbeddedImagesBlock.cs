using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MarkdownEditor.Services
{
    /// <summary>
    /// Manages the reference-style image block that is stored at the bottom of
    /// a markdown document between the following delimiters:
    ///
    ///   &lt;!-- embedded-images:start --&gt;
    ///   [img-1]: data:image/webp;base64,...
    ///   [img-2]: data:image/webp;base64,...
    ///   &lt;!-- embedded-images:end --&gt;
    /// </summary>
    public static class EmbeddedImagesBlock
    {
        private const string StartMarker = "<!-- embedded-images:start -->";
        private const string EndMarker   = "<!-- embedded-images:end -->";

        private static readonly Regex EntryPattern = new(
            @"^\[img-(\d+)\]:\s*(data:image/[^\s]+)$",
            RegexOptions.Compiled);

        // -----------------------------------------------------------------------
        // Parse
        // -----------------------------------------------------------------------

        /// <summary>
        /// Returns (existingDataUriById, blockStartIndex, blockEndIndex).
        /// Indices are character offsets in the source string — blockStart is the
        /// index of the first character of the start marker; blockEnd is the index
        /// ONE PAST the last character of the end marker.
        /// If no block exists, returns an empty dictionary and both indices = -1.
        /// </summary>
        public static (Dictionary<string, string> images, int blockStart, int blockEnd)
            Parse(string markdown)
        {
            var images = new Dictionary<string, string>(StringComparer.Ordinal);

            int blockStart = markdown.IndexOf(StartMarker, StringComparison.Ordinal);
            if (blockStart < 0)
                return (images, -1, -1);

            int endMarkerPos = markdown.IndexOf(EndMarker,
                blockStart + StartMarker.Length, StringComparison.Ordinal);
            if (endMarkerPos < 0)
                return (images, -1, -1);

            int blockEnd = endMarkerPos + EndMarker.Length;

            // Parse lines between the two markers.
            string inner = markdown.Substring(
                blockStart + StartMarker.Length,
                endMarkerPos - (blockStart + StartMarker.Length));

            foreach (var rawLine in inner.Split('\n'))
            {
                var trimmed = rawLine.Trim();
                if (string.IsNullOrEmpty(trimmed)) continue;

                // [img-N]: data:image/<type>;base64,<base64>
                var m = EntryPattern.Match(trimmed);
                if (m.Success)
                    images["img-" + m.Groups[1].Value] = m.Groups[2].Value;
            }

            return (images, blockStart, blockEnd);
        }

        // -----------------------------------------------------------------------
        // Upsert
        // -----------------------------------------------------------------------

        /// <summary>
        /// Appends or updates one entry identified by <paramref name="id"/> and
        /// returns the updated full markdown string.
        /// If the block doesn't exist it is created at the end, preceded by exactly
        /// one blank line.
        /// </summary>
        public static string Upsert(string markdown, string id, string dataUri)
        {
            var (images, blockStart, blockEnd) = Parse(markdown);
            images[id] = dataUri;

            string newBlock = BuildBlock(images);

            if (blockStart >= 0)
            {
                // Replace the existing block (markers included) in-place.
                return markdown.Substring(0, blockStart)
                     + newBlock
                     + markdown.Substring(blockEnd);
            }
            else
            {
                // Append after a blank line.
                string trimmed = markdown.TrimEnd();
                return trimmed + "\n\n" + newBlock;
            }
        }

        // -----------------------------------------------------------------------
        // NextId
        // -----------------------------------------------------------------------

        /// <summary>
        /// Generates the next unused id in the "img-{N}" series (N ≥ 1).
        /// </summary>
        public static string NextId(IReadOnlyDictionary<string, string> existing)
        {
            int n = 1;
            while (existing.ContainsKey("img-" + n))
                n++;
            return "img-" + n;
        }

        // -----------------------------------------------------------------------
        // Helpers
        // -----------------------------------------------------------------------

        private static string BuildBlock(Dictionary<string, string> images)
        {
            var sb = new StringBuilder();
            sb.Append(StartMarker).Append('\n');

            foreach (var kvp in SortedImages(images))
                sb.Append('[').Append(kvp.Key).Append("]: ").Append(kvp.Value).Append('\n');

            sb.Append(EndMarker).Append('\n');
            return sb.ToString();
        }

        private static IEnumerable<KeyValuePair<string, string>> SortedImages(
            Dictionary<string, string> images)
        {
            var list = new List<KeyValuePair<string, string>>(images);
            list.Sort((a, b) =>
            {
                int na = TryParseN(a.Key);
                int nb = TryParseN(b.Key);
                return na.CompareTo(nb);
            });
            return list;
        }

        private static int TryParseN(string id)
        {
            // id is "img-{N}"
            if (id.StartsWith("img-", StringComparison.Ordinal) &&
                int.TryParse(id.Substring(4), out int n))
                return n;
            return int.MaxValue;
        }
    }
}
