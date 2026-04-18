using Avalonia;
using Avalonia.Controls;
using MiniNotifier.Avalonia.ViewModels;
using MiniNotifier.Models.DTOs;

namespace MiniNotifier.Avalonia.Views;

public partial class ReminderWindow : Window
{
    private const double ScreenPadding = 18;
    private const double PopupMinWidth = 328;
    private const double PopupMaxWidth = 420;
    private const double PopupMinHeight = 240;
    private const double MessageMinHeight = 88;
    private const double MessageMaxHeight = 180;

    private readonly ScrollViewer? _messageScrollViewer;
    private ReminderWindowViewModel? _viewModel;

    public ReminderWindow()
    {
        InitializeComponent();

        _messageScrollViewer = this.FindControl<ScrollViewer>("MessageScrollViewer");
        Opened += OnOpened;
        Closed += OnClosed;
    }

    public ReminderWindow(ReminderWindowViewModel viewModel) : this()
    {
        _viewModel = viewModel;
        DataContext = _viewModel;
    }

    public void Prepare(HydrationSettingsDto settings)
    {
        _viewModel?.Prepare(settings, Close);
    }

    private void OnOpened(object? sender, EventArgs e)
    {
        var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
        if (screen is null)
        {
            return;
        }

        var workingArea = screen.WorkingArea;
        ApplyWorkingAreaConstraints(workingArea);
        UpdateLayout();

        var popupWidth = Math.Ceiling(Bounds.Width);
        var popupHeight = Math.Ceiling(Bounds.Height);

        Position = new PixelPoint(
            workingArea.X + Math.Max((int)ScreenPadding, workingArea.Width - (int)popupWidth - (int)ScreenPadding),
            workingArea.Y + Math.Max((int)ScreenPadding, workingArea.Height - (int)popupHeight - (int)ScreenPadding));
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel?.Dispose();
    }

    private void ApplyWorkingAreaConstraints(PixelRect workingArea)
    {
        MaxWidth = Math.Max(PopupMinWidth, Math.Min(PopupMaxWidth, workingArea.Width - ScreenPadding * 2));
        MaxHeight = Math.Max(PopupMinHeight, workingArea.Height - ScreenPadding * 2);

        if (_messageScrollViewer is not null)
        {
            _messageScrollViewer.MaxHeight = Math.Max(
                MessageMinHeight,
                Math.Min(MessageMaxHeight, workingArea.Height * 0.28));
        }
    }
}
