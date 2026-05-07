using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace MarkdownEditor.Services
{
    public static class HtmlTemplateService
    {
        private const string LiveTemplate = @"<!DOCTYPE html>
<html lang=""en"">
<head>
<meta charset=""UTF-8"">
<meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
<title>Markdown Preview</title>
<link rel=""preconnect"" href=""https://fonts.googleapis.com"">
<link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin>
<link href=""https://fonts.googleapis.com/css2?family=Inter:ital,wght@0,400;0,500;0,600;1,400&display=swap"" rel=""stylesheet"">
<style>
:root {
    /* GitHub Primer Light — https://primer.style/primitives/colors */
    --bg: #ffffff;                     /* canvas.default */
    --text: #24292f;                   /* fg.default */
    --text-secondary: #57606a;         /* fg.muted */
    --border: #d0d7de;                 /* border.default */
    --code-bg: #f6f8fa;                /* canvas.subtle */
    --blockquote-border: #0969da;      /* accent.fg */
    --blockquote-bg: #ddf4ff;          /* accent.subtle */
    --link: #0969da;                   /* accent.fg */
    --table-header-bg: #f6f8fa;        /* canvas.subtle */
    --table-border: #d0d7de;           /* border.default */
    --hr: #d0d7de;                     /* border.default */
    --inline-code-bg: #f6f8fa;         /* canvas.subtle */
    --inline-code-text: #cf222e;       /* danger.fg */
    --checkbox-accent: #0969da;        /* accent.fg */
    --heading-color: #1f2328;          /* fg.default (strong) */
    --shadow: rgba(31,35,40,0.06);     /* GitHub-style shadow */
}

* { margin: 0; padding: 0; box-sizing: border-box; }

body {
    font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Helvetica, Arial, sans-serif;
    font-size: 16px;
    line-height: 1.6;
    color: var(--text);
    background: var(--bg);
    padding: 28px 36px;
    max-width: 900px;
    margin: 0 auto;
    -webkit-font-smoothing: antialiased;
    font-feature-settings: 'calt' 1, 'kern' 1;
}

h1, h2, h3, h4, h5, h6 {
    color: var(--heading-color);
    margin-top: 1.5em;
    margin-bottom: 0.5em;
    font-weight: 600;
    line-height: 1.25;
    letter-spacing: -0.02em;
}

h1 { font-size: 2em; border-bottom: 2px solid var(--border); padding-bottom: 0.3em; }
h2 { font-size: 1.5em; border-bottom: 1px solid var(--border); padding-bottom: 0.25em; }
h3 { font-size: 1.25em; }
h4 { font-size: 1.1em; }

p { margin-bottom: 1em; }

a { color: var(--link); text-decoration: none; border-bottom: 1px solid transparent; transition: border-color 0.2s; }
a:hover { border-bottom-color: var(--link); }

strong { font-weight: 700; }
em { font-style: italic; }

code {
    font-family: 'Cascadia Code', 'Fira Code', 'JetBrains Mono', 'Consolas', monospace;
    font-size: 0.88em;
    background: var(--inline-code-bg);
    color: var(--inline-code-text);
    padding: 0.15em 0.4em;
    border-radius: 4px;
}

pre {
    background: var(--code-bg);
    border: 1px solid var(--border);
    border-radius: 8px;
    padding: 16px 20px;
    overflow-x: auto;
    margin-bottom: 1.2em;
    box-shadow: 0 1px 3px var(--shadow);
}

pre code {
    background: none;
    color: var(--text);
    padding: 0;
    font-size: 0.88em;
    line-height: 1.6;
}

blockquote {
    border-left: 4px solid var(--blockquote-border);
    background: var(--blockquote-bg);
    padding: 12px 20px;
    margin: 0 0 1.2em 0;
    border-radius: 0 8px 8px 0;
    color: var(--text-secondary);
    font-style: italic;
}

blockquote p { margin-bottom: 0.4em; }
blockquote p:last-child { margin-bottom: 0; }

ul, ol { margin-bottom: 1em; padding-left: 1.8em; }
li { margin-bottom: 0.3em; }
li > ul, li > ol { margin-bottom: 0; }

ul.contains-task-list { list-style: none; padding-left: 0.5em; }
ul.contains-task-list li { position: relative; padding-left: 1.6em; }
input[type=""checkbox""] { accent-color: var(--checkbox-accent); margin-right: 0.5em; transform: scale(1.15); position: absolute; left: 0; top: 0.35em; }

table {
    width: 100%;
    border-collapse: collapse;
    margin-bottom: 1.2em;
    border-radius: 8px;
    overflow: hidden;
    box-shadow: 0 1px 3px var(--shadow);
}

th, td {
    padding: 10px 14px;
    text-align: left;
    border: 1px solid var(--table-border);
    font-size: 0.93em;
}

th {
    background: var(--table-header-bg);
    font-weight: 600;
    color: var(--heading-color);
}

tr:nth-child(even) td { background: var(--code-bg); }

hr {
    border: none;
    height: 2px;
    background: var(--hr);
    margin: 2em 0;
    border-radius: 1px;
}

img {
    max-width: 100%;
    height: auto;
    border-radius: 8px;
    margin: 0.8em 0;
    box-shadow: 0 2px 8px var(--shadow);
}

.mermaid {
    display: flex;
    justify-content: center;
    margin: 1.2em 0;
    padding: 16px;
    background: var(--code-bg);
    border: 1px solid var(--border);
    border-radius: 8px;
    overflow-x: auto;
    page-break-inside: avoid;
    break-inside: avoid;
}

.mermaid svg {
    max-width: 100%;
    width: auto !important;
    height: auto !important;
    page-break-inside: avoid;
    break-inside: avoid;
}

#live-indicator {
    position: fixed;
    bottom: 8px;
    right: 12px;
    font-size: 11px;
    color: #9496a1;
    opacity: 0.6;
}

/* ── TOC Sidebar ──────────────────────────────────────────── */
#toc-toggle {
    position: fixed;
    top: 16px;
    left: 16px;
    z-index: 1001;
    width: 40px;
    height: 40px;
    border-radius: 50%;
    border: 1px solid rgba(0,0,0,0.08);
    background: rgba(255,255,255,0.85);
    backdrop-filter: blur(8px);
    -webkit-backdrop-filter: blur(8px);
    cursor: pointer;
    display: flex;
    align-items: center;
    justify-content: center;
    font-size: 16px;
    color: #57606a;
    box-shadow: 0 1px 4px rgba(31,35,40,0.12);
    transition: transform 0.2s, background 0.2s, box-shadow 0.2s;
}
#toc-toggle:hover {
    transform: scale(1.08);
    background: rgba(255,255,255,0.95);
    box-shadow: 0 2px 8px rgba(31,35,40,0.16);
}

