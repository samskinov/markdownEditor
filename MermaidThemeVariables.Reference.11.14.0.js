/**
 * Mermaid themeVariables reference for Mermaid 11.14.0 — GitHub Light theme.
 *
 * Colors are mapped from the GitHub Primer design system (Light mode).
 * See: https://primer.style/primitives/colors
 *
 * Only `theme: 'base'` supports `themeVariables` overrides.
 * This repository also embeds Mermaid 10.9.5 locally, so some keys below are
 * newer than the bundled script even though they exist in Mermaid 11.14.0.
 *
 * Usage:
 *   mermaid.initialize({
 *     theme: 'base',
 *     themeVariables: mermaidThemeVariablesReference,
 *   });
 *
 * Notes:
 * - `darkMode` is a Mermaid config key, not a declared base `themeVariables`
 *   property, so it is intentionally not listed here.
 * - `cScaleInv*`, `cScalePeer*`, and `cScaleLabel*` are included because they
 *   are consumed by Mermaid 11.14.0 at runtime for Treemap, even though they
 *   are not declared in `dist/themes/theme-base.d.ts`.
 *
 * Primer Light token reference (hex values):
 *   canvas.default  #ffffff   canvas.subtle   #f6f8fa
 *   fg.default      #24292f   fg.muted        #57606a   fg.subtle  #6e7781
 *   border.default  #d0d7de   border.muted    #d8dee4
 *   accent.*        blue-5 #0969da  / blue-0 #ddf4ff
 *   success.*       green-5 #1a7f37 / green-0 #dafbe1
 *   attention.*     yellow-5 #9a6700 / yellow-0 #fff8c5
 *   danger.*        red-5 #cf222e   / red-0 #ffebe9
 *   done.*          purple-5 #8250df / purple-0 #fbefff
 *   sponsors.*      pink-5 #bf3989  / pink-0 #ffeff7
 */

