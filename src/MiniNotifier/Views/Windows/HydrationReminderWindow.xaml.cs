using System.Windows;
using System.Windows.Threading;
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

    public void WarmUpLayout()
    {
        ApplyPopupLayoutBounds();
        ApplyTemplate();
        PopupRoot.ApplyTemplate();
        MessageScrollViewer.ApplyTemplate();
        Measure(new Size(MaxWidth, MaxHeight));
        Arrange(new Rect(0, 0, Math.Max(DesiredSize.Width, 1), Math.Max(DesiredSize.Height, 1)));
        UpdateLayout();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        ApplyPopupLayoutBounds();
        PositionNearWorkArea();
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(PositionNearWorkArea));
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

    private void ApplyPopupLayoutBounds()
    {
        var workArea = SystemParameters.WorkArea;
        MaxWidth = Math.Max(328, Math.Min(420, workArea.Width - 36));
        MaxHeight = Math.Max(240, workArea.Height - 36);
        MessageScrollViewer.MaxHeight = Math.Max(88, Math.Min(180, workArea.Height * 0.28));
    }

    private void PositionNearWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        var width = ActualWidth > 0 ? ActualWidth : Math.Min(MaxWidth, 420);
        var height = ActualHeight > 0 ? ActualHeight : Math.Min(MaxHeight, 240);

        Left = Math.Max(workArea.Left + 18, workArea.Right - width - 18);
        Top = Math.Max(workArea.Top + 18, workArea.Bottom - height - 18);
    }
}