#toc-sidebar {
    position: fixed;
    top: 0;
    left: 0;
    z-index: 1000;
    width: 280px;
    height: 100vh;
    background: rgba(255,255,255,0.82);
    backdrop-filter: blur(16px) saturate(180%);
    -webkit-backdrop-filter: blur(16px) saturate(180%);
    border-right: 1px solid rgba(0,0,0,0.08);
    box-shadow: 2px 0 16px rgba(31,35,40,0.08);
    transform: translateX(-100%);
    transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
    overflow-y: auto;
    overflow-x: hidden;
    overscroll-behavior: contain;
    padding-bottom: 24px;
}
#toc-sidebar.open {
    transform: translateX(0);
}

#toc-header {
    padding: 16px 16px 8px;
    font-size: 11px;
    font-weight: 700;
    letter-spacing: 0.08em;
    text-transform: uppercase;
    color: #9496a1;
    display: flex;
    align-items: center;
    justify-content: space-between;
}
#toc-count {
    font-size: 11px;
    font-weight: 500;
    color: #9496a1;
    background: rgba(0,0,0,0.04);
    padding: 2px 8px;
    border-radius: 10px;
}

#toc-list {
    list-style: none;
    padding: 0;
    margin: 0;
}
#toc-list a {
    display: block;
    padding: 5px 12px;
    font-size: 13px;
    line-height: 1.5;
    color: #57606a;
    text-decoration: none;
    border-left: 3px solid transparent;
    transition: background 0.15s, color 0.15s, border-color 0.15s;
    white-space: nowrap;
    overflow: hidden;
    text-overflow: ellipsis;
}
#toc-list a[data-level=""1""] { padding-left: 12px; border-left-color: rgba(99,102,241,0.2); }
#toc-list a[data-level=""2""] { padding-left: 28px; border-left-color: rgba(124,127,238,0.2); }
#toc-list a[data-level=""3""] { padding-left: 44px; border-left-color: rgba(146,149,240,0.2); }
#toc-list a[data-level=""4""],
#toc-list a[data-level=""5""],
#toc-list a[data-level=""6""] { padding-left: 60px; border-left-color: rgba(180,182,245,0.2); }

