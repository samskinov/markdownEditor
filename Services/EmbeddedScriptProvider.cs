using System.IO;
using System.Reflection;

namespace MarkdownEditor.Services
{
    public static class EmbeddedScriptProvider
    {
        private static string? _markedJs;
        private static string? _mermaidJs;

        public static string? GetScript(string fileName)
        {
            switch (fileName)
            {
                case "marked.min.js":
                    return _markedJs ??= LoadEmbeddedResource("Scripts.marked.min.js");
                case "mermaid.min.js":
                    return _mermaidJs ??= LoadEmbeddedResource("Scripts.mermaid.min.js");
                default:
                    return null;
            }
        }

        private static string? LoadEmbeddedResource(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var fullName = assembly.GetName().Name + "." + resourceName;

            using (var stream = assembly.GetManifestResourceStream(fullName))
            {
                if (stream == null) return null;
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}
