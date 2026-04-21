using System;
using System.Collections.Generic;
using System.Windows.Input;
using System.Windows.Threading;
using MarkdownEditor.Mvvm;
using MarkdownEditor.Services;

namespace MarkdownEditor.ViewModels
{
    public sealed class MarkdownEditorViewModel : ViewModelBase
    {
        private readonly DispatcherTimer _debounceTimer;
        private string _markdownText = string.Empty;
        private bool _isModified;
        private int _lineCount;
        private int _charCount;
        private int _cursorLine = 1;
        private int _cursorColumn = 1;
        private bool _isMermaidHelpOpen;
        private bool _isMarkdownHelpOpen;
        private bool _isPreviewActive;
        private IReadOnlyList<TocEntry> _tocEntries = new List<TocEntry>();
        private bool _isTocOpen;

        public MarkdownEditorViewModel()
        {
            _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
            _debounceTimer.Tick += OnDebounceTimerTick;

            SaveCommand = new RelayCommand(OnSave, () => IsModified);
            UndoCommand = new RelayCommand(() => RequestUndo?.Invoke());
            RedoCommand = new RelayCommand(() => RequestRedo?.Invoke());
            InsertBoldCommand = new RelayCommand(() => RequestInsertSurround?.Invoke("**", "**", "bold text"));
            InsertItalicCommand = new RelayCommand(() => RequestInsertSurround?.Invoke("*", "*", "italic text"));
            InsertH1Command = new RelayCommand(() => RequestInsertAtLineStart?.Invoke("# "));
            InsertH2Command = new RelayCommand(() => RequestInsertAtLineStart?.Invoke("## "));
            InsertH3Command = new RelayCommand(() => RequestInsertAtLineStart?.Invoke("### "));
            InsertBulletListCommand = new RelayCommand(() => RequestInsertText?.Invoke("- Item\n- Item\n- Item\n"));
            InsertNumberedListCommand = new RelayCommand(() => RequestInsertText?.Invoke("1. First\n2. Second\n3. Third\n"));
            InsertChecklistCommand = new RelayCommand(() => RequestInsertText?.Invoke("- [x] Completed task\n- [ ] Pending task\n- [ ] Another task\n"));
            InsertQuoteCommand = new RelayCommand(() => RequestInsertAtLineStart?.Invoke("> "));
            InsertInlineCodeCommand = new RelayCommand(() => RequestInsertSurround?.Invoke("`", "`", "code"));
            InsertCodeBlockCommand = new RelayCommand(() => RequestInsertText?.Invoke("```csharp\n// your code here\n```\n"));
            InsertLinkCommand = new RelayCommand(() => RequestInsertText?.Invoke("[Link text](https://example.com)"));
            InsertImageCommand = new RelayCommand(() => RequestInsertText?.Invoke("![Alt text](https://example.com/image.png)"));
            InsertTableCommand = new RelayCommand(() => RequestInsertText?.Invoke("| Column 1 | Column 2 | Column 3 |\n|-----------|-----------|----------|\n| Cell      | Cell      | Cell     |\n| Cell      | Cell      | Cell     |\n"));
            InsertHorizontalRuleCommand = new RelayCommand(() => RequestInsertText?.Invoke("\n---\n"));
            InsertMermaidBlockCommand = new RelayCommand(() => RequestInsertText?.Invoke("```mermaid\ngraph TD\n    A[Start] --> B{Condition ?}\n    B -->|Yes| C[Action 1]\n    B -->|No| D[Action 2]\n    C --> E[End]\n    D --> E\n```\n"));
            ToggleMermaidHelpCommand = new RelayCommand(() => IsMermaidHelpOpen = !IsMermaidHelpOpen);
            ToggleMarkdownHelpCommand = new RelayCommand(() => IsMarkdownHelpOpen = !IsMarkdownHelpOpen);

            MarkdownHelpVM = new MarkdownHelpViewModel();
            MarkdownHelpVM.InsertRequested += syntax => RequestInsertText?.Invoke(syntax);
            MermaidHelpVM = new MermaidHelpViewModel();
            MermaidHelpVM.InsertRequested += code => RequestInsertText?.Invoke(code);
            ToggleTocCommand = new RelayCommand(() => IsTocOpen = !IsTocOpen);
            NavigateToLineCommand = new RelayCommand(OnNavigateToLine);
            OpenPreviewCommand = new RelayCommand(OnOpenPreview);

            MarkdownText = GetDefaultDocument();
            IsModified = false;
        }

        // Events for the host application
        public event Action? SaveRequested;

        // Event raised when user clicks "Preview" to open browser preview
        public event Action? RequestOpenPreview;

        // Events for the View (editor integration)
        public event Action? RequestUndo;
        public event Action? RequestRedo;
        public event Action<string>? RequestInsertText;
        public event Action<string, string, string>? RequestInsertSurround;
        public event Action<string>? RequestInsertAtLineStart;
        public event Action<int>? RequestNavigateToLine;

        // Event raised when Markdown content changes (raw markdown, rendered client-side)
        public event Action<string>? MarkdownContentChanged;

        public string MarkdownText
        {
            get => _markdownText;
            set
            {
                if (SetProperty(ref _markdownText, value))
                {
                    IsModified = true;
                    UpdateStats();
                    RestartDebounce();
                }
            }
        }

        public bool IsModified
        {
            get => _isModified;
            set => SetProperty(ref _isModified, value);
        }

        public int LineCount
        {
            get => _lineCount;
            private set => SetProperty(ref _lineCount, value);
        }

        public int CharCount
        {
            get => _charCount;
            private set => SetProperty(ref _charCount, value);
        }

