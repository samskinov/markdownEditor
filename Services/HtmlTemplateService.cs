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
<style>
:root {
    --bg: #ffffff;
    --text: #1a1a2e;
    --text-secondary: #555770;
    --border: #e2e4e9;
    --code-bg: #f6f8fa;
    --blockquote-border: #6366f1;
    --blockquote-bg: #f0f0ff;
    --link: #6366f1;
    --table-header-bg: #f8f9fb;
    --table-border: #e2e4e9;
    --hr: #e2e4e9;
    --inline-code-bg: #eff1f5;
    --inline-code-text: #d63384;
    --checkbox-accent: #6366f1;
    --heading-color: #16163a;
    --shadow: rgba(0,0,0,0.04);
}

* { margin: 0; padding: 0; box-sizing: border-box; }

body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, sans-serif;
    font-size: 15px;
    line-height: 1.7;
    color: var(--text);
    background: var(--bg);
    padding: 28px 36px;
    max-width: 900px;
    margin: 0 auto;
    -webkit-font-smoothing: antialiased;
}

h1, h2, h3, h4, h5, h6 {
    color: var(--heading-color);
    margin-top: 1.6em;
    margin-bottom: 0.6em;
    font-weight: 700;
    line-height: 1.3;
    letter-spacing: -0.01em;
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

tr:nth-child(even) td { background: #fafbfc; }

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
}

.mermaid svg {
    max-width: 100%;
    height: auto;
}

#live-indicator {
    position: fixed;
    bottom: 8px;
    right: 12px;
    font-size: 11px;
    color: #9496a1;
    opacity: 0.6;
}
</style>
<script src=""/scripts/marked.min.js""></script>
<script src=""/scripts/mermaid.min.js""></script>
<script>
mermaid.initialize({
    startOnLoad: false,
    theme: 'default',
    securityLevel: 'strict',
    flowchart: { useMaxWidth: true, htmlLabels: true },
    sequence: { useMaxWidth: true }
});

marked.setOptions({ gfm: true, breaks: false });

var _sourceLine = 0;
var _renderer = new marked.Renderer();
var _origHeading = _renderer.heading.bind(_renderer);
_renderer.heading = function(text, depth, raw) {
    _sourceLine++;
    return '<h' + depth + ' data-source-line=""' + _sourceLine + '"">' + text + '</h' + depth + '>';
};

var _origParagraph = _renderer.paragraph.bind(_renderer);
_renderer.paragraph = function(text) {
    _sourceLine++;
    return '<p data-source-line=""' + _sourceLine + '"">' + text + '</p>';
};

var _origCode = _renderer.code.bind(_renderer);
_renderer.code = function(code, lang, escaped) {
    _sourceLine++;
    return '<pre data-source-line=""' + _sourceLine + '""><code class=""language-' + (lang || '') + '"">' + (escaped ? code : (code || '')) + '</code></pre>';
};

var _origListitem = _renderer.listitem.bind(_renderer);
_renderer.listitem = function(text, task, checked) {
    _sourceLine++;
    return '<li data-source-line=""' + _sourceLine + '"">' + text + '</li>';
};

marked.setOptions({ renderer: _renderer });

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
    var savedScrollY = window.scrollY;
    document.getElementById('content').innerHTML = sanitizeHtml(marked.parse(md));
    renderMermaidBlocks();
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
        } catch (e) {
            var errEl = document.createElement('pre');
            errEl.style.cssText = 'color:#e53e3e;font-size:0.85em';
            errEl.textContent = 'Mermaid error: ' + e.message;
            container.appendChild(errEl);
        }
        pre.replaceWith(container);
    }
}

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
</script>
</head>
<body>
<div id=""content""><p style=""color:#9496a1;text-align:center;margin-top:3em;"">Loading preview…</p></div>
<div id=""live-indicator"">● live</div>
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

            html = html.Replace("<script src=\"/scripts/marked.min.js\"></script>",
                "<script src=\"https://cdn.jsdelivr.net/npm/marked@9/marked.min.js\"></script>");
            html = html.Replace("<script src=\"/scripts/mermaid.min.js\"></script>",
                "<script src=\"https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js\"></script>");

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