const mermaidThemeVariablesReference = {
  // ── Core / shared ─────────────────────────────────────────────────────────────
  // canvas.default
  background: '#ffffff',
  // accent.subtle (blue-0) — drives node fills across most diagrams
  primaryColor: '#ddf4ff',
  // attention.subtle (yellow-0) — note backgrounds
  noteBkgColor: '#fff8c5',
  // fg.default
  noteTextColor: '#24292f',
  THEME_COLOR_LIMIT: 12,
  // GitHub UI uses 6 px rounded corners
  radius: 6,
  strokeWidth: 1,
  // GitHub system font stack
  fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Helvetica, Arial, sans-serif',
  fontSize: '14px',
  // Flat appearance — no fill gradient, GitHub-style 1 px shadow
  useGradient: false,
  dropShadow: 'drop-shadow(0px 1px 2px rgba(31, 35, 40, 0.12))',
  // fg.default
  primaryTextColor: '#24292f',
  // success.subtle (green-0)
  secondaryColor: '#dafbe1',
  // attention.subtle (yellow-0)
  tertiaryColor: '#fff8c5',
  // accent.fg (blue-5)
  primaryBorderColor: '#0969da',
  // success.fg (green-5)
  secondaryBorderColor: '#1a7f37',
  // attention.fg (yellow-5)
  tertiaryBorderColor: '#9a6700',
  // attention.fg
  noteBorderColor: '#9a6700',
  // fg.muted
  secondaryTextColor: '#57606a',
  // fg.muted
  tertiaryTextColor: '#57606a',
  // fg.muted — neutral connectors / edges
  lineColor: '#57606a',
  arrowheadColor: '#57606a',
  // fg.default
  textColor: '#24292f',
  // border.default
  border2: '#d0d7de',
  // accent.subtle — generic node background
  nodeBkg: '#ddf4ff',
  mainBkg: '#ddf4ff',
  // accent.fg — generic node border
  nodeBorder: '#0969da',
  // canvas.subtle — subgraph / cluster background
  clusterBkg: '#f6f8fa',
  // border.default
  clusterBorder: '#d0d7de',
  // fg.muted
  defaultLinkColor: '#57606a',
  // fg.default
  titleColor: '#24292f',
  // canvas.subtle — edge label pill background
  edgeLabelBackground: '#f6f8fa',
  // fg.default
  nodeTextColor: '#24292f',

  // ── Sequence diagram ──────────────────────────────────────────────────────────
  // accent.fg
  actorBorder: '#0969da',
  // accent.subtle
  actorBkg: '#ddf4ff',
  // fg.default
  actorTextColor: '#24292f',
  // border.default — lifeline
  actorLineColor: '#d0d7de',
  // accent.subtle
  labelBoxBkgColor: '#ddf4ff',
  // fg.muted — arrows
  signalColor: '#57606a',
  // fg.default
  signalTextColor: '#24292f',
  // accent.fg
  labelBoxBorderColor: '#0969da',
  // fg.default
  labelTextColor: '#24292f',
  loopTextColor: '#24292f',
  // accent.fg
  activationBorderColor: '#0969da',
  // blue-1 — activation box fill
  activationBkgColor: '#b6e3ff',
  // fg.onEmphasis — sequence number badge text
  sequenceNumberColor: '#ffffff',

  // ── Gantt diagram ─────────────────────────────────────────────────────────────
  // accent.subtle — odd sections
  sectionBkgColor: '#ddf4ff',
  // canvas.subtle — even sections
  altSectionBkgColor: '#f6f8fa',
  // success.subtle — second section color variant
  sectionBkgColor2: '#dafbe1',
  // canvas.subtle — excluded periods (weekends etc.)
  excludeBkgColor: '#f6f8fa',
  // accent.fg
  taskBorderColor: '#0969da',
  // blue-3 — task bar fill
  taskBkgColor: '#54aeff',
  // blue-6 — active task border
  activeTaskBorderColor: '#0550ae',
  // blue-1 — active task fill
  activeTaskBkgColor: '#b6e3ff',
  // border.default — grid lines
  gridColor: '#d0d7de',
  // border.muted — completed task fill
  doneTaskBkgColor: '#d8dee4',
  // fg.disabled — completed task border
  doneTaskBorderColor: '#8c959f',
  // danger.fg — critical task border
  critBorderColor: '#cf222e',
  // danger.subtle — critical task fill
  critBkgColor: '#ffebe9',
  // danger.fg — today marker
  todayLineColor: '#cf222e',
  // fg.disabled — vertical grid line
  vertLineColor: '#8c959f',
  // fg.default
  taskTextColor: '#24292f',
  taskTextOutsideColor: '#24292f',
  // fg.onEmphasis — text on colored task bars
  taskTextLightColor: '#ffffff',
  // fg.default — text on light task bars
  taskTextDarkColor: '#24292f',
  // accent.fg — clickable task text
  taskTextClickableColor: '#0969da',

  // ── Shared typography weight knobs ────────────────────────────────────────────
  noteFontWeight: 'normal',
  fontWeight: 'normal',

  // ── C4 diagram (person nodes) ─────────────────────────────────────────────────
  // accent.fg
  personBorder: '#0969da',
  // accent.subtle
  personBkg: '#ddf4ff',

  // ── ER diagram (attribute row alternating fills) ───────────────────────────────
  // canvas.default
  rowOdd: '#ffffff',
  // canvas.subtle
  rowEven: '#f6f8fa',

  // ── State diagram ─────────────────────────────────────────────────────────────
  // fg.muted — transition arrows
  transitionColor: '#57606a',
  // fg.default
  transitionLabelColor: '#24292f',
  stateLabelColor: '#24292f',
  // accent.subtle — state node fill
  stateBkg: '#ddf4ff',
  labelBackgroundColor: '#ddf4ff',
  // canvas.default — composite state interior
  compositeBackground: '#ffffff',
  // canvas.subtle — alternate region background
  altBackground: '#f6f8fa',
  // canvas.subtle — composite state title bar
  compositeTitleBackground: '#f6f8fa',
  // border.default
  compositeBorder: '#d0d7de',
  // accent.fg — end state fill
  innerEndBackground: '#0969da',
  // danger.subtle — error state fill
  errorBkgColor: '#ffebe9',
  // danger.fg — error state text
  errorTextColor: '#cf222e',
  // fg.muted — special state (choice, fork, join) color
  specialStateColor: '#57606a',

  // ── Treemap base palette (cScale0–11) ─────────────────────────────────────────
  // Subtle background tones — each maps to a GitHub Primer semantic color family.
  cScale0:  '#ddf4ff',   // blue-0    (accent.subtle)
  cScale1:  '#dafbe1',   // green-0   (success.subtle)
  cScale2:  '#fff8c5',   // yellow-0  (attention.subtle)
  cScale3:  '#ffebe9',   // red-0     (danger.subtle)
  cScale4:  '#fbefff',   // purple-0  (done.subtle)
  cScale5:  '#ffeff7',   // pink-0    (sponsors.subtle)
  cScale6:  '#fff0eb',   // coral-0
  cScale7:  '#fff1e5',   // orange-0
  cScale8:  '#b6e3ff',   // blue-1
  cScale9:  '#aceebb',   // green-1
  cScale10: '#fae17d',   // yellow-1
  cScale11: '#ecd8ff',   // purple-1
  // fg.default
  scaleLabelColor: '#24292f',

  // ── Treemap runtime palette (Mermaid 11.14.0, not in theme-base.d.ts) ─────────
  // cScaleInv*: high-contrast readable text on the corresponding cScale background.
  cScaleInv0:  '#0550ae',   // blue-6  — on blue-0
  cScaleInv1:  '#116329',   // green-6 — on green-0
  cScaleInv2:  '#7d4e00',   // yellow-6 — on yellow-0
  cScaleInv3:  '#a40e26',   // red-6  — on red-0
  cScaleInv4:  '#6639ba',   // purple-6 — on purple-0
  cScaleInv5:  '#99286e',   // pink-6  — on pink-0
  cScaleInv6:  '#9e2f1c',   // coral-6 — on coral-0
  cScaleInv7:  '#953800',   // orange-6 — on orange-0
  cScaleInv8:  '#033d8b',   // blue-7  — on blue-1
  cScaleInv9:  '#044f1e',   // green-7 — on green-1
  cScaleInv10: '#633c01',   // yellow-7 — on yellow-1
  cScaleInv11: '#512a97',   // purple-7 — on purple-1

  // cScalePeer*: one step more saturated than cScale, used for peer/sibling nodes.
  cScalePeer0:  '#b6e3ff',  // blue-1
  cScalePeer1:  '#aceebb',  // green-1
  cScalePeer2:  '#fae17d',  // yellow-1
  cScalePeer3:  '#ffcecb',  // red-1
  cScalePeer4:  '#ecd8ff',  // purple-1
  cScalePeer5:  '#ffd3eb',  // pink-1
  cScalePeer6:  '#ffd6cc',  // coral-1
  cScalePeer7:  '#ffd8b5',  // orange-1
  cScalePeer8:  '#80ccff',  // blue-2
  cScalePeer9:  '#6fdd8b',  // green-2
  cScalePeer10: '#eac54f',  // yellow-2
  cScalePeer11: '#d8b9ff',  // purple-2

  // cScaleLabel*: label text on the cScale backgrounds (same as cScaleInv).
  cScaleLabel0:  '#0550ae',
  cScaleLabel1:  '#116329',
  cScaleLabel2:  '#7d4e00',
  cScaleLabel3:  '#a40e26',
  cScaleLabel4:  '#6639ba',
  cScaleLabel5:  '#99286e',
  cScaleLabel6:  '#9e2f1c',
  cScaleLabel7:  '#953800',
  cScaleLabel8:  '#033d8b',
  cScaleLabel9:  '#044f1e',
  cScaleLabel10: '#633c01',
  cScaleLabel11: '#512a97',

  // ── Class diagram ─────────────────────────────────────────────────────────────
  // fg.default
  classText: '#24292f',
  // fillType0–7 map to the same Primer semantic families as cScale0–7.
  fillType0: '#ddf4ff',   // blue-0
  fillType1: '#dafbe1',   // green-0
  fillType2: '#fff8c5',   // yellow-0
  fillType3: '#ffebe9',   // red-0
  fillType4: '#fbefff',   // purple-0
  fillType5: '#ffeff7',   // pink-0
  fillType6: '#fff0eb',   // coral-0
  fillType7: '#fff1e5',   // orange-0

  // ── Pie / Donut chart ──────────────────────────────────────────────────────────
  // Mid-saturation GitHub palette colors for distinct, accessible segments.
  pie1:  '#218bff',   // blue-4
  pie2:  '#2da44e',   // green-4
  pie3:  '#d4a72c',   // yellow-3
  pie4:  '#fa4549',   // red-4
  pie5:  '#a475f9',   // purple-4
  pie6:  '#e85aad',   // pink-4
  pie7:  '#ec6547',   // coral-4
  pie8:  '#fb8f44',   // orange-3
  pie9:  '#4ac26b',   // green-3
  pie10: '#54aeff',   // blue-3
  pie11: '#c297ff',   // purple-3
  pie12: '#eac54f',   // yellow-2
  pieTitleTextSize:    '16px',
  pieTitleTextColor:   '#24292f',
  pieSectionTextSize:  '13px',
  // fg.onEmphasis — white text visible on all mid-saturation segment colors
  pieSectionTextColor: '#ffffff',
  pieLegendTextSize:   '14px',
  pieLegendTextColor:  '#24292f',
  // canvas.default — crisp segment separators
  pieStrokeColor:      '#ffffff',
  pieStrokeWidth:      '2px',
  pieOuterStrokeWidth: '2px',
  // border.default — outer ring
  pieOuterStrokeColor: '#d0d7de',
  pieOpacity:          '0.9',

  // ── Venn diagram ──────────────────────────────────────────────────────────────
  venn1: '#218bff',   // blue-4
  venn2: '#2da44e',   // green-4
  venn3: '#d4a72c',   // yellow-3
  venn4: '#fa4549',   // red-4
  venn5: '#a475f9',   // purple-4
  venn6: '#e85aad',   // pink-4
  venn7: '#ec6547',   // coral-4
  venn8: '#fb8f44',   // orange-3
  // fg.default
  vennTitleTextColor: '#24292f',
  vennSetTextColor:   '#24292f',

  // ── Radar chart ───────────────────────────────────────────────────────────────
  radar: {
    // fg.muted — axis lines
    axisColor:           '#57606a',
    axisStrokeWidth:     1,
    axisLabelFontSize:   12,
    // Subtle area fill opacity
    curveOpacity:        0.3,
    curveStrokeWidth:    2,
    // border.default — grid rings
    graticuleColor:      '#d0d7de',
    graticuleStrokeWidth: 1,
    graticuleOpacity:    0.5,
    legendBoxSize:       12,
    legendFontSize:      12,
  },

  // ── Architecture diagram ───────────────────────────────────────────────────────
  // fg.muted
  archEdgeColor:        '#57606a',
  archEdgeArrowColor:   '#57606a',
  archEdgeWidth:        '2',
  // border.default — group bounding box
  archGroupBorderColor: '#d0d7de',
  archGroupBorderWidth: '2px',

  // ── Quadrant chart ─────────────────────────────────────────────────────────────
  // Each quadrant uses a distinct GitHub semantic subtle color.
  quadrant1Fill: '#ddf4ff',   // blue-0    (top-right)
  quadrant2Fill: '#dafbe1',   // green-0   (top-left)
  quadrant3Fill: '#fff8c5',   // yellow-0  (bottom-left)
  quadrant4Fill: '#f6f8fa',   // canvas.subtle (bottom-right)
  // fg.default
  quadrant1TextFill: '#24292f',
  quadrant2TextFill: '#24292f',
  quadrant3TextFill: '#24292f',
  quadrant4TextFill: '#24292f',
  // accent.fg — data point dots
  quadrantPointFill:                '#0969da',
  // fg.default
  quadrantPointTextFill:            '#24292f',
  // fg.muted — axis labels
  quadrantXAxisTextFill:            '#57606a',
  quadrantYAxisTextFill:            '#57606a',
  // border.default — internal and external grid lines
  quadrantInternalBorderStrokeFill: '#d0d7de',
  quadrantExternalBorderStrokeFill: '#d0d7de',
  // fg.default — chart title
  quadrantTitleFill:                '#24292f',

  // ── XY chart ──────────────────────────────────────────────────────────────────
  xyChart: {
    // canvas.default
    backgroundColor: '#ffffff',
    // fg.default
    titleColor:       '#24292f',
    // fg.muted — data point labels
    dataLabelColor:   '#57606a',
    // fg.muted — axis titles and labels
    xAxisTitleColor:  '#57606a',
    xAxisLabelColor:  '#57606a',
    // border.default — tick marks and axis line
    xAxisTickColor:   '#d0d7de',
    xAxisLineColor:   '#d0d7de',
    yAxisTitleColor:  '#57606a',
    yAxisLabelColor:  '#57606a',
    yAxisTickColor:   '#d0d7de',
    yAxisLineColor:   '#d0d7de',
    // Mid-saturation GitHub palette — 10 distinct series colors.
    plotColorPalette: '#218bff,#2da44e,#d4a72c,#fa4549,#a475f9,#e85aad,#ec6547,#fb8f44,#4ac26b,#54aeff',
  },

  // ── Requirement diagram ────────────────────────────────────────────────────────
  // accent.subtle
  requirementBackground:  '#ddf4ff',
  // accent.fg
  requirementBorderColor: '#0969da',
  requirementBorderSize:  '1',
  // fg.default
  requirementTextColor:   '#24292f',

  // ── Relationship diagram ───────────────────────────────────────────────────────
  // fg.muted — connector lines
  relationColor:           '#57606a',
  // canvas.subtle — label background pill
  relationLabelBackground: '#f6f8fa',
  // fg.default
  relationLabelColor:      '#24292f',

  // ── GitGraph diagram ───────────────────────────────────────────────────────────
  // Branch stripe colors follow GitHub's semantic emphasis palette.
  git0: '#0969da',   // blue-5    (main / HEAD)
  git1: '#1a7f37',   // green-5
  git2: '#8250df',   // purple-5
  git3: '#cf222e',   // red-5
  git4: '#bf3989',   // pink-5
  git5: '#9a6700',   // yellow-5
  git6: '#bc4c00',   // orange-5
  git7: '#57606a',   // fg.muted  (gray)
  // fg.onEmphasis — white commit-node labels on colored branch stripes
  gitInv0: '#ffffff',
  gitInv1: '#ffffff',
  gitInv2: '#ffffff',
  gitInv3: '#ffffff',
  gitInv4: '#ffffff',
  gitInv5: '#ffffff',
  gitInv6: '#ffffff',
  gitInv7: '#ffffff',
  // fg.default — branch label text
  branchLabelColor: '#24292f',
  // fg.onEmphasis — text inside colored branch pill badges
  gitBranchLabel0: '#ffffff',
  gitBranchLabel1: '#ffffff',
  gitBranchLabel2: '#ffffff',
  gitBranchLabel3: '#ffffff',
  gitBranchLabel4: '#ffffff',
  gitBranchLabel5: '#ffffff',
  gitBranchLabel6: '#ffffff',
  gitBranchLabel7: '#ffffff',
  // fg.default — tag label text
  tagLabelColor:      '#24292f',
  // attention.subtle — tag label background
  tagLabelBackground: '#fff8c5',
  // attention.fg — tag label border
  tagLabelBorder:     '#9a6700',
  tagLabelFontSize:   '10px',
  // fg.default — commit SHA label text
  commitLabelColor:      '#24292f',
  // canvas.subtle — commit label background pill
  commitLabelBackground: '#f6f8fa',
  commitLabelFontSize:   '10px',

  // ── ER attribute-row background colors ────────────────────────────────────────
  // canvas.default / canvas.subtle — alternating attribute rows
  attributeBackgroundColorOdd:  '#ffffff',
  attributeBackgroundColorEven: '#f6f8fa',

  // ── Gradient stops (Mindmap, Timeline, GitGraph decorative gradients) ──────────
  // Blue → Purple matches GitHub's own brand gradient.
  gradientStart: '#0969da',   // accent.fg  (blue-5)
  gradientStop:  '#8250df',   // done.fg    (purple-5)
};