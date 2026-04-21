using System;
using System.Windows;
using MarkdownEditor.ViewModels;

namespace MarkdownEditor.Views
{
    public partial class MermaidHelpWindow : Window
    {
        private readonly MermaidHelpViewModel _viewModel;

        /// <summary>
        /// Raised when user clicks "Insert" on an example.
        /// The string is the Mermaid code to insert.
        /// </summary>
        public event Action<string>? InsertRequested;

        public MermaidHelpWindow()
        {
            InitializeComponent();
            _viewModel = new MermaidHelpViewModel();
            _viewModel.InsertRequested += code => InsertRequested?.Invoke(code);
            DataContext = _viewModel;
        }
    }
}
