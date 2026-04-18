using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;

namespace MiniNotifier.Avalonia;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        using var singleInstance = SingleInstanceCoordinator.CreatePrimary();

        if (!singleInstance.IsPrimary)
        {
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args, ShutdownMode.OnExplicitShutdown);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .LogToTrace();
}
