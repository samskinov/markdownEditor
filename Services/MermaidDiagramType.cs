namespace MarkdownEditor.Services
{
    /// <summary>
    /// Known Mermaid v11 diagram types, ordered by detection priority.
    /// </summary>
    public enum MermaidDiagramType
    {
        Unknown = 0,

        // ── Graph / Flowchart ──────────────────────────────────────────────────
        Flowchart,      // flowchart TD|LR|...
        Graph,          // graph TD|LR|...  (legacy alias)

        // ── Sequence ──────────────────────────────────────────────────────────
        Sequence,       // sequenceDiagram

        // ── Class ─────────────────────────────────────────────────────────────
        Class,          // classDiagram

        // ── State ─────────────────────────────────────────────────────────────
        State,          // stateDiagram-v2 | stateDiagram

        // ── ER ────────────────────────────────────────────────────────────────
        Er,             // erDiagram

        // ── Gantt ─────────────────────────────────────────────────────────────
        Gantt,          // gantt

        // ── Pie ───────────────────────────────────────────────────────────────
        Pie,            // pie

        // ── Mindmap ───────────────────────────────────────────────────────────
        Mindmap,        // mindmap

        // ── Timeline ──────────────────────────────────────────────────────────
        Timeline,       // timeline

        // ── GitGraph ──────────────────────────────────────────────────────────
        GitGraph,       // gitGraph

        // ── Quadrant ──────────────────────────────────────────────────────────
        Quadrant,       // quadrantChart

        // ── XY Chart ──────────────────────────────────────────────────────────
        XyChart,        // xychart-beta

        // ── Block ─────────────────────────────────────────────────────────────
        Block,          // block-beta

        // ── Requirement ───────────────────────────────────────────────────────
        Requirement,    // requirementDiagram

        // ── C4 ────────────────────────────────────────────────────────────────
        C4,             // C4Context|C4Container|C4Component|C4Dynamic|C4Deployment

        // ── Packet ────────────────────────────────────────────────────────────
        Packet,         // packet-beta

        // ── Architecture ──────────────────────────────────────────────────────
        Architecture,   // architecture-beta

        // ── Journey ───────────────────────────────────────────────────────────
        Journey,        // journey

        // ── Sankey ────────────────────────────────────────────────────────────
        Sankey,         // sankey-beta

        // ── Kanban ────────────────────────────────────────────────────────────
        Kanban,         // kanban

        // ── Radar ─────────────────────────────────────────────────────────────
        Radar,          // radar-beta

        // ── Treemap ───────────────────────────────────────────────────────────
        Treemap,        // treemap-beta
    }
}
