using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniNotifier.Avalonia.Services.Implementations;
using MiniNotifier.Avalonia.ViewModels;
using MiniNotifier.Avalonia.Views;
using MiniNotifier.Helpers;
using MiniNotifier.Repositories.Implementations;
using MiniNotifier.Repositories.Interfaces;
using MiniNotifier.Services.Implementations;
using MiniNotifier.Services.Interfaces;

namespace MiniNotifier.Avalonia;

public partial class App : Application
{
    private IHost? _host;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override async void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = global::Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
            desktop.Exit += OnDesktopExit;

            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddSingleton<IHydrationSettingsRepository, JsonHydrationSettingsRepository>();
                    services.AddSingleton<IAutoStartService, RegistryAutoStartService>();
                    services.AddSingleton<IHydrationSettingsService, HydrationSettingsService>();
                    services.AddSingleton<IMouseActivityService, MouseActivityService>();
                    services.AddSingleton<IReminderMessageService, ReminderMessageService>();
                    services.AddSingleton<IWindowManager, AvaloniaWindowManager>();
                    services.AddSingleton<IReminderPreviewService, AvaloniaReminderPreviewService>();
                    services.AddSingleton<IReminderSchedulerService, AvaloniaReminderSchedulerService>();

                    services.AddSingleton<MainWindowViewModel>();
                    services.AddTransient<ReminderWindowViewModel>();

                    services.AddSingleton<MainWindow>();
                    services.AddTransient<ReminderWindow>();
                })
                .Build();

            await _host.StartAsync();

            try
            {
                _host.Services.GetRequiredService<IMouseActivityService>().Initialize();
            }
            catch (Exception ex)
            {
                AppDiagnostics.LogException("App.MouseActivity.Initialize", ex);
            }

            var mainWindowViewModel = _host.Services.GetRequiredService<MainWindowViewModel>();
            DataContext = mainWindowViewModel;

            var mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = mainWindowViewModel;

            var windowManager = (AvaloniaWindowManager)_host.Services.GetRequiredService<IWindowManager>();
            windowManager.Attach(desktop, mainWindow);

            await mainWindowViewModel.InitializeAsync();

            _host.Services.GetRequiredService<IReminderSchedulerService>().Initialize();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private async void OnDesktopExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        if (_host is not null)
        {
            try
            {
                await _host.StopAsync();
            }
            finally
            {
                _host.Dispose();
                _host = null;
            }
        }
    }
}