#toc-list a:hover {
    background: rgba(99,102,241,0.06);
    color: #24292f;
}
#toc-list a.active {
    font-weight: 600;
    color: #6366F1;
    border-left-color: #6366F1;
    background: rgba(99,102,241,0.08);
}

@media (max-width: 767px) {
    #toc-sidebar { width: 100vw; }
    #toc-backdrop {
        display: none;
        position: fixed;
        inset: 0;
        background: rgba(0,0,0,0.3);
        z-index: 999;
    }
    #toc-backdrop.show { display: block; }
}

@media print {
    html, body {
        height: auto;
    }

    body {
        max-width: 100%;
        padding: 20px 24px;
        background: #fff;
        color: #000;
        -webkit-print-color-adjust: exact;
        print-color-adjust: exact;
    }

    p, li {
        orphans: 3;
        widows: 3;
    }

    pre, blockquote, table, img, figure {
        page-break-inside: avoid;
        break-inside: avoid;
    }

    .mermaid {
        display: block;
        text-align: center;
        page-break-inside: avoid;
        break-inside: avoid;
        overflow: visible;
        max-width: 100%;
    }

    .mermaid svg {
        display: block;
        margin: 0 auto;
        max-width: 100% !important;
        width: auto !important;
        height: auto !important;
        max-height: 85vh;
    }

    h1, h2, h3, h4, h5, h6 {
        page-break-after: avoid;
        break-after: avoid;
    }

    a {
        color: #000;
        text-decoration: underline;
    }

    #live-indicator {
        display: none;
    }

    #toc-toggle, #toc-sidebar, #toc-backdrop { display: none !important; }
}
</style>
<script src=""https://cdn.jsdelivr.net/npm/marked/lib/marked.umd.js""></script>
<script src=""https://cdn.jsdelivr.net/npm/mermaid/dist/mermaid.min.js""></script>
<script>
mermaid.initialize({
    startOnLoad: false,
    theme: 'base',
    themeVariables: {
        /* ── Core ─────────────────────────────────────────────────── */
        background: '#ffffff',
        primaryColor: '#ddf4ff',
        primaryTextColor: '#24292f',
        primaryBorderColor: '#0969da',
        secondaryColor: '#dafbe1',
        secondaryBorderColor: '#1a7f37',
        secondaryTextColor: '#57606a',
        tertiaryColor: '#fff8c5',
        tertiaryBorderColor: '#9a6700',
        tertiaryTextColor: '#57606a',
        noteBkgColor: '#fff8c5',
        noteBorderColor: '#9a6700',
        noteTextColor: '#24292f',
        lineColor: '#57606a',
        arrowheadColor: '#57606a',
        textColor: '#24292f',
        border2: '#d0d7de',
        mainBkg: '#ddf4ff',
        nodeBkg: '#ddf4ff',
        nodeBorder: '#0969da',
        nodeTextColor: '#24292f',
        clusterBkg: '#f6f8fa',
        clusterBorder: '#d0d7de',
        defaultLinkColor: '#57606a',
        titleColor: '#24292f',
        edgeLabelBackground: '#f6f8fa',
        fontFamily: 'Inter, -apple-system, BlinkMacSystemFont, Helvetica, Arial, sans-serif',
        fontSize: '14px',
        useGradient: false,
        dropShadow: 'drop-shadow(0px 1px 2px rgba(31,35,40,0.12))',
        /* ── Sequence ─────────────────────────────────────────────── */
        actorBorder: '#0969da', actorBkg: '#ddf4ff', actorTextColor: '#24292f',
        actorLineColor: '#d0d7de', signalColor: '#57606a', signalTextColor: '#24292f',
        labelBoxBkgColor: '#ddf4ff', labelBoxBorderColor: '#0969da',
        labelTextColor: '#24292f', loopTextColor: '#24292f',
        activationBorderColor: '#0969da', activationBkgColor: '#b6e3ff',
        sequenceNumberColor: '#ffffff',
        /* ── Gantt ────────────────────────────────────────────────── */
        sectionBkgColor: '#ddf4ff', altSectionBkgColor: '#f6f8fa',
        sectionBkgColor2: '#dafbe1', excludeBkgColor: '#f6f8fa',
        taskBorderColor: '#0969da', taskBkgColor: '#54aeff',
        activeTaskBorderColor: '#0550ae', activeTaskBkgColor: '#b6e3ff',
        gridColor: '#d0d7de', doneTaskBkgColor: '#d8dee4', doneTaskBorderColor: '#8c959f',
        critBorderColor: '#cf222e', critBkgColor: '#ffebe9', todayLineColor: '#cf222e',
        taskTextColor: '#24292f', taskTextLightColor: '#ffffff', taskTextDarkColor: '#24292f',
        taskTextClickableColor: '#0969da',
        /* ── State ────────────────────────────────────────────────── */
        stateBkg: '#ddf4ff', stateLabelColor: '#24292f',
        transitionColor: '#57606a', transitionLabelColor: '#24292f',
        compositeBackground: '#ffffff', altBackground: '#f6f8fa',
        compositeTitleBackground: '#f6f8fa', compositeBorder: '#d0d7de',
        innerEndBackground: '#0969da',
        errorBkgColor: '#ffebe9', errorTextColor: '#cf222e',
        labelBackgroundColor: '#ddf4ff', specialStateColor: '#57606a',
        /* ── GitGraph ─────────────────────────────────────────────── */
        git0: '#0969da', git1: '#1a7f37', git2: '#8250df', git3: '#cf222e',
        git4: '#bf3989', git5: '#9a6700', git6: '#bc4c00', git7: '#57606a',
        gitInv0: '#ffffff', gitInv1: '#ffffff', gitInv2: '#ffffff', gitInv3: '#ffffff',
        gitInv4: '#ffffff', gitInv5: '#ffffff', gitInv6: '#ffffff', gitInv7: '#ffffff',
        gitBranchLabel0: '#ffffff', gitBranchLabel1: '#ffffff', gitBranchLabel2: '#ffffff',
        gitBranchLabel3: '#ffffff', gitBranchLabel4: '#ffffff', gitBranchLabel5: '#ffffff',
        gitBranchLabel6: '#ffffff', gitBranchLabel7: '#ffffff',
        branchLabelColor: '#24292f',
        tagLabelColor: '#24292f', tagLabelBackground: '#fff8c5', tagLabelBorder: '#9a6700',
        commitLabelColor: '#24292f', commitLabelBackground: '#f6f8fa',
        /* ── Pie / Donut ──────────────────────────────────────────── */
        pie1: '#218bff', pie2: '#2da44e', pie3: '#d4a72c', pie4: '#fa4549',
        pie5: '#a475f9', pie6: '#e85aad', pie7: '#ec6547', pie8: '#fb8f44',
        pie9: '#4ac26b', pie10: '#54aeff', pie11: '#c297ff', pie12: '#eac54f',
        pieTitleTextColor: '#24292f', pieSectionTextColor: '#ffffff',
        pieLegendTextColor: '#24292f', pieStrokeColor: '#ffffff',
        pieOuterStrokeColor: '#d0d7de', pieOpacity: '0.9',
        /* ── Treemap / cScale ─────────────────────────────────────── */
        cScale0: '#ddf4ff',  cScale1: '#dafbe1',  cScale2: '#fff8c5',
        cScale3: '#ffebe9',  cScale4: '#fbefff',  cScale5: '#ffeff7',
        cScale6: '#fff0eb',  cScale7: '#fff1e5',  cScale8: '#b6e3ff',
        cScale9: '#aceebb',  cScale10: '#fae17d', cScale11: '#ecd8ff',
        cScaleInv0: '#0550ae',  cScaleInv1: '#116329',  cScaleInv2: '#7d4e00',
        cScaleInv3: '#a40e26',  cScaleInv4: '#6639ba',  cScaleInv5: '#99286e',
        cScaleInv6: '#9e2f1c',  cScaleInv7: '#953800',  cScaleInv8: '#033d8b',
        cScaleInv9: '#044f1e',  cScaleInv10: '#633c01', cScaleInv11: '#512a97',
        cScalePeer0: '#b6e3ff', cScalePeer1: '#aceebb', cScalePeer2: '#fae17d',
        cScalePeer3: '#ffcecb', cScalePeer4: '#ecd8ff', cScalePeer5: '#ffd3eb',
        cScalePeer6: '#ffd6cc', cScalePeer7: '#ffd8b5', cScalePeer8: '#80ccff',
        cScalePeer9: '#6fdd8b', cScalePeer10: '#eac54f', cScalePeer11: '#d8b9ff',
        scaleLabelColor: '#24292f',
        /* ── Class ────────────────────────────────────────────────── */
        classText: '#24292f',
        fillType0: '#ddf4ff', fillType1: '#dafbe1', fillType2: '#fff8c5',
        fillType3: '#ffebe9', fillType4: '#fbefff', fillType5: '#ffeff7',
        fillType6: '#fff0eb', fillType7: '#fff1e5',
        /* ── Quadrant ─────────────────────────────────────────────── */
        quadrant1Fill: '#ddf4ff',    quadrant2Fill: '#dafbe1',
        quadrant3Fill: '#fff8c5',    quadrant4Fill: '#f6f8fa',
        quadrantPointFill: '#0969da', quadrantPointTextFill: '#24292f',
        quadrantXAxisTextFill: '#57606a', quadrantYAxisTextFill: '#57606a',
        quadrantInternalBorderStrokeFill: '#d0d7de',
        quadrantExternalBorderStrokeFill: '#d0d7de',
        quadrantTitleFill: '#24292f',
        /* ── C4 ───────────────────────────────────────────────────── */
        personBorder: '#0969da', personBkg: '#ddf4ff',
        /* ── ER ───────────────────────────────────────────────────── */
        rowOdd: '#ffffff', rowEven: '#f6f8fa',
        attributeBackgroundColorOdd: '#ffffff', attributeBackgroundColorEven: '#f6f8fa',
        /* ── Architecture ─────────────────────────────────────────── */
        archEdgeColor: '#57606a', archEdgeArrowColor: '#57606a',
        archGroupBorderColor: '#d0d7de', archGroupBorderWidth: '2px',
        /* ── Requirement ──────────────────────────────────────────── */
        requirementBackground: '#ddf4ff',
        requirementBorderColor: '#0969da',
        requirementTextColor: '#24292f',
        /* ── Gradient (Mindmap, Timeline, GitGraph) ───────────────── */
        gradientStart: '#0969da',
        gradientStop: '#8250df',
    },
    securityLevel: 'strict',
    wrap: true,
    flowchart: { useMaxWidth: true, htmlLabels: true, wrap: true },
    sequence:  { useMaxWidth: true, wrap: true },
    gantt:     { useMaxWidth: true },
    er:        { useMaxWidth: true, layoutDirection: 'TB', diagramPadding: 20, entityPadding: 15 }
});

