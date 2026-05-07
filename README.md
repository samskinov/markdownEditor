## MarkdownEditor

A lightweight WPF Markdown editor component with live preview and Mermaid diagram support. Targets .NET Framework 4.8 and ships as a small, self-contained library intended for embedding in WPF apps.

Table of contents
- Features
- Quick Start
- Embedding in your WPF app
- Preview modes (live vs static)
- API reference (key classes)
- Embedded images and folding behavior
- Template and resources
- Build & development
- Security & notes

---

## Features

- AvalonEdit-based editor with Markdown syntax highlighting and folding
- Live preview served to the default browser (no WebView2 / Chromium required)
- Client-side Markdown rendering with `marked.js` and Mermaid diagrams via `mermaid.js`
- Embedded image support (images are encoded as base64 data URIs and stored in a reference block)
- Programmatic viewer (`MarkdownViewer`) for quick display of documentation to end users
- Export to standalone HTML via `HtmlTemplateService` (optionally set the browser tab title)

## Quick Start

1. Add a project reference to the `MarkdownEditor` project/assembly.
2. Drop the `MarkdownEditorView` into your XAML window.
3. Wire up the `MarkdownEditorViewModel` and handle `SaveRequested` if you need to persist content.

Embedding example (XAML):

```xml
<Window xmlns:md="clr-namespace:MarkdownEditor.Views;assembly=MarkdownEditor">
    <Grid>
        <md:MarkdownEditorView x:Name="MdEditor" />
    </Grid>
</Window>
```

