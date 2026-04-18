using Avalonia;
using Avalonia.Controls;
using MiniNotifier.Avalonia.ViewModels;
using MiniNotifier.Models.DTOs;

namespace MiniNotifier.Avalonia.Views;

public partial class ReminderWindow : Window
{
    private ReminderWindowViewModel? _viewModel;

    public ReminderWindow()
    {
        InitializeComponent();

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
        Position = new PixelPoint(
            workingArea.X + Math.Max(0, workingArea.Width - (int)Width - 24),
            workingArea.Y + Math.Max(0, workingArea.Height - (int)Height - 24));
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _viewModel?.Dispose();
    }
}