// marked v5+ uses marked.use() — renderer functions receive a token object,
// not separate string arguments like in v4.
marked.use({ gfm: true, breaks: false });

var _sourceLine = 0;
var _headingSlugs = {};
marked.use({
    renderer: {
        heading(token) {
            _sourceLine++;
            var text = this.parser.parseInline(token.tokens);
            var slug = text.toLowerCase()
                .replace(/<[^>]+>/g, '')
                .replace(/[^\w\u00C0-\u024F]+/g, '-')
                .replace(/^-+|-+$/g, '');
            if (_headingSlugs[slug] !== undefined) {
                _headingSlugs[slug]++;
                slug = slug + '-' + _headingSlugs[slug];
            } else {
                _headingSlugs[slug] = 0;
            }
            return '<h' + token.depth + ' id=""' + slug + '"" data-source-line=""' + _sourceLine + '"">' + text + '</h' + token.depth + '>';
        },
        paragraph(token) {
            _sourceLine++;
            var text = this.parser.parseInline(token.tokens);
            return '<p data-source-line=""' + _sourceLine + '"">' + text + '</p>';
        },
        code(token) {
            _sourceLine++;
            // Escape HTML entities so the DOM parses correctly and
            // .textContent returns the original source (used by renderMermaidBlocks).
            var escaped = token.text
                .replace(/&/g, '&amp;')
                .replace(/</g, '&lt;')
                .replace(/>/g, '&gt;');
            return '<pre data-source-line=""' + _sourceLine + '""><code class=""language-' + (token.lang || '') + '"">' + escaped + '</code></pre>';
        },
        listitem(token) {
            _sourceLine++;
            var body = token.tokens ? this.parser.parse(token.tokens, !!token.loose) : token.text;
            return '<li data-source-line=""' + _sourceLine + '"">' + body + '</li>';
        }
    }
});

