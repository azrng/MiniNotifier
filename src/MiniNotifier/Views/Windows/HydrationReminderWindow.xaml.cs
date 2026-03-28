using System.Windows;
using MiniNotifier.Helpers;
using MiniNotifier.Models.DTOs;
using MiniNotifier.ViewModels.Windows;

namespace MiniNotifier.Views.Windows;

public partial class HydrationReminderWindow
{
    public HydrationReminderWindow(HydrationReminderViewModel viewModel)
    {
        ViewModel = viewModel;

        InitializeComponent();

        DataContext = ViewModel;
        Icon = AppIconProvider.LoadWindowIcon();
        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    public HydrationReminderViewModel ViewModel { get; }

    public void Prepare(HydrationSettingsDto settings)
    {
        ViewModel.Initialize(settings);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var workArea = SystemParameters.WorkArea;
        MaxWidth = Math.Max(328, Math.Min(420, workArea.Width - 36));
        MaxHeight = Math.Max(240, workArea.Height - 36);
        MessageScrollViewer.MaxHeight = Math.Max(88, Math.Min(180, workArea.Height * 0.28));

        UpdateLayout();

        Left = Math.Max(workArea.Left + 18, workArea.Right - ActualWidth - 18);
        Top = Math.Max(workArea.Top + 18, workArea.Bottom - ActualHeight - 18);
        ViewModel.RequestClose += OnRequestClose;
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        ViewModel.RequestClose -= OnRequestClose;
        Loaded -= OnLoaded;
        Closed -= OnClosed;
    }

    private void OnRequestClose(object? sender, EventArgs e)
    {
        Close();
    }
}
