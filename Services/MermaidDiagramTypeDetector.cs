using System;

namespace MarkdownEditor.Services
{
    /// <summary>
    /// Detects the Mermaid diagram type from the first non-empty line of the code block.
    /// </summary>
    public static class MermaidDiagramTypeDetector
    {
        /// <summary>
        /// Returns the <see cref="MermaidDiagramType"/> for the given Mermaid source code.
        /// The first non-empty line (after optional front-matter / %%{init} directives)
        /// is matched against known diagram-type keywords.
        /// </summary>
        public static MermaidDiagramType Detect(string mermaidCode)
        {
            if (string.IsNullOrWhiteSpace(mermaidCode))
                return MermaidDiagramType.Unknown;

            foreach (var rawLine in mermaidCode.Split('\n'))
            {
                var line = rawLine.Trim();

                // Skip blank lines, comments, and %%{init}%% directives
                if (line.Length == 0) continue;
                if (line.StartsWith("%%", StringComparison.Ordinal)) continue;
                if (line.StartsWith("---", StringComparison.Ordinal)) continue; // front-matter fence

                // Normalise to lower-case for keyword matching
                var lower = line.ToLowerInvariant();

                if (lower.StartsWith("flowchart", StringComparison.Ordinal))      return MermaidDiagramType.Flowchart;
                if (lower.StartsWith("graph ", StringComparison.Ordinal)
                    || lower == "graph")                                            return MermaidDiagramType.Graph;
                if (lower.StartsWith("sequencediagram", StringComparison.Ordinal)) return MermaidDiagramType.Sequence;
                if (lower.StartsWith("classdiagram", StringComparison.Ordinal))    return MermaidDiagramType.Class;
                if (lower.StartsWith("statediagram-v2", StringComparison.Ordinal)
                    || lower.StartsWith("statediagram", StringComparison.Ordinal)) return MermaidDiagramType.State;
                if (lower.StartsWith("erdiagram", StringComparison.Ordinal))       return MermaidDiagramType.Er;
                if (lower.StartsWith("gantt", StringComparison.Ordinal))           return MermaidDiagramType.Gantt;
                if (lower.StartsWith("pie", StringComparison.Ordinal))             return MermaidDiagramType.Pie;
                if (lower.StartsWith("mindmap", StringComparison.Ordinal))         return MermaidDiagramType.Mindmap;
                if (lower.StartsWith("timeline", StringComparison.Ordinal))        return MermaidDiagramType.Timeline;
                if (lower.StartsWith("gitgraph", StringComparison.Ordinal))        return MermaidDiagramType.GitGraph;
                if (lower.StartsWith("quadrantchart", StringComparison.Ordinal))   return MermaidDiagramType.Quadrant;
                if (lower.StartsWith("xychart-beta", StringComparison.Ordinal))    return MermaidDiagramType.XyChart;
                if (lower.StartsWith("block-beta", StringComparison.Ordinal))      return MermaidDiagramType.Block;
                if (lower.StartsWith("requirementdiagram", StringComparison.Ordinal)) return MermaidDiagramType.Requirement;
                if (lower.StartsWith("c4context", StringComparison.Ordinal)
                    || lower.StartsWith("c4container", StringComparison.Ordinal)
                    || lower.StartsWith("c4component", StringComparison.Ordinal)
                    || lower.StartsWith("c4dynamic", StringComparison.Ordinal)
                    || lower.StartsWith("c4deployment", StringComparison.Ordinal)) return MermaidDiagramType.C4;
                if (lower.StartsWith("packet-beta", StringComparison.Ordinal))     return MermaidDiagramType.Packet;
                if (lower.StartsWith("architecture-beta", StringComparison.Ordinal)) return MermaidDiagramType.Architecture;
                if (lower.StartsWith("journey", StringComparison.Ordinal))         return MermaidDiagramType.Journey;
                if (lower.StartsWith("sankey-beta", StringComparison.Ordinal))     return MermaidDiagramType.Sankey;
                if (lower.StartsWith("kanban", StringComparison.Ordinal))          return MermaidDiagramType.Kanban;
                if (lower.StartsWith("radar-beta", StringComparison.Ordinal))      return MermaidDiagramType.Radar;
                if (lower.StartsWith("treemap-beta", StringComparison.Ordinal))    return MermaidDiagramType.Treemap;

                // First non-skipped line did not match — stop; return Unknown
                break;
            }

            return MermaidDiagramType.Unknown;
        }

        /// <summary>Returns a human-readable display name for a diagram type.</summary>
        public static string ToDisplayName(MermaidDiagramType type) => type switch
        {
            MermaidDiagramType.Flowchart     => "Flowchart",
            MermaidDiagramType.Graph         => "Graph (legacy flowchart)",
            MermaidDiagramType.Sequence      => "Sequence Diagram",
            MermaidDiagramType.Class         => "Class Diagram",
            MermaidDiagramType.State         => "State Diagram",
            MermaidDiagramType.Er            => "ER Diagram",
            MermaidDiagramType.Gantt         => "Gantt Chart",
            MermaidDiagramType.Pie           => "Pie Chart",
            MermaidDiagramType.Mindmap       => "Mindmap",
            MermaidDiagramType.Timeline      => "Timeline",
            MermaidDiagramType.GitGraph      => "Git Graph",
            MermaidDiagramType.Quadrant      => "Quadrant Chart",
            MermaidDiagramType.XyChart       => "XY Chart",
            MermaidDiagramType.Block         => "Block Diagram",
            MermaidDiagramType.Requirement   => "Requirement Diagram",
            MermaidDiagramType.C4            => "C4 Diagram",
            MermaidDiagramType.Packet        => "Packet Diagram",
            MermaidDiagramType.Architecture  => "Architecture Diagram",
            MermaidDiagramType.Journey       => "User Journey",
            MermaidDiagramType.Sankey        => "Sankey Diagram",
            MermaidDiagramType.Kanban        => "Kanban Board",
            MermaidDiagramType.Radar         => "Radar Chart",
            MermaidDiagramType.Treemap       => "Treemap",
            _                                => "Unknown / Unrecognised",
        };
    }
}