function sanitizeHtml(html) {
    var doc = new DOMParser().parseFromString(html, 'text/html');
    var dangerous = doc.querySelectorAll('script, object, embed, iframe');
    for (var i = dangerous.length - 1; i >= 0; i--) dangerous[i].remove();
    var all = doc.querySelectorAll('*');
    for (var i = 0; i < all.length; i++) {
        var el = all[i];
        for (var j = el.attributes.length - 1; j >= 0; j--) {
            var attr = el.attributes[j];
            if (/^on/i.test(attr.name)) { el.removeAttribute(attr.name); continue; }
            if ((attr.name === 'href' || attr.name === 'src' || attr.name === 'action') &&
                /^\s*javascript:/i.test(attr.value)) el.removeAttribute(attr.name);
        }
    }
    return doc.body.innerHTML;
}

function renderContent(md, cursorLine) {
    _sourceLine = 0;
    _headingSlugs = {};
    var savedScrollY = window.scrollY;
    document.getElementById('content').innerHTML = sanitizeHtml(marked.parse(md));
    renderMermaidBlocks();
    buildToc();
    if (cursorLine && cursorLine > 0) {
        scrollToSourceLine(cursorLine);
    } else {
        window.scrollTo(0, savedScrollY);
    }
}

function scrollToSourceLine(line) {
    var best = null;
    var bestDiff = Infinity;
    var els = document.querySelectorAll('[data-source-line]');
    for (var i = 0; i < els.length; i++) {
        var elLine = parseInt(els[i].getAttribute('data-source-line'), 10);
        var diff = Math.abs(elLine - line);
        if (diff < bestDiff) {
            bestDiff = diff;
            best = els[i];
        }
        if (elLine > line + 5) break;
    }
    if (best) {
        var rect = best.getBoundingClientRect();
        var y = window.scrollY + rect.top - window.innerHeight / 3;
        window.scrollTo({ top: y, behavior: 'smooth' });
    }
}

