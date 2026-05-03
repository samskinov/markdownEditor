using System.Windows;
using System.Windows.Threading;
using MarkdownEditor.Mvvm;
using MarkdownEditor.Services;

namespace MarkdownEditor.ViewModels
{
    public sealed class MermaidFixPromptViewModel : ViewModelBase
    {
        private string _prompt       = string.Empty;
        private string _mermaidCode  = string.Empty;
        private string _response     = string.Empty;
        private string _copyButtonText = "📋  Copy to Clipboard";
        private bool   _isCopied;

        private readonly DispatcherTimer _resetTimer;

        public MermaidFixPromptViewModel(string mermaidCode, string prompt, MermaidDiagramType diagramType)
        {
            _mermaidCode  = mermaidCode;
            _prompt       = prompt;
            DiagramType   = diagramType;

            // Rough token estimate: ~4 chars per token
            PromptTokenEstimate = prompt.Length / 4;

            CopyCommand = new RelayCommand(OnCopy);

            _resetTimer = new DispatcherTimer { Interval = System.TimeSpan.FromSeconds(2) };
            _resetTimer.Tick += (_, __) =>
            {
                _resetTimer.Stop();
                CopyButtonText = "📋  Copy to Clipboard";
                IsCopied = false;
            };
        }

        // ── Read-only info ────────────────────────────────────────────────────

        public MermaidDiagramType DiagramType { get; }

        public string DetectedTypeDisplay
            => MermaidDiagramTypeDetector.ToDisplayName(DiagramType);

        public int PromptTokenEstimate { get; }

        public string MermaidCode
        {
            get => _mermaidCode;
            private set => SetProperty(ref _mermaidCode, value);
        }

        public string Prompt
        {
            get => _prompt;
            private set => SetProperty(ref _prompt, value);
        }

        // ── Copy ─────────────────────────────────────────────────────────────

        public string CopyButtonText
        {
            get => _copyButtonText;
            private set => SetProperty(ref _copyButtonText, value);
        }

        public bool IsCopied
        {
            get => _isCopied;
            private set => SetProperty(ref _isCopied, value);
        }

        public RelayCommand CopyCommand { get; }

        private void OnCopy()
        {
            try
            {
                Clipboard.SetText(_prompt);
                CopyButtonText = "✅  Copied!";
                IsCopied = true;
                _resetTimer.Stop();
                _resetTimer.Start();
            }
            catch
            {
                CopyButtonText = "⚠️  Copy failed";
            }
        }

        // ── Response (pasted AI reply) ────────────────────────────────────────

        public string Response
        {
            get => _response;
            set
            {
                if (SetProperty(ref _response, value))
                    OnPropertyChanged(nameof(HasResponse));
            }
        }

        public bool HasResponse => !string.IsNullOrWhiteSpace(_response);
    }
}
