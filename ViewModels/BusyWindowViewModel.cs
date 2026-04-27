using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Threading;
using MarkdownEditor.Mvvm;

namespace MarkdownEditor.ViewModels
{
    /// <summary>Collapses when the string is null or empty.</summary>
    public sealed class StringNotEmptyToVisibilityConverter : IValueConverter
    {
        public static readonly StringNotEmptyToVisibilityConverter Instance = new();
        public object Convert(object value, System.Type t, object p, CultureInfo c)
            => value is string s && !string.IsNullOrWhiteSpace(s) ? Visibility.Visible : Visibility.Collapsed;
        public object ConvertBack(object v, System.Type t, object p, CultureInfo c) => throw new System.NotSupportedException();
    }

    /// <summary>
    /// ViewModel for the BusyWindow progress dialog.
    /// The calling application creates an instance, shows the window, then calls
    /// <see cref="AddStep"/> and <see cref="Complete"/> from any thread.
    /// </summary>
    public sealed class BusyWindowViewModel : ViewModelBase
    {
        private readonly Dispatcher _dispatcher;
        private string _title;
        private string _subtitle;
        private bool _isCompleted;

        public BusyWindowViewModel(string title = "Processing…", string subtitle = "")
        {
            _dispatcher = Application.Current?.Dispatcher ?? Dispatcher.CurrentDispatcher;
            _title = title;
            _subtitle = subtitle;
            Steps = new ObservableCollection<BusyStep>();
        }

        public ObservableCollection<BusyStep> Steps { get; }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Subtitle
        {
            get => _subtitle;
            set => SetProperty(ref _subtitle, value);
        }

        public bool IsCompleted
        {
            get => _isCompleted;
            private set
            {
                if (SetProperty(ref _isCompleted, value))
                    OnPropertyChanged(nameof(IsInProgress));
            }
        }

        public bool IsInProgress => !_isCompleted;

        /// <summary>Appends a progress step message. Thread-safe.</summary>
        public void AddStep(string message) => AddStep(message, BusyStepKind.Info);

        /// <summary>Appends a step with explicit kind. Thread-safe.</summary>
        public void AddStep(string message, BusyStepKind kind)
        {
            _dispatcher.InvokeAsync(() => Steps.Add(new BusyStep(message ?? string.Empty, kind)));
        }

        /// <summary>
        /// Marks the process as completed, enabling the Close button.
        /// Optionally appends a final summary message. Thread-safe.
        /// </summary>
        public void Complete(string? finalMessage = null)
        {
            _dispatcher.InvokeAsync(() =>
            {
                if (finalMessage != null)
                    Steps.Add(new BusyStep(finalMessage, BusyStepKind.Success));
                IsCompleted = true;
            });
        }

        /// <summary>Marks the process as failed. Thread-safe.</summary>
        public void Fail(string errorMessage)
        {
            _dispatcher.InvokeAsync(() =>
            {
                Steps.Add(new BusyStep(errorMessage, BusyStepKind.Error));
                IsCompleted = true;
            });
        }
    }

    public enum BusyStepKind { Info, Success, Warning, Error }

    public sealed class BusyStep
    {
        public BusyStep(string message, BusyStepKind kind)
        {
            Message = message;
            Kind = kind;
            Icon = kind switch
            {
                BusyStepKind.Success => "✔",
                BusyStepKind.Warning => "⚠",
                BusyStepKind.Error   => "✖",
                _                    => "→"
            };
            IconColor = kind switch
            {
                BusyStepKind.Success => "#10B981",
                BusyStepKind.Warning => "#F59E0B",
                BusyStepKind.Error   => "#EF4444",
                _                    => "#6366F1"
            };
        }

        public string Message { get; }
        public BusyStepKind Kind { get; }
        public string Icon { get; }
        public string IconColor { get; }
    }
}
