using System.Windows;
using MiniNotifier.Services.Interfaces;
using MiniNotifier.Views.Windows;

namespace MiniNotifier.Services.Implementations;

public sealed class WindowManager(MainWindow mainWindow) : IWindowManager
{
    public void ShowSettingsWindow()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (!mainWindow.IsVisible)
            {
                mainWindow.Show();
            }

            if (mainWindow.WindowState == WindowState.Minimized)
            {
                mainWindow.WindowState = WindowState.Normal;
            }

            mainWindow.Activate();
            mainWindow.Topmost = true;
            mainWindow.Topmost = false;
            mainWindow.Focus();
        });
    }

    public void ShutdownApplication()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            mainWindow.PrepareForExit();
            Application.Current.Shutdown();
        });
    }
}
