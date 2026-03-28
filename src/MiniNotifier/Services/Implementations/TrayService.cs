using System.Drawing;
using System.Windows;
using System.Windows.Controls;
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

    private TaskbarIcon? _taskbarIcon;
    private MenuItem? _pauseMenuItem;
    private bool _isInitialized;
    private Icon? _trayIcon;

    public TrayService(
        IWindowManager windowManager,
        IReminderPreviewService reminderPreviewService,
        IHydrationSettingsService settingsService
    )
    {
        _windowManager = windowManager;
        _reminderPreviewService = reminderPreviewService;
        _settingsService = settingsService;

        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _pauseMenuItem = new MenuItem();
        _pauseMenuItem.Click += PauseMenuItemOnClick;

        var openMenuItem = new MenuItem { Header = "打开设置" };
        openMenuItem.Click += (_, _) => _windowManager.ShowSettingsWindow();

        var previewMenuItem = new MenuItem { Header = "立即提醒一次" };
        previewMenuItem.Click += async (_, _) =>
        {
            var settings = await _settingsService.GetCurrentAsync();
            await _reminderPreviewService.ShowAsync(settings);
        };

        var exitMenuItem = new MenuItem { Header = "退出应用" };
        exitMenuItem.Click += (_, _) => _windowManager.ShutdownApplication();

        var menu = new ContextMenu();
        menu.Items.Add(openMenuItem);
        menu.Items.Add(previewMenuItem);
        menu.Items.Add(_pauseMenuItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(exitMenuItem);

        _trayIcon = AppIconFactory.CreateTrayIcon();

        _taskbarIcon = new TaskbarIcon
        {
            ContextMenu = menu,
            Icon = _trayIcon,
            ToolTipText = "MiniNotifier"
        };

        _taskbarIcon.TrayLeftMouseUp += (_, _) => _windowManager.ShowSettingsWindow();

        _isInitialized = true;
        _ = RefreshTrayStateAsync();
    }

    public void Dispose()
    {
        _settingsService.SettingsChanged -= OnSettingsChanged;

        if (_pauseMenuItem is not null)
        {
            _pauseMenuItem.Click -= PauseMenuItemOnClick;
        }

        _taskbarIcon?.Dispose();
        _taskbarIcon = null;
        _trayIcon?.Dispose();
        _trayIcon = null;
    }

    private async void PauseMenuItemOnClick(object sender, RoutedEventArgs e)
    {
        await _settingsService.TogglePauseAsync();
    }

    private void OnSettingsChanged(object? sender, HydrationSettingsDto settings)
    {
        UpdateTrayDisplay(settings);
    }

    private async Task RefreshTrayStateAsync()
    {
        var settings = await _settingsService.GetCurrentAsync();
        UpdateTrayDisplay(settings);
    }

    private void UpdateTrayDisplay(HydrationSettingsDto settings)
    {
        if (_pauseMenuItem is null || _taskbarIcon is null)
        {
            return;
        }

        _pauseMenuItem.Header = settings.IsPaused ? "恢复提醒" : "暂停提醒";
        _taskbarIcon.ToolTipText = settings.IsReminderEnabled
            ? settings.IsPaused ? "MiniNotifier · 已暂停" : "MiniNotifier · 运行中"
            : "MiniNotifier · 已关闭";
    }
}