async function renderMermaidBlocks() {
    const blocks = document.querySelectorAll('pre > code.language-mermaid');
    for (let i = 0; i < blocks.length; i++) {
        const pre = blocks[i].parentElement;
        const code = blocks[i].textContent;
        const container = document.createElement('div');
        container.className = 'mermaid';
        try {
            const { svg } = await mermaid.render('mermaid-' + Date.now() + '-' + i, code);
            container.innerHTML = svg;
            // Strip fixed pixel dimensions injected by Mermaid so CSS controls sizing
            const svgEl = container.querySelector('svg');
            if (svgEl) {
                svgEl.removeAttribute('width');
                svgEl.removeAttribute('height');
                svgEl.style.maxWidth = '100%';
                svgEl.style.width = 'auto';
                svgEl.style.height = 'auto';
            }
        } catch (e) {
            var errEl = document.createElement('pre');
            errEl.style.cssText = 'color:#e53e3e;font-size:0.85em';
            errEl.textContent = 'Mermaid error: ' + e.message;
            container.appendChild(errEl);
        }
        pre.replaceWith(container);
    }
}

// ── TOC Navigation ──────────────────────────────────────────────
var _tocToggle   = null;
var _tocSidebar  = null;
var _tocList     = null;
var _tocCount    = null;
var _tocBackdrop = null;
var _tocOpen     = false;

