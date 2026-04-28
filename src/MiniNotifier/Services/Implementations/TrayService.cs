using System.Drawing;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Hardcodet.Wpf.TaskbarNotification;
using MiniNotifier.Helpers;
using MiniNotifier.Models.DTOs;
using MiniNotifier.Services.Interfaces;

namespace MiniNotifier.Services.Implementations;

public sealed class TrayService : ITrayService, IDisposable
{
    private readonly IWindowManager _windowManager;
    private readonly IReminderPreviewService _reminderPreviewService;
    private readonly IHydrationSettingsService _settingsService;
    private readonly IApplicationUpdateService _applicationUpdateService;

    private TaskbarIcon? _taskbarIcon;
    private ContextMenu? _contextMenu;
    private MenuItem? _pauseMenuItem;
    private Icon? _trayIcon;
    private bool _isInitialized;
    private bool _isContextMenuWarmed;

    public TrayService(
        IWindowManager windowManager,
        IReminderPreviewService reminderPreviewService,
        IHydrationSettingsService settingsService,
        IApplicationUpdateService applicationUpdateService
    )
    {
        _windowManager = windowManager;
        _reminderPreviewService = reminderPreviewService;
        _settingsService = settingsService;
        _applicationUpdateService = applicationUpdateService;

        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _pauseMenuItem = new MenuItem { Header = "暂停提醒" };
        _pauseMenuItem.Click += PauseMenuItemOnClick;

        var openMenuItem = new MenuItem { Header = "打开设置" };
        openMenuItem.Click += OpenMenuItemOnClick;

        var previewMenuItem = new MenuItem { Header = "立即提醒一次" };
        previewMenuItem.Click += PreviewMenuItemOnClick;

        var checkUpdateMenuItem = new MenuItem { Header = "检查更新" };
        checkUpdateMenuItem.Click += CheckUpdateMenuItemOnClick;

        var exitMenuItem = new MenuItem { Header = "退出应用" };
        exitMenuItem.Click += ExitMenuItemOnClick;

        _contextMenu = new ContextMenu();
        _contextMenu.Items.Add(openMenuItem);
        _contextMenu.Items.Add(previewMenuItem);
        _contextMenu.Items.Add(_pauseMenuItem);
        _contextMenu.Items.Add(checkUpdateMenuItem);
        _contextMenu.Items.Add(new Separator());
        _contextMenu.Items.Add(exitMenuItem);

        _trayIcon = AppIconProvider.LoadTrayIcon();

        _taskbarIcon = new TaskbarIcon
        {
            ContextMenu = _contextMenu,
            Icon = _trayIcon,
            ToolTipText = "MiniNotifier"
        };

        _taskbarIcon.TrayLeftMouseUp += TrayLeftMouseUpOnClick;

        _isInitialized = true;
        _ = RefreshTrayStateAsync();
        WarmUpContextMenu();
    }

    public void Dispose()
    {
        _settingsService.SettingsChanged -= OnSettingsChanged;

        if (_pauseMenuItem is not null)
        {
            _pauseMenuItem.Click -= PauseMenuItemOnClick;
        }

        if (_taskbarIcon is not null)
        {
            _taskbarIcon.TrayLeftMouseUp -= TrayLeftMouseUpOnClick;
            _taskbarIcon.Dispose();
            _taskbarIcon = null;
        }

        _contextMenu = null;
        _trayIcon?.Dispose();
        _trayIcon = null;
    }

    private async void TrayLeftMouseUpOnClick(object sender, RoutedEventArgs e)
    {
        await HandleTrayActionAsync(() =>
        {
            _windowManager.ShowSettingsWindow();
            return Task.CompletedTask;
        });
    }

    private async void OpenMenuItemOnClick(object sender, RoutedEventArgs e)
    {
        await HandleTrayActionAsync(() =>
        {
            _windowManager.ShowSettingsWindow();
            return Task.CompletedTask;
        });
    }

    private async void PreviewMenuItemOnClick(object sender, RoutedEventArgs e)
    {
        await HandleTrayActionAsync(async () =>
        {
            var settings = await _settingsService.GetCurrentAsync();
            await _reminderPreviewService.ShowAsync(settings, preserveNextReminder: false);
        });
    }

    private async void PauseMenuItemOnClick(object sender, RoutedEventArgs e)
    {
        await HandleTrayActionAsync(async () => await _settingsService.TogglePauseAsync());
    }

    private async void CheckUpdateMenuItemOnClick(object sender, RoutedEventArgs e)
    {
        await HandleTrayActionAsync(CheckForUpdatesFromTrayAsync);
    }

    private async void ExitMenuItemOnClick(object sender, RoutedEventArgs e)
    {
        await HandleTrayActionAsync(() =>
        {
            _windowManager.ShutdownApplication();
            return Task.CompletedTask;
        });
    }

