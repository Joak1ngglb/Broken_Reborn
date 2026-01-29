using System;
using System.Windows;

namespace Intersect.Editor.Forms.WpfWindows;

public sealed partial class TranslationQueueWindow : Window
{
    public TranslationQueueWindow()
    {
        InitializeComponent();
        DataContext = new TranslationQueueViewModel();
        Closed += OnClosed;
    }

    public void ApplyFilter(string? entityType, string? entityId, string? searchText = null)
    {
        if (DataContext is TranslationQueueViewModel viewModel)
        {
            viewModel.ApplyFilter(entityType, entityId, searchText);
        }
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
