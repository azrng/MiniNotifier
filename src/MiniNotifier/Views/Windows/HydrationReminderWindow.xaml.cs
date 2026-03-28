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
        Icon = AppIconFactory.CreateWindowIcon();
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
        Left = workArea.Right - ActualWidth - 18;
        Top = workArea.Bottom - ActualHeight - 18;
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
