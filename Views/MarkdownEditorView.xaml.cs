using System;
using System.Diagnostics;
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
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _foldingTimer.Stop();
            StopPreviewServer();
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
            }
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
