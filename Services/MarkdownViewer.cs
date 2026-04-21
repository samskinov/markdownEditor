using System;
using System.Diagnostics;

namespace MarkdownEditor.Services
{
    /// <summary>
    /// Static entry point to display Markdown directly in the default browser
    /// (Edge, Chrome, etc.) without requiring the WPF editor control.
    /// Useful for showing documentation or rich content to end users.
    ///
    /// Markdown -> HTML rendering is performed client-side by marked.js (CDN).
    /// Mermaid diagrams are rendered by mermaid.js (CDN).
    /// No external DLLs or system installation required.
    ///
    /// Basic usage:
    /// <code>
    ///   // Open the browser and display markdown:
    ///   MarkdownViewer.ShowAndOpen("# Title\n\nContent...");
    ///
    ///   // Update the content without opening a new tab
    ///   // (the browser refreshes automatically via polling):
    ///   MarkdownViewer.Show("# Updated");
    ///
    ///   // Clean shutdown when the application exits:
    ///   MarkdownViewer.Close();
    /// </code>
    /// </summary>
    public static class MarkdownViewer
    {
        private static PreviewHttpServer? _server;
        private static readonly object _lock = new object();

        /// <summary>
        /// Displays the given Markdown content in the default browser.
        /// If the browser is already open on the viewer page, the content
        /// refreshes automatically without opening a new tab (400ms polling).
        /// The server is started automatically on first use.
        /// </summary>
        /// <param name="markdown">Markdown content to display (GFM + Mermaid supported).</param>
        public static void Show(string markdown)
        {
            EnsureServer();
            _server!.UpdateContent(markdown ?? string.Empty);
        }

        /// <summary>
        /// Displays the Markdown content AND opens a tab in the default browser.
        /// Use this for the first open or to force browser focus.
        /// </summary>
        /// <param name="markdown">Markdown content to display (GFM + Mermaid supported).</param>
        public static void ShowAndOpen(string markdown)
        {
            EnsureServer();
            _server!.UpdateContent(markdown ?? string.Empty);
            OpenBrowser(_server!.Url);
        }

        /// <summary>
        /// Stops the preview server and releases network resources.
        /// Call this when the application is shutting down.
        /// After this call, the next call to <see cref="Show"/> or
        /// <see cref="ShowAndOpen"/> will restart a new server on a new port.
        /// </summary>
        public static void Close()
        {
            lock (_lock)
            {
                _server?.Dispose();
                _server = null;
            }
        }

        /// <summary>
        /// Local URL of the preview server (e.g. http://localhost:54321/).
        /// Returns <c>null</c> if the server has not yet been started.
        /// </summary>
        public static string? ServerUrl => _server?.Url;

        /// <summary>
        /// Indicates whether the server is currently running.
        /// </summary>
        public static bool IsRunning => _server != null;

        // ─── Private ─────────────────────────────────────────

        private static void EnsureServer()
        {
            if (_server != null) return;
            lock (_lock)
            {
                if (_server != null) return;
                _server = new PreviewHttpServer();
            }
        }

        private static void OpenBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MarkdownViewer: unable to open browser: {ex.Message}");
            }
        }
    }
}
