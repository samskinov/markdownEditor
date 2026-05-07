using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;

namespace MarkdownEditor.Services
{
    public static class HtmlTemplateService
    {
        private static string? _liveTemplate;

        private static string LiveTemplate =>
            _liveTemplate ??= LoadTemplate();

        private static string LoadTemplate()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = assembly.GetName().Name + ".Resources.preview-template.html";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException(
                        $"Embedded resource '{resourceName}' not found.");
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                    return reader.ReadToEnd();
            }
        }

        public static string GetLiveTemplate()
        {
            return LiveTemplate;
        }


        public static string BuildStandaloneHtml(string markdown, string? title = null)
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

            if (!string.IsNullOrWhiteSpace(title))
                html = html.Replace("<title>Markdown Preview</title>",
                    $"<title>{System.Security.SecurityElement.Escape(title)}</title>");

            return html;
        }

        public static string SaveAndOpenStandaloneHtml(string markdown, string? title = null)
        {
            var html = BuildStandaloneHtml(markdown, title);
            var tempPath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".html");
            File.WriteAllText(tempPath, html, Encoding.UTF8);
            Process.Start(new ProcessStartInfo { FileName = tempPath, UseShellExecute = true });
            return tempPath;
        }
    }
}