function toggleToc() {
    _tocOpen = !_tocOpen;
    _tocSidebar.classList.toggle('open', _tocOpen);
    _tocToggle.textContent = _tocOpen ? '✕' : '☰';
    if (_tocBackdrop) _tocBackdrop.classList.toggle('show', _tocOpen);
    if (_tocOpen) updateActiveHeading();
}

document.addEventListener('DOMContentLoaded', function() {
    _tocToggle   = document.getElementById('toc-toggle');
    _tocSidebar  = document.getElementById('toc-sidebar');
    _tocList     = document.getElementById('toc-list');
    _tocCount    = document.getElementById('toc-count');
    _tocBackdrop = document.getElementById('toc-backdrop');
    if (_tocToggle) _tocToggle.addEventListener('click', toggleToc);
    if (_tocBackdrop) _tocBackdrop.addEventListener('click', toggleToc);
});

document.addEventListener('keydown', function(e) {
    if (e.key === 't' || e.key === 'T') {
        var tag = document.activeElement ? document.activeElement.tagName : '';
        if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return;
        toggleToc();
    }
});

function buildToc() {
    var headings = document.querySelectorAll('#content h1, #content h2, #content h3, #content h4, #content h5, #content h6');
    _tocList.innerHTML = '';
    var count = 0;
    headings.forEach(function(h) {
        count++;
        var a = document.createElement('a');
        a.href = '#' + (h.id || '');
        a.setAttribute('data-level', h.tagName.charAt(1));
        a.textContent = h.textContent;
        a.addEventListener('click', function(e) {
            e.preventDefault();
            var target = document.getElementById(h.id);
            if (target) {
                var y = target.getBoundingClientRect().top + window.scrollY - 60;
                window.scrollTo({ top: y, behavior: 'smooth' });
            }
            if (window.innerWidth < 768 && _tocOpen) toggleToc();
        });
        _tocList.appendChild(a);
    });
    _tocCount.textContent = count + (count === 1 ? ' heading' : ' headings');
    setupScrollSpy();
}

var _scrollObserver = null;

function setupScrollSpy() {
    if (_scrollObserver) _scrollObserver.disconnect();
    var headings = document.querySelectorAll('#content h1, #content h2, #content h3, #content h4, #content h5, #content h6');
    if (!headings.length) return;

    _scrollObserver = new IntersectionObserver(function(entries) {
        entries.forEach(function(entry) {
            if (entry.isIntersecting) setActiveHeading(entry.target.id);
        });
    }, { root: null, rootMargin: '-80px 0px -60% 0px', threshold: 0 });

    headings.forEach(function(h) { _scrollObserver.observe(h); });
    updateActiveHeading();
}

