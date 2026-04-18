using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using MiniNotifier.Avalonia.Views;
using MiniNotifier.Services.Interfaces;

namespace MiniNotifier.Avalonia.Services.Implementations;

public sealed class AvaloniaWindowManager : IWindowManager
{
    private IClassicDesktopStyleApplicationLifetime? _desktop;
    private MainWindow? _mainWindow;

    public void Attach(IClassicDesktopStyleApplicationLifetime desktop, MainWindow mainWindow)
    {
        _desktop = desktop;
        _mainWindow = mainWindow;
    }

    public void ShowSettingsWindow()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (_mainWindow is null)
            {
                return;
            }

            if (_desktop is not null && _desktop.MainWindow is null)
            {
                _desktop.MainWindow = _mainWindow;
            }

            if (!_mainWindow.IsVisible)
            {
                _mainWindow.Show();
            }

            if (_mainWindow.WindowState == WindowState.Minimized)
            {
                _mainWindow.WindowState = WindowState.Normal;
            }

            _mainWindow.Activate();
        });
    }

    public void ShutdownApplication()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _mainWindow?.PrepareForExit();
            _desktop?.Shutdown();
        });
    }
}