    private async Task CheckForUpdatesFromTrayAsync()
    {
        var result = await _applicationUpdateService.CheckForUpdatesAsync(ResolveApplicationVersion());
        if (!result.IsSuccess || result.Data is null)
        {
            ShowTrayMessage("检查更新失败", BuildUpdateFailureMessage(result), MessageBoxImage.Error);
            return;
        }

        if (!result.Data.HasUpdate)
        {
            ShowTrayMessage(
                "MiniNotifier",
                $"当前已是最新版本 {result.Data.CurrentVersion}。",
                MessageBoxImage.Information
            );
            return;
        }

        var answer = MessageBox.Show(
            $"发现新版本 {result.Data.LatestVersion}，是否立即更新？",
            "MiniNotifier",
            MessageBoxButton.YesNo,
            MessageBoxImage.Information
        );
        if (answer != MessageBoxResult.Yes)
        {
            _windowManager.ShowSettingsWindow();
            return;
        }

        var startResult = await _applicationUpdateService.StartUpdateAsync(result.Data);
        if (!startResult.IsSuccess)
        {
            ShowTrayMessage("启动更新失败", BuildUpdateFailureMessage(startResult), MessageBoxImage.Error);
            return;
        }

        ShowTrayMessage(
            "更新程序已启动",
            "MiniNotifier 将退出，更新完成后会自动重新打开。",
            MessageBoxImage.Information
        );
        _windowManager.ShutdownApplication();
    }

    private void OnSettingsChanged(object? sender, HydrationSettingsDto settings)
    {
        UpdateTrayDisplay(settings);
    }

    private async Task RefreshTrayStateAsync()
    {
        try
        {
            var settings = await _settingsService.GetCurrentAsync();
            UpdateTrayDisplay(settings);
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("TrayService.RefreshTrayStateAsync", ex);
        }
    }

    private void UpdateTrayDisplay(HydrationSettingsDto settings)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            if (_pauseMenuItem is null || _taskbarIcon is null)
            {
                return;
            }

            _pauseMenuItem.Header = settings.IsPaused ? "恢复提醒" : "暂停提醒";
            _taskbarIcon.ToolTipText = settings.IsReminderEnabled
                ? settings.IsPaused ? "MiniNotifier · 已暂停" : "MiniNotifier · 运行中"
                : "MiniNotifier · 已关闭";
        });
    }

    private void WarmUpContextMenu()
    {
        if (_contextMenu is null || _isContextMenuWarmed)
        {
            return;
        }

        Application.Current.Dispatcher.BeginInvoke(
            () =>
            {
                if (_contextMenu is null || _isContextMenuWarmed)
                {
                    return;
                }

                try
                {
                    PrepareFrameworkElement(_contextMenu, new System.Windows.Size(220, 236));

                    foreach (var item in _contextMenu.Items)
                    {
                        switch (item)
                        {
                            case MenuItem menuItem:
                                PrepareFrameworkElement(menuItem, new System.Windows.Size(220, 36));
                                break;
                            case Separator separator:
                                PrepareFrameworkElement(separator, new System.Windows.Size(220, 8));
                                break;
                        }
                    }

                    var originalPlacement = _contextMenu.Placement;
                    var originalHorizontalOffset = _contextMenu.HorizontalOffset;
                    var originalVerticalOffset = _contextMenu.VerticalOffset;
                    var originalOpacity = _contextMenu.Opacity;
                    var originalStaysOpen = _contextMenu.StaysOpen;

                    _contextMenu.Placement = PlacementMode.AbsolutePoint;
                    _contextMenu.HorizontalOffset = -10000;
                    _contextMenu.VerticalOffset = -10000;
                    _contextMenu.Opacity = 0;
                    _contextMenu.StaysOpen = true;
                    _contextMenu.IsOpen = true;
                    _contextMenu.UpdateLayout();
                    _contextMenu.IsOpen = false;

                    _contextMenu.Placement = originalPlacement;
                    _contextMenu.HorizontalOffset = originalHorizontalOffset;
                    _contextMenu.VerticalOffset = originalVerticalOffset;
                    _contextMenu.Opacity = originalOpacity;
                    _contextMenu.StaysOpen = originalStaysOpen;

                    _isContextMenuWarmed = true;
                }
                catch (Exception ex)
                {
                    AppDiagnostics.LogException("TrayService.WarmUpContextMenu", ex);
                }
            },
            DispatcherPriority.ApplicationIdle
        );
    }

    private static void PrepareFrameworkElement(FrameworkElement element, System.Windows.Size desiredSize)
    {
        element.ApplyTemplate();
        element.Measure(desiredSize);
        element.Arrange(new Rect(0, 0, desiredSize.Width, desiredSize.Height));
        element.UpdateLayout();
    }

    private static async Task HandleTrayActionAsync(Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("TrayService.HandleTrayActionAsync", ex);

            Application.Current.Dispatcher.Invoke(() =>
            {
                MessageBox.Show(
                    "托盘交互发生异常，错误已记录到本地日志。",
                    "MiniNotifier",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            });
        }
    }

    private static void ShowTrayMessage(string title, string message, MessageBoxImage image)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, image);
        });
    }

    private static string BuildUpdateFailureMessage<T>(AppUpdateOperationResult<T> result)
    {
        return string.IsNullOrWhiteSpace(result.ErrorCode)
            ? result.Message
            : $"{result.Message}（{result.ErrorCode}）";
    }

    private static string ResolveApplicationVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var informationalVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            return informationalVersion.Split('+')[0];
        }

        return assembly.GetName().Version?.ToString() ?? "1.0.0";
    }
}