function updateActiveHeading() {
    var headings = document.querySelectorAll('#content h1, #content h2, #content h3, #content h4, #content h5, #content h6');
    if (!headings.length) return;
    var active = null;
    var threshold = window.scrollY + 100;
    for (var i = headings.length - 1; i >= 0; i--) {
        if (headings[i].getBoundingClientRect().top + window.scrollY <= threshold) {
            active = headings[i];
            break;
        }
    }
    setActiveHeading(active ? active.id : headings[0].id);
}

function setActiveHeading(id) {
    var links = _tocList.querySelectorAll('a');
    var found = false;
    for (var i = 0; i < links.length; i++) {
        var isActive = links[i].getAttribute('href') === '#' + id;
        links[i].classList.toggle('active', isActive);
        if (isActive && !found) {
            found = true;
            links[i].scrollIntoView({ block: 'nearest', behavior: 'smooth' });
        }
    }
}

var _scrollSpyRaf = null;
window.addEventListener('scroll', function() {
    if (!_tocOpen) return;
    if (_scrollSpyRaf) cancelAnimationFrame(_scrollSpyRaf);
    _scrollSpyRaf = requestAnimationFrame(updateActiveHeading);
}, { passive: true });

var _lastVersion = -1;
var _cursorLine = 1;

function startSSE() {
    var es = new EventSource('/events');
    es.addEventListener('version', function(e) {
        var v = parseInt(e.data, 10);
        if (v !== _lastVersion) {
            _lastVersion = v;
            fetch('/content').then(function(r) { return r.json(); }).then(function(data) {
                renderContent(data.md, data.cursorLine);
            }).catch(function() {});
        }
    });
    es.addEventListener('cursor', function(e) {
        _cursorLine = parseInt(e.data, 10);
    });
    es.onerror = function() {
        setTimeout(startSSE, 2000);
    };
}

document.addEventListener('DOMContentLoaded', function() {
    startSSE();
});

window.addEventListener('beforeprint', function() {
    document.querySelectorAll('.mermaid svg').forEach(function(svg) {
        svg.removeAttribute('width');
        svg.removeAttribute('height');
        svg.style.maxWidth = '100%';
        svg.style.width = 'auto';
        svg.style.height = 'auto';
    });
});
</script>
</head>
<body>
<div id=""content""><p style=""color:#9496a1;text-align:center;margin-top:3em;"">Loading preview…</p></div>
<div id=""live-indicator"">● live</div>
<button id=""toc-toggle"" title=""Table of Contents (T)"">☰</button>
<div id=""toc-backdrop""></div>
<nav id=""toc-sidebar"">
    <div id=""toc-header"">
        <span>Outline</span>
        <span id=""toc-count"">0 headings</span>
    </div>
    <ul id=""toc-list""></ul>
</nav>
</body>
</html>";

        public static string GetLiveTemplate()
        {
            return LiveTemplate;
        }

        public static string BuildStandaloneHtml(string markdown)
        {
            var md = markdown ?? string.Empty;
            var safeMd = md.Replace("</script", "<\\/script");

            const string placeholder = @"<p style=""color:#9496a1;text-align:center;margin-top:3em;"">Loading preview…</p>";
            const string liveIndicator = @"<div id=""live-indicator"">● live</div>";
            const string sseScript = @"document.addEventListener('DOMContentLoaded', function() {
    startSSE();
});";
            const string initScript = @"document.addEventListener('DOMContentLoaded', function() {
    var d = document.getElementById('__md_data');
    if (d) renderContent(d.textContent);
});";

            var html = LiveTemplate
                .Replace(placeholder, $"<script type=\"text/plain\" id=\"__md_data\">{safeMd}</script>")
                .Replace(liveIndicator, "")
                .Replace(sseScript, initScript);

            return html;
        }

        public static string SaveAndOpenStandaloneHtml(string markdown)
        {
            var html = BuildStandaloneHtml(markdown);
            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".html");
            File.WriteAllText(tempPath, html, Encoding.UTF8);
            Process.Start(new ProcessStartInfo { FileName = tempPath, UseShellExecute = true });
            return tempPath;
        }
    }
}
