using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Data;
using MarkdownEditor.ViewModels;

namespace MarkdownEditor.Views
{
    public partial class BusyWindow : Window
    {
        public BusyWindow(BusyWindowViewModel viewModel, Window? owner = null)
        {
            InitializeComponent();
            DataContext = viewModel;
            if (owner != null) Owner = owner;

            viewModel.Steps.CollectionChanged += OnStepsChanged;
            viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnStepsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            StepsScrollViewer.ScrollToBottom();
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(BusyWindowViewModel.Title))
                Title = ((BusyWindowViewModel)DataContext).Title;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
