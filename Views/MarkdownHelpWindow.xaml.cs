using System;
using System.Windows;
using MarkdownEditor.ViewModels;

namespace MarkdownEditor.Views
{
    public partial class MarkdownHelpWindow : Window
    {
        private readonly MarkdownHelpViewModel _viewModel;

        /// <summary>
        /// Raised when user clicks "Insert" on a snippet.
        /// The string is the Markdown syntax to insert.
        /// </summary>
        public event Action<string>? InsertRequested;

        public MarkdownHelpWindow()
        {
            InitializeComponent();
            _viewModel = new MarkdownHelpViewModel();
            _viewModel.InsertRequested += syntax => InsertRequested?.Invoke(syntax);
            DataContext = _viewModel;
        }
    }
}
