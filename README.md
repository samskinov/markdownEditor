# MarkdownEditor — Markdown + Mermaid WPF

Lightweight WPF component for editing Markdown with live preview and Mermaid diagrams. Targets **.NET Framework 4.8**.

Features
- AvalonEdit-based Markdown editor with syntax highlighting
- Live preview served to the default browser (no WebView2 runtime required)
- Client-side Markdown rendering using `marked.js` and Mermaid diagrams via `mermaid.js`
- Small footprint: no bundled Chromium or CefSharp binaries

## Architecture

```
MarkdownEditor/
├── Mvvm/                     # MVVM base (ViewModelBase, RelayCommand)
├── Models/                   # MermaidExample, MarkdownSnippet
├── Services/                 # PreviewHttpServer, HtmlTemplateService, helpers
├── ViewModels/               # MarkdownEditorViewModel, help VMs
├── Views/                    # MarkdownEditorView and help windows
└── Themes/                   # ResourceDictionary with modern styles
```

Pattern: MVVM (view models contain state, views contain UI glue). Code-behind is limited to UI plumbing (AvalonEdit wiring, preview server lifecycle).

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| Microsoft.NETFramework.ReferenceAssemblies.net48 | 1.x | Target framework reference (build-time)
| AvalonEdit | 6.3.0.90 | Editor control (line numbers, syntax highlighting)

Notes:
- The old server-side Markdown conversion (Markdig) has been removed — Markdown is rendered client-side with `marked.js`.
- The preview does not require `WebView2` or any Chromium runtime; it uses an embedded `HttpListener` and the system default browser.

## Prerequisites

- .NET Framework 4.8 SDK installed.
- Internet access (for `marked.js` and `mermaid.js` CDNs). For offline scenarios, host those scripts locally or embed them as resources.

## Integration

### 1) Add a project reference

Reference `MarkdownEditor.csproj` from your WPF application.

### 2) Use the control in XAML

```xml
<Window xmlns:md="clr-namespace:MarkdownEditor.Views;assembly=MarkdownEditor">
  <Grid>
    <md:MarkdownEditorView x:Name="MdEditor" />
  </Grid>
</Window>
```

### 3) Wire the ViewModel and Save handling

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

        // Handle Save (user clicks Save in the toolbar)
        _editorVm.SaveRequested += OnSaveRequested;

        // Load initial content
        _editorVm.LoadContent("# Hello\n\nStart writing...");
    }

    private void OnSaveRequested()
    {
        var markdown = _editorVm.GetContent();
        SaveToDatabase(markdown);
        _editorVm.MarkAsSaved();
    }

    private void SaveToDatabase(string md) { /* your logic */ }
}
```

## ViewModel public API (summary)

| Member | Description |
|--------|-------------|
| `LoadContent(string)` | Load Markdown into the editor |
| `GetContent()` | Get current Markdown text |
| `MarkAsSaved()` | Mark document as saved |
| `SaveRequested` (event) | Fired when user clicks Save |
| `OpenPreviewCommand` | Command bound to the toolbar preview button (opens browser preview)
| `RequestOpenPreview` (event) | Raised when preview should open (view handles server lifecycle)
| `MarkdownContentChanged` (event) | Fired when the Markdown text changes (raw markdown) |
| `IsPreviewActive` | True when a preview server is active |
| `IsModified` | Indicates whether the document has unsaved changes |

Note: The view handles the preview server lifecycle. Programmatic preview/control is provided via `MarkdownViewer` (see below).

## Shortcuts

| Shortcut | Action |
|---------:|:------|
| Ctrl+S | Save |
| Ctrl+B | Bold |
| Ctrl+I | Italic |
| Ctrl+K | Insert link |
| Ctrl+Z | Undo |
| Ctrl+Y | Redo |

## Preview & Printing

- Preview is served by an embedded HTTP server (`PreviewHttpServer`) and rendered client-side in the default browser using `marked.js` (Markdown) and `mermaid.js` (diagrams).
- Live-reload: the browser polls `GET /content` every ~400 ms and updates the DOM when the version changes.
- To print or save as PDF, use the browser's Print dialog (Ctrl+P) when the preview is open, or generate a standalone HTML using `HtmlTemplateService.BuildStandaloneHtml(markdown)` and print that file.


## Displaying Markdown to users: two modes

### 1. Live preview (dynamic, with live reload)

Use the programmatic viewer API to show Markdown in the default browser with live updates (requires a local HTTP server):

```csharp
using MarkdownEditor.Services;

// Open the browser and display markdown (starts server if needed)
MarkdownViewer.ShowAndOpen(markdown);

// Update the content without opening a new tab (browser auto-refreshes)
MarkdownViewer.Show(updatedMarkdown);

// Stop the preview server (only when you are sure the user no longer needs the preview)
MarkdownViewer.Close();

// Inspect server URL
var url = MarkdownViewer.ServerUrl; // e.g. http://localhost:54321/
```

This is useful for dynamic documentation, tutorials, or any scenario where you want the browser to update automatically as the Markdown changes. Do not call `Close()` while the user is still viewing the page, or live reload will break.


### 2. Display-only mode (static HTML, no server)

For a simple, read-only display (no live reload, no server), just call:

```csharp
using MarkdownEditor.Services;

// The easiest way: one line, no manual Process.Start, no temp file handling
HtmlTemplateService.SaveAndOpenStandaloneHtml(markdown);
```

This method:
- Generates a static HTML file from your Markdown
- Saves it in the Windows temp folder
- Opens it automatically in the default browser
- Returns the file path if you want to track or delete it later

You do not need to manage Process.Start, file paths, or cleanup unless you want to. This is the recommended way for display-only scenarios, exports, or when you do not want to start a server.

## Export / Standalone HTML

Create a static HTML file (baked-in Markdown) using:

```csharp
var html = HtmlTemplateService.BuildStandaloneHtml(markdown);
File.WriteAllText("preview.html", html, Encoding.UTF8);
Process.Start(new ProcessStartInfo { FileName = "preview.html", UseShellExecute = true });
```

The generated HTML contains `marked.js` + `mermaid.js` usage and will render diagrams on load.

## Extension points & notes

- Theme: modify `Themes/ModernTheme.xaml` to customize colors and load a dark variant.
- Syntax highlighting: implemented in `Services/MarkdownHighlightingDefinition.cs` (embedded XSHD).
- Client-side rendering: `HtmlTemplateService` uses `marked.js` and `mermaid.js` from CDN by default. For offline usage, host these files locally and adjust the template.
- Preview server: `Services/PreviewHttpServer.cs` uses `HttpListener` and serves the live template on a random localhost port. `MarkdownViewer` provides a convenient wrapper.

## Build

From the repository root:

```bash
dotnet build MarkdownEditor/MarkdownEditor.csproj -c Debug
```

## Migration notes (what changed in this branch)

- Removed server-side Markdown conversion (`Markdig`) — rendering moved to the browser via `marked.js`.
- Removed any dependency on `WebView2` / Chromium; preview is opened in the system default browser via a tiny HTTP server.
- Added `MarkdownViewer` for quick read-only display.

If you want, I can also provide a short sample application that demonstrates embedding the editor and using `MarkdownViewer` programmatically.

