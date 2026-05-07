using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using ICSharpCode.AvalonEdit.Folding;
using ICSharpCode.AvalonEdit.Search;
using MarkdownEditor.Services;
using MarkdownEditor.ViewModels;

namespace MarkdownEditor.Views
{
    public partial class MarkdownEditorView : UserControl
    {
        private MarkdownEditorViewModel? _viewModel;
        private bool _isUpdatingFromViewModel;
        private PreviewHttpServer? _previewServer;
        private FoldingManager? _foldingManager;
        private MarkdownFoldingStrategy? _foldingStrategy;
        private readonly DispatcherTimer _foldingTimer;
        private Window? _hostWindow;

        public MarkdownEditorView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            _foldingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.5) };
            _foldingTimer.Tick += OnFoldingTimerTick;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            MarkdownTextEditor.SyntaxHighlighting = MarkdownHighlightingDefinition.Get();
            SearchPanel.Install(MarkdownTextEditor);

            _foldingManager = FoldingManager.Install(MarkdownTextEditor.TextArea);
            _foldingStrategy = new MarkdownFoldingStrategy();
            _foldingStrategy.UpdateFoldings(_foldingManager, MarkdownTextEditor.Document);
            _foldingTimer.Start();

            _hostWindow = Window.GetWindow(this);
            if (_hostWindow != null)
                _hostWindow.Closing += OnHostWindowClosing;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _foldingTimer.Stop();
            StopPreviewServer();
            if (_hostWindow != null)
            {
                _hostWindow.Closing -= OnHostWindowClosing;
                _hostWindow = null;
            }
        }

        private void OnFoldingTimerTick(object? sender, EventArgs e)
        {
            if (_foldingManager != null && _foldingStrategy != null)
            {
                _foldingStrategy.UpdateFoldings(_foldingManager, MarkdownTextEditor.Document);
            }
        }

        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (_viewModel != null)
            {
                _viewModel.MarkdownContentChanged -= OnMarkdownContentChanged;
                _viewModel.RequestUndo -= OnRequestUndo;
                _viewModel.RequestRedo -= OnRequestRedo;
                _viewModel.RequestInsertText -= OnRequestInsertText;
                _viewModel.RequestInsertSurround -= OnRequestInsertSurround;
                _viewModel.RequestInsertAtLineStart -= OnRequestInsertAtLineStart;
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.RequestOpenPreview -= OnRequestOpenPreview;
                _viewModel.RequestNavigateToLine -= OnRequestNavigateToLine;
                _viewModel.SaveFailed -= OnSaveFailed;
                _viewModel.RequestGenerateMermaidFixPrompt -= OnRequestGenerateMermaidFixPrompt;
                _viewModel.RequestEmbedImage -= OnRequestEmbedImage;
                _viewModel.RequestEmbedImageFromClipboard -= OnRequestEmbedImageFromClipboard;
            }

            _viewModel = e.NewValue as MarkdownEditorViewModel;

            if (_viewModel != null)
            {
                _viewModel.MarkdownContentChanged += OnMarkdownContentChanged;
                _viewModel.RequestUndo += OnRequestUndo;
                _viewModel.RequestRedo += OnRequestRedo;
                _viewModel.RequestInsertText += OnRequestInsertText;
                _viewModel.RequestInsertSurround += OnRequestInsertSurround;
                _viewModel.RequestInsertAtLineStart += OnRequestInsertAtLineStart;
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
                _viewModel.RequestOpenPreview += OnRequestOpenPreview;
                _viewModel.RequestNavigateToLine += OnRequestNavigateToLine;
                _viewModel.SaveFailed += OnSaveFailed;
                _viewModel.RequestGenerateMermaidFixPrompt += OnRequestGenerateMermaidFixPrompt;
                _viewModel.RequestEmbedImage += OnRequestEmbedImage;
                _viewModel.RequestEmbedImageFromClipboard += OnRequestEmbedImageFromClipboard;

                _isUpdatingFromViewModel = true;
                MarkdownTextEditor.Text = _viewModel.MarkdownText;
                _isUpdatingFromViewModel = false;
            }

            MarkdownTextEditor.TextChanged -= OnEditorTextChanged;
            MarkdownTextEditor.TextChanged += OnEditorTextChanged;

            MarkdownTextEditor.TextArea.Caret.PositionChanged -= OnCaretPositionChanged;
            MarkdownTextEditor.TextArea.Caret.PositionChanged += OnCaretPositionChanged;

            MarkdownTextEditor.Options.HighlightCurrentLine = true;
        }

        private void OnEditorTextChanged(object? sender, EventArgs e)
        {
            if (_isUpdatingFromViewModel || _viewModel == null) return;
            _viewModel.MarkdownText = MarkdownTextEditor.Text;
        }

        private void OnCaretPositionChanged(object? sender, EventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.CursorLine = MarkdownTextEditor.TextArea.Caret.Line;
            _viewModel.CursorColumn = MarkdownTextEditor.TextArea.Caret.Column;
            _previewServer?.UpdateCursorLine(_viewModel.CursorLine);
        }

        // ─── Preview Server ──────────────────────────────────

        private void OnRequestOpenPreview()
        {
            EnsurePreviewServer();
            OpenBrowser(_previewServer!.Url);
        }

        private void EnsurePreviewServer()
        {
            if (_previewServer != null) return;

            _previewServer = new PreviewHttpServer();

            if (_viewModel != null && !string.IsNullOrEmpty(_viewModel.MarkdownText))
            {
                _previewServer.UpdateContent(_viewModel.MarkdownText);
            }

            if (_viewModel != null)
            {
                _viewModel.IsPreviewActive = true;
            }

            System.Diagnostics.Debug.WriteLine($"Preview server started on {_previewServer.Url}");
        }

        private void StopPreviewServer()
        {
            if (_previewServer == null) return;

            _previewServer.Dispose();
            _previewServer = null;

            if (_viewModel != null)
            {
                _viewModel.IsPreviewActive = false;
            }
        }

        private void OnMarkdownContentChanged(string markdown)
        {
            _previewServer?.UpdateContent(markdown);
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
                System.Diagnostics.Debug.WriteLine($"Cannot open browser: {ex.Message}");
                var message = $"Unable to open the browser automatically.\n\nPreview URL:\n{url}";
                try { Clipboard.SetText(url); message += "\n\n(URL copied to clipboard)"; }
                catch { }
                MessageBox.Show(message, "Preview", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnHostWindowClosing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_viewModel?.IsModified != true) return;
            var result = MessageBox.Show(
                "You have unsaved changes. Save before closing?",
                "Unsaved Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Warning);
            switch (result)
            {
                case MessageBoxResult.Yes:
                    _viewModel.SaveCommand.Execute(null);
                    break;
                case MessageBoxResult.Cancel:
                    e.Cancel = true;
                    break;
            }
        }

        private void OnSaveFailed(string message)
        {
            MessageBox.Show(
                $"Failed to save: {message}",
                "Save Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        // ─── Editor Insert Operations ────────────────────────

        private void OnRequestUndo()
        {
            MarkdownTextEditor.Undo();
        }

        private void OnRequestRedo()
        {
            MarkdownTextEditor.Redo();
        }

        private void OnRequestInsertText(string text)
        {
            var editor = MarkdownTextEditor;
            var offset = editor.CaretOffset;
            editor.Document.Insert(offset, text);
            editor.CaretOffset = offset + text.Length;
            editor.Focus();
        }

        private void OnRequestInsertSurround(string before, string after, string placeholder)
        {
            var editor = MarkdownTextEditor;
            var selection = editor.SelectedText;

            if (string.IsNullOrEmpty(selection))
            {
                var offset = editor.CaretOffset;
                var toInsert = before + placeholder + after;
                editor.Document.Insert(offset, toInsert);
                editor.Select(offset + before.Length, placeholder.Length);
            }
            else
            {
                var selStart = editor.SelectionStart;
                var selLength = editor.SelectionLength;
                var wrapped = before + selection + after;
                editor.Document.Replace(selStart, selLength, wrapped);
                editor.Select(selStart + before.Length, selection.Length);
            }
            editor.Focus();
        }

        private void OnRequestInsertAtLineStart(string prefix)
        {
            var editor = MarkdownTextEditor;
            var line = editor.Document.GetLineByOffset(editor.CaretOffset);
            editor.Document.Insert(line.Offset, prefix);
            editor.CaretOffset = line.Offset + prefix.Length;
            editor.Focus();
        }

        private void OnRequestNavigateToLine(int lineNumber)
        {
            if (lineNumber < 1 || lineNumber > MarkdownTextEditor.Document.LineCount) return;
            var line = MarkdownTextEditor.Document.GetLineByNumber(lineNumber);
            MarkdownTextEditor.ScrollTo(lineNumber, 1);
            MarkdownTextEditor.CaretOffset = line.Offset;
            MarkdownTextEditor.Select(line.Offset, line.Length);
            MarkdownTextEditor.Focus();
        }

        public void InsertTextAtCursor(string text)
        {
            OnRequestInsertText(text);
        }

        // ─── Help Windows ────────────────────────────────────

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (_viewModel == null) return;

            if (e.PropertyName == nameof(MarkdownEditorViewModel.MarkdownText))
            {
                if (_isUpdatingFromViewModel) return;
                var vmText = _viewModel.MarkdownText;
                if (MarkdownTextEditor.Text != vmText)
                {
                    _isUpdatingFromViewModel = true;
                    MarkdownTextEditor.Text = vmText;
                    _isUpdatingFromViewModel = false;
                }
            }
        }

        // ─── Mermaid Fix Prompt ───────────────────────────────

        private void OnRequestGenerateMermaidFixPrompt()
        {
            var markdown    = MarkdownTextEditor.Text;
            var caretOffset = MarkdownTextEditor.CaretOffset;

            var block = MermaidBlockExtractor.TryExtract(markdown, caretOffset);

            if (block == null || string.IsNullOrWhiteSpace(block.Content))
            {
                MessageBox.Show(
                    "No Mermaid block found at the current cursor position.\n\n" +
                    "Place the cursor inside a ```mermaid … ``` block and try again.",
                    "Fix Mermaid — No Block Detected",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var prompt = MermaidPromptService.BuildFixPrompt(block.Content, block.DiagramType);
            var vm     = new MermaidFixPromptViewModel(block.Content, prompt, block.DiagramType);
            var window = new MermaidFixPromptWindow(vm, Window.GetWindow(this));
            window.ShowDialog();

            if (window.ExtractedCode is { } fixedCode)
            {
                var updatedMarkdown = MermaidBlockExtractor.ReplaceBlockContent(markdown, block, fixedCode);
                MarkdownTextEditor.Text = updatedMarkdown;
            }
        }

        // ─── Embed Image ────────────────────────────────────

        private async void OnRequestEmbedImage()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp;*.tiff;*.tif",
                Title  = "Select an image to embed"
            };
            if (dlg.ShowDialog() != true) return;

            string dataUri;
            try
            {
                dataUri = await Task.Run(() => ImageEmbedder.EncodeFile(dlg.FileName));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not embed image: {ex.Message}",
                    "Embed image", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var sizeMB = dataUri.Length / (1024.0 * 1024.0);
            if (sizeMB > 2.0)
            {
                var warn = MessageBox.Show(
                    $"The encoded image is {sizeMB:F1} MB. Large embeddings may slow editing.\n\nEmbed anyway?",
                    "Large Image", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (warn != MessageBoxResult.Yes) return;
            }

            var altText = System.IO.Path.GetFileNameWithoutExtension(dlg.FileName);
            InsertEmbeddedImage(dataUri, altText);
        }

        private async void OnRequestEmbedImageFromClipboard()
        {
            if (!Clipboard.ContainsImage())
            {
                MessageBox.Show(
                    "No image found on the clipboard.\n\nCopy an image first (e.g. screenshot, Ctrl+C on an image).",
                    "Embed from clipboard",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var bitmapSource = Clipboard.GetImage();
            if (bitmapSource == null)
            {
                MessageBox.Show("Could not read the clipboard image.",
                    "Embed from clipboard", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Convert WPF BitmapSource → PNG bytes → ImageSharp.
            // Clipboard must be accessed on the UI thread; only the encoding is offloaded.
            string dataUri;
            try
            {
                var pngEncoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                pngEncoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmapSource));

                byte[] bytes;
                using (var pngStream = new System.IO.MemoryStream())
                {
                    pngEncoder.Save(pngStream);
                    bytes = pngStream.ToArray();
                }

                dataUri = await Task.Run(() =>
                {
                    using var ms = new System.IO.MemoryStream(bytes);
                    return ImageEmbedder.EncodeStream(ms);
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Could not embed clipboard image: {ex.Message}",
                    "Embed from clipboard", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            InsertEmbeddedImage(dataUri, "clipboard-image");
        }

        private void InsertEmbeddedImage(string dataUri, string altText)
        {
            var current = MarkdownTextEditor.Text;
            var parsed  = EmbeddedImagesBlock.Parse(current);
            var id      = EmbeddedImagesBlock.NextId(parsed.images);

            MarkdownTextEditor.Document.BeginUpdate();
            try
            {
                var insertion = $"![{altText}][{id}]";
                var caret = MarkdownTextEditor.CaretOffset;
                MarkdownTextEditor.Document.Insert(caret, insertion);
                MarkdownTextEditor.CaretOffset = caret + insertion.Length;

                var withRef = MarkdownTextEditor.Text;
                var updated = EmbeddedImagesBlock.Upsert(withRef, id, dataUri);
                if (updated != withRef)
                {
                    var keepCaret = MarkdownTextEditor.CaretOffset;
                    MarkdownTextEditor.Document.Replace(0, withRef.Length, updated);
                    if (keepCaret <= MarkdownTextEditor.Document.TextLength)
                        MarkdownTextEditor.CaretOffset = keepCaret;
                }
            }
            finally
            {
                MarkdownTextEditor.Document.EndUpdate();
            }

            // Immediately collapse the embedded-images fold (DefaultClosed = true
            // in the folding strategy only applies when the fold is first created).
            if (_foldingManager != null && _foldingStrategy != null)
                _foldingStrategy.UpdateFoldings(_foldingManager, MarkdownTextEditor.Document);

            MarkdownTextEditor.Focus();
        }

        // ─── TOC Navigation ──────────────────────────────────

        private void TocEntry_Click(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe && fe.DataContext is TocEntry entry && _viewModel != null)
            {
                _viewModel.NavigateToLine(entry.LineNumber);
            }
        }
    }
}