Wiring in code-behind (C#):

```csharp
using MarkdownEditor.ViewModels;

public partial class MainWindow : Window
{
        private readonly MarkdownEditorViewModel _editorVm;

        public MainWindow()
        {
                InitializeComponent();
                _editorVm = new MarkdownEditorViewModel();
                MdEditor.DataContext = _editorVm;

                // Save handling
                _editorVm.SaveRequested += OnSaveRequested;
        }

        private void OnSaveRequested()
        {
                var markdown = _editorVm.GetContent();
                // persist markdown (file, DB, etc.)
                _editorVm.MarkAsSaved();
        }
}
```

## Preview modes

There are two primary ways to show rendered Markdown to users:

1. Live preview (dynamic, with auto-refresh) using `MarkdownViewer`.
2. Static/standalone HTML export using `HtmlTemplateService.BuildStandaloneHtml` / `SaveAndOpenStandaloneHtml`.

### Live preview (recommended for editing)

`MarkdownViewer` starts a small local HTTP server and serves a preview page that renders Markdown client-side. Use:

```csharp
using MarkdownEditor.Services;

// Start the server (if needed) and open the default browser (or focus it)
MarkdownViewer.ShowAndOpen(markdown);

// Update content programmatically (browser refreshes)
MarkdownViewer.Show(updatedMarkdown);

// When your app exits, stop the server
MarkdownViewer.Close();
```

This mode keeps a single browser tab open and refreshes content automatically (polling ~400ms). The server URL is available as `MarkdownViewer.ServerUrl`.

### Static / standalone HTML (good for distribution, printing)

Create a single HTML file that you can ship to users or open in a browser directly. The HTML is generated from a template resource (see below).

```csharp
using MarkdownEditor.Services;

// Generate and open a standalone preview with a custom browser tab title
// The 'title' parameter sets the <title> of the HTML — useful when opening in Edge/Chrome.
var path = HtmlTemplateService.SaveAndOpenStandaloneHtml(markdown, "User Guide — MyApp");

// Or just get the HTML string and control how/where you save/open it
var html = HtmlTemplateService.BuildStandaloneHtml(markdown, "User Guide — MyApp");
File.WriteAllText("my-docs.html", html, Encoding.UTF8);
Process.Start(new ProcessStartInfo { FileName = "msedge", Arguments = $"--new-window \"my-docs.html\"", UseShellExecute = true });
```

If `title` is `null` or empty, the default title `Markdown Preview` is used. The title is HTML-escaped to avoid injection.

## API reference (selected)

- `MarkdownEditorViewModel` — primary ViewModel used by the view.
    - `LoadContent(string)` — load Markdown into the editor programmatically.
    - `GetContent()` — returns current Markdown text.
    - `MarkAsSaved()` — mark document as saved after external persistence.
    - `SaveRequested` (event) — raised when user clicks Save.
    - Commands for inserting Markdown constructs and embedding images are exposed (e.g., `EmbedImageCommand`).

- `MarkdownViewer` — small helper to show Markdown in the default browser.
    - `Show(string markdown)` — update content without opening a new tab.
    - `ShowAndOpen(string markdown)` — update content and open/focus browser.
    - `Close()` — stop the preview server.

- `HtmlTemplateService`
    - `BuildStandaloneHtml(string markdown, string? title = null)` — returns the full HTML string for the given Markdown. `title` sets the HTML `<title>`.
    - `SaveAndOpenStandaloneHtml(string markdown, string? title = null)` — writes the HTML to a temp file and opens it in the default browser; returns the path.

- `ImageEmbedder`
    - `EncodeFile(string path)` / `EncodeStream(Stream)` — encodes an image to a `data:image/webp;base64,...` URI. Images are resized so the largest side ≤ 1600px (no upscale) and encoded to WebP (lossy, quality ~80).

- `EmbeddedImagesBlock`
    - `Parse(string markdown)` — parse the reference-style embedded-images block and return existing entries.
    - `NextId(...)` — generate the next `img-N` id.
    - `Upsert(string markdown, string id, string dataUri)` — insert/update the embedded images block and return the updated markdown.

## Embedded images behavior

- When the user embeds an image (from file dialog or clipboard), images are encoded to WebP and inserted as a reference link like `![alt][img-1]` with the actual `data:` URI stored in an `<!-- embedded-images:start -->` / `<!-- embedded-images:end -->` block at the bottom of the document.
- The folding strategy creates a single fold for the embedded images block and marks it as folded by default. The fold shows a summary like: "embedded-images (3 images, 1.2 MB)".
- We also trigger an immediate fold refresh after insertion so the block collapses right away. To change this behaviour, modify `Services/MarkdownFoldingStrategy.cs` (the `DefaultClosed` property) or remove the immediate update call in `Views/MarkdownEditorView.xaml.cs`.

## Template & resources

- The HTML preview template is stored as an embedded resource at `Resources/preview-template.html` inside the `MarkdownEditor` assembly.
- `HtmlTemplateService` lazily loads that resource from the assembly. The resource name is `{{AssemblyName}}.Resources.preview-template.html`.
- By default the template references `marked.js` and `mermaid.js` via CDN. If you want offline usage, either:
    1. Edit `Resources/preview-template.html` to inline local copies of the scripts, or
    2. Use the `Services/EmbeddedScriptProvider` to get embedded script contents for `Scripts/marked.min.js` and `Scripts/mermaid.min.js` and then modify the template at runtime before serving.

The template contains client-side sanitization and Mermaid configuration (theme variables, `wrap: true`, `securityLevel: 'strict'`) — see `Resources/preview-template.html`.

## Build & Development

- Requirements: Visual Studio or MSBuild with .NET Framework 4.8 targeting packs.
- Build with:

```bash
dotnet build -c Debug
```

### NuGet packages

- `AvalonEdit` (editor control)
- `SixLabors.ImageSharp` (image decoding/encoding)

Note: a recent `dotnet build` may warn about known advisories for `SixLabors.ImageSharp` version 2.1.9. Consider updating the package to a patched version if required by your security policy.

## Security & notes

- The preview template performs client-side sanitization (`sanitizeHtml`) to remove `<script>`, inline event handlers and `javascript:` links prior to inserting rendered content. This reduces XSS risk when displaying untrusted input, but you should still validate and sanitize content according to your application's threat model.
- `HtmlTemplateService.BuildStandaloneHtml` escapes the provided `title` to avoid HTML injection when setting the `<title>` element.
- Embedded images are stored inline as base64 data URIs; be mindful of large images (the editor warns if encoded size > 2MB). Consider storing large images externally and using regular image URLs for large assets.

## Where to look in the codebase

- View: `Views/MarkdownEditorView.xaml` and `Views/MarkdownEditorView.xaml.cs`
- ViewModel: `ViewModels/MarkdownEditorViewModel.cs`
- Preview server: `Services/PreviewHttpServer.cs`
- Template loader: `Services/HtmlTemplateService.cs` (loads `Resources/preview-template.html`)
- Script provider: `Services/EmbeddedScriptProvider.cs` (embedded `marked`/`mermaid`)
- Image embedder: `Services/ImageEmbedder.cs` and `Services/EmbeddedImagesBlock.cs`
- Folding strategy: `Services/MarkdownFoldingStrategy.cs`

---

If you'd like, I can also:

- produce a minimal sample WPF host app showing the editor embedded and saving/loading markdown
- create a short test harness that demonstrates `MarkdownViewer` and `HtmlTemplateService` with custom titles
- update the template to inline `marked.js`/`mermaid.js` for offline usage

Tell me which of the above you'd like next.

