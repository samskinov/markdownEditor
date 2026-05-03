using System.Windows;
using MarkdownEditor.Services;
using MarkdownEditor.ViewModels;

namespace MarkdownEditor.Views
{
    public partial class MermaidFixPromptWindow : Window
    {
        /// <summary>
        /// Set by the Apply button. Contains the extracted / cleaned Mermaid code
        /// ready to be inserted into the editor, or null when Apply was not used.
        /// </summary>
        public string? ExtractedCode { get; private set; }

        public MermaidFixPromptWindow(MermaidFixPromptViewModel viewModel, Window? owner = null)
        {
            InitializeComponent();
            DataContext = viewModel;
            if (owner != null) Owner = owner;
        }

        private void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = (MermaidFixPromptViewModel)DataContext;
            var parsed = MermaidPromptService.ParseResponse(vm.Response);
            if (parsed is null)
            {
                MessageBox.Show(
                    "Could not extract Mermaid code from the response.\n\n" +
                    "Make sure the AI wrapped its output between <<<MERMAID>>> and <<<END>>> markers, " +
                    "or inside ```mermaid fences.",
                    "Apply — Nothing to Extract",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            ExtractedCode = parsed;
            DialogResult = true;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
