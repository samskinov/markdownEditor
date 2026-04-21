using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using MarkdownEditor.Models;
using MarkdownEditor.Mvvm;
using MarkdownEditor.Services;

namespace MarkdownEditor.ViewModels
{
    public sealed class MarkdownHelpViewModel : ViewModelBase
    {
        private readonly IReadOnlyList<MarkdownSnippet> _allSnippets = MarkdownSnippetProvider.GetSnippets();
        private string _filterText = string.Empty;
        private IReadOnlyList<MarkdownSnippet> _filteredSnippets;

        public ICommand InsertSnippetCommand { get; }

        public event Action<string>? InsertRequested;

        public MarkdownHelpViewModel()
        {
            _filteredSnippets = _allSnippets;
            InsertSnippetCommand = new RelayCommand(param =>
            {
                if (param is MarkdownSnippet snippet)
                    InsertRequested?.Invoke(snippet.Syntax);
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

        public IReadOnlyList<MarkdownSnippet> FilteredSnippets
        {
            get => _filteredSnippets;
            private set => SetProperty(ref _filteredSnippets, value);
        }

        private void ApplyFilter()
        {
            if (string.IsNullOrWhiteSpace(_filterText))
            {
                FilteredSnippets = _allSnippets;
                return;
            }

            FilteredSnippets = _allSnippets
                .Where(s => s.Name.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0
                         || s.Description.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();
        }
    }
}
