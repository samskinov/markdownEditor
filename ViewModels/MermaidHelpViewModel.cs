using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using MarkdownEditor.Models;
using MarkdownEditor.Mvvm;
using MarkdownEditor.Services;

namespace MarkdownEditor.ViewModels
{
    public sealed class MermaidHelpViewModel : ViewModelBase
    {
        private readonly IReadOnlyList<MermaidExample> _allExamples = MermaidExampleProvider.GetExamples();
        private string _filterText = string.Empty;
        private IReadOnlyList<MermaidExample> _filteredExamples;

        public ICommand InsertExampleCommand { get; }

        public event Action<string>? InsertRequested;

        public MermaidHelpViewModel()
        {
            _filteredExamples = _allExamples;
            InsertExampleCommand = new RelayCommand(param =>
            {
                if (param is MermaidExample example)
                    InsertRequested?.Invoke(example.Code);
            });
        }

        public string FilterText
        {
            get => _filterText;
            set
            {
                if (SetProperty(ref _filterText, value))
                    ApplyFilter();
            }
        }

        public IReadOnlyList<MermaidExample> FilteredExamples
        {
            get => _filteredExamples;
            private set => SetProperty(ref _filteredExamples, value);
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(_filterText))
            {
                FilteredExamples = _allExamples;
                return;
            }

            FilteredExamples = _allExamples
                .Where(e => e.Name.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0
                         || e.Description.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }
    }
}