        public int CursorLine
        {
            get => _cursorLine;
            set => SetProperty(ref _cursorLine, value);
        }

        public int CursorColumn
        {
            get => _cursorColumn;
            set => SetProperty(ref _cursorColumn, value);
        }

        /// <summary>
        /// Indicates whether the preview server is active (set by the code-behind).
        /// </summary>
        public bool IsPreviewActive
        {
            get => _isPreviewActive;
            set => SetProperty(ref _isPreviewActive, value);
        }

        public string StatusLabel => IsModified ? "Modified" : "Saved";

        public bool IsMermaidHelpOpen
        {
            get => _isMermaidHelpOpen;
            set
            {
                if (SetProperty(ref _isMermaidHelpOpen, value))
                    OnPropertyChanged(nameof(IsAnyHelpOpen));
            }
        }

        public bool IsMarkdownHelpOpen
        {
            get => _isMarkdownHelpOpen;
            set
            {
                if (SetProperty(ref _isMarkdownHelpOpen, value))
                    OnPropertyChanged(nameof(IsAnyHelpOpen));
            }
        }

        public bool IsAnyHelpOpen => IsMarkdownHelpOpen || IsMermaidHelpOpen;

        public MarkdownHelpViewModel MarkdownHelpVM { get; }
        public MermaidHelpViewModel MermaidHelpVM { get; }

        public bool IsTocOpen
        {
            get => _isTocOpen;
            set => SetProperty(ref _isTocOpen, value);
        }

        public IReadOnlyList<TocEntry> TocEntries
        {
            get => _tocEntries;
            private set => SetProperty(ref _tocEntries, value);
        }

        // Commands
        public ICommand SaveCommand { get; }
        public ICommand UndoCommand { get; }
        public ICommand RedoCommand { get; }
        public ICommand InsertBoldCommand { get; }
        public ICommand InsertItalicCommand { get; }
        public ICommand InsertH1Command { get; }
        public ICommand InsertH2Command { get; }
        public ICommand InsertH3Command { get; }
        public ICommand InsertBulletListCommand { get; }
        public ICommand InsertNumberedListCommand { get; }
        public ICommand InsertChecklistCommand { get; }
        public ICommand InsertQuoteCommand { get; }
        public ICommand InsertInlineCodeCommand { get; }
        public ICommand InsertCodeBlockCommand { get; }
        public ICommand InsertLinkCommand { get; }
        public ICommand InsertImageCommand { get; }
        public ICommand InsertTableCommand { get; }
        public ICommand InsertHorizontalRuleCommand { get; }
        public ICommand InsertMermaidBlockCommand { get; }
        public ICommand ToggleMermaidHelpCommand { get; }
        public ICommand ToggleMarkdownHelpCommand { get; }
        public ICommand ToggleTocCommand { get; }
        public ICommand NavigateToLineCommand { get; }
        public ICommand OpenPreviewCommand { get; }

        /// <summary>
        /// Load markdown content from an external source (e.g. database).
        /// </summary>
        public void LoadContent(string markdown)
        {
            _markdownText = markdown ?? string.Empty;
            OnPropertyChanged(nameof(MarkdownText));
            IsModified = false;
            UpdateStats();
            NotifyContentChanged();
        }

        /// <summary>
        /// Get the current markdown text (e.g. for saving to database).
        /// </summary>
        public string GetContent() => MarkdownText;

        /// <summary>
        /// Mark as saved after an external save operation completes.
        /// </summary>
        public void MarkAsSaved()
        {
            IsModified = false;
            OnPropertyChanged(nameof(StatusLabel));
        }

        private void OnSave()
        {
            SaveRequested?.Invoke();
        }

        private void OnOpenPreview()
        {
            RequestOpenPreview?.Invoke();
        }

        private void OnNavigateToLine()
        {
        }

        public void NavigateToLine(int lineNumber)
        {
            RequestNavigateToLine?.Invoke(lineNumber);
        }

        private void RestartDebounce()
        {
            _debounceTimer.Stop();
            _debounceTimer.Start();
        }

        private void OnDebounceTimerTick(object? sender, EventArgs e)
        {
            _debounceTimer.Stop();
            NotifyContentChanged();
        }

        private void NotifyContentChanged()
        {
            MarkdownContentChanged?.Invoke(MarkdownText);
        }

        private void UpdateStats()
        {
            var text = MarkdownText ?? string.Empty;
            CharCount = text.Length;
            LineCount = text.Length == 0 ? 1 : CountLines(text);
            TocEntries = TocExtractor.Extract(text);
            OnPropertyChanged(nameof(StatusLabel));
        }

        private static int CountLines(string text)
        {
            int count = 1;
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '\n') count++;
            }
            return count;
        }

        private static string GetDefaultDocument()
        {
            return @"# Welcome to the Markdown Editor

This editor supports full **Markdown** and **Mermaid** diagrams.

## Features

- ✏️ Live Markdown editing with preview
- 📊 Integrated Mermaid diagrams
- 🎨 Modern, productive UI

## Code example

```csharp
public class Hello
{
    public static void Main()
    {
        Console.WriteLine(""Hello, Markdown!"");
    }
}
```

## Diagram example

```mermaid
graph LR
    A[Write Markdown] --> B[Live preview]
    B --> C{Satisfied?}
    C -->|Yes| D[Save]
    C -->|No| A
```

## Table

| Feature        | Status |
|----------------|--------|
| Markdown       | ✅     |
| Mermaid        | ✅     |
| Export         | 🔜     |

> **Tip:** Use the toolbar to quickly insert Markdown and Mermaid elements.

---

Happy writing!
";
        }
    }
}
