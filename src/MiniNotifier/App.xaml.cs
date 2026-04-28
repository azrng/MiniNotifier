using System.Windows;
using System.Windows.Threading;
using System.Threading;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MiniNotifier.Helpers;
using MiniNotifier.Repositories.Implementations;
using MiniNotifier.Repositories.Interfaces;
using MiniNotifier.Services.Implementations;
using MiniNotifier.Services.Interfaces;
using MiniNotifier.ViewModels.Windows;
using MiniNotifier.Views.Windows;
using Wpf.Ui;
using Wpf.Ui.Appearance;

namespace MiniNotifier;

public partial class App : Application, IDisposable
{
    private const string SingleInstanceMutexName = "Local\\MiniNotifier.SingleInstance";

    private IHost? _host;
    private Mutex? _singleInstanceMutex;

    protected override async void OnStartup(StartupEventArgs e)
    {
        if (AppUpdateRunner.IsUpdateMode(e.Args))
        {
            var exitCode = await AppUpdateRunner.RunAsync(e.Args);
            Shutdown(exitCode);
            return;
        }

        _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
            Shutdown();
            return;
        }

        base.OnStartup(e);

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        ApplicationThemeManager.Apply(ApplicationTheme.Light);

        _host = Host.CreateDefaultBuilder(e.Args)
            .ConfigureServices(services =>
            {
                services.AddSingleton<IMessenger>(WeakReferenceMessenger.Default);
                services.AddSingleton<ISnackbarService, SnackbarService>();

                services.AddSingleton<IHydrationSettingsRepository, JsonHydrationSettingsRepository>();
                services.AddSingleton<IAutoStartService, RegistryAutoStartService>();
                services.AddSingleton<IHydrationSettingsService, HydrationSettingsService>();
                services.AddSingleton<IMouseActivityService, MouseActivityService>();
                services.AddSingleton<IWindowManager, WindowManager>();
                services.AddSingleton<ITrayService, TrayService>();
                services.AddSingleton<IReminderPreviewService, ReminderPreviewService>();
                services.AddSingleton<IReminderSchedulerService, ReminderSchedulerService>();
                services.AddSingleton<IReminderMessageService, ReminderMessageService>();
                services.AddSingleton<IApplicationUpdateService, ApplicationUpdateService>();

                services.AddSingleton<MainWindowViewModel>();
                services.AddTransient<HydrationReminderViewModel>();

                services.AddSingleton<MainWindow>();
                services.AddTransient<HydrationReminderWindow>();
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

        Current.MainWindow = _host.Services.GetRequiredService<MainWindow>();
        await _host.Services.GetRequiredService<MainWindowViewModel>().InitializeAsync();
        _host.Services.GetRequiredService<ITrayService>().Initialize();
        _host.Services.GetRequiredService<IReminderSchedulerService>().Initialize();
        _host.Services.GetRequiredService<IWindowManager>().ShowSettingsWindow();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            if (_host.Services.GetService<ITrayService>() is IDisposable disposableTray)
            {
                disposableTray.Dispose();
            }

            if (_host.Services.GetService<IReminderSchedulerService>() is IDisposable disposableScheduler)
            {
                disposableScheduler.Dispose();
            }

            await _host.StopAsync();
            _host.Dispose();
            _host = null;
        }

        ReleaseSingleInstanceMutex();

        base.OnExit(e);
    }

    public void Dispose()
    {
        _host?.Dispose();
        _host = null;
        ReleaseSingleInstanceMutex();
        GC.SuppressFinalize(this);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        AppDiagnostics.LogException("App.DispatcherUnhandledException", e.Exception);
        e.Handled = true;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception exception)
        {
            AppDiagnostics.LogException("AppDomain.UnhandledException", exception);
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        AppDiagnostics.LogException("TaskScheduler.UnobservedTaskException", e.Exception);
        e.SetObserved();
    }

    private void ReleaseSingleInstanceMutex()
    {
        if (_singleInstanceMutex is null)
        {
            return;
        }

        try
        {
            _singleInstanceMutex.ReleaseMutex();
        }
        catch (ApplicationException)
        {
        }

        _singleInstanceMutex.Dispose();
        _singleInstanceMutex = null;
    }
}
