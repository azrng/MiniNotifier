using System.Reflection;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniNotifier.Helpers;
using MiniNotifier.Models;
using MiniNotifier.Models.DTOs;
using MiniNotifier.Services.Interfaces;

namespace MiniNotifier.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IHydrationSettingsService _settingsService;
    private readonly IReminderPreviewService _reminderPreviewService;
    private readonly IWindowManager _windowManager;
    private bool _isInitialized;
    private bool _isApplyingSettings;
    private DateTimeOffset? _lastReminderAt;
    private DateTimeOffset? _nextReminderAt;

    public MainWindowViewModel(
        IHydrationSettingsService settingsService,
        IReminderPreviewService reminderPreviewService,
        IWindowManager windowManager)
    {
        _settingsService = settingsService;
        _reminderPreviewService = reminderPreviewService;
        _windowManager = windowManager;

        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    [ObservableProperty]
    private SettingsViewState _currentViewState = SettingsViewState.Loading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeroStateText))]
    [NotifyPropertyChangedFor(nameof(HeroHintText))]
    [NotifyPropertyChangedFor(nameof(TrayToolTipText))]
    private bool _isReminderEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeroStateText))]
    [NotifyPropertyChangedFor(nameof(HeroHintText))]
    [NotifyPropertyChangedFor(nameof(PauseButtonText))]
    [NotifyPropertyChangedFor(nameof(PauseMenuText))]
    [NotifyPropertyChangedFor(nameof(TrayToolTipText))]
    private bool _isPaused;

    [ObservableProperty]
    private bool _isAutoStartEnabled;

    [ObservableProperty]
    private string _reminderIntervalText = "30";

    [ObservableProperty]
    private string _autoCloseSecondsText = "15";

    [ObservableProperty]
    private string _nextReminderText = "--";

    [ObservableProperty]
    private string _lastReminderText = "--";

    [ObservableProperty]
    private string _saveStateText = "等待加载";

    [ObservableProperty]
    private string _autoStartStatusText = "未开启";

    [ObservableProperty]
    private string _statusMessageText = "应用会继续驻留在托盘中运行。";

    [ObservableProperty]
    private bool _hasStatusError;

    public string HeroStateText =>
        !IsReminderEnabled ? "提醒已关闭" : IsPaused ? "提醒已暂停" : "提醒进行中";

    public string HeroHintText =>
        !IsReminderEnabled
            ? "你仍然可以从托盘重新打开设置并恢复提醒。"
            : IsPaused
                ? "当前不会弹出新的提醒窗口，但托盘入口仍然可用。"
                : "应用会在后台按照当前节奏提醒你喝水，并实时更新下次提醒时间。";

    public string IntervalBadgeText =>
        int.TryParse(ReminderIntervalText, out var minutes) ? $"{minutes} 分钟 / 次" : "待校验";

    public string PauseButtonText => IsPaused ? "恢复提醒" : "暂停提醒";

    public string PauseMenuText => IsPaused ? "恢复提醒" : "暂停提醒";

    public string TrayToolTipText =>
        !IsReminderEnabled ? "MiniNotifier · 提醒已关闭" : IsPaused ? "MiniNotifier · 提醒已暂停" : "MiniNotifier · 提醒进行中";

    public string ApplicationVersionText => $"当前版本 v{ResolveApplicationVersion()}";

    public bool IsContentVisible => CurrentViewState == SettingsViewState.Content;

    public bool IsLoadingVisible => CurrentViewState == SettingsViewState.Loading;

    public bool IsEmptyVisible => CurrentViewState == SettingsViewState.Empty;

    public bool IsErrorVisible => CurrentViewState == SettingsViewState.Error;

    public bool IsNoPermissionVisible => CurrentViewState == SettingsViewState.NoPermission;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        await LoadAsync();
    }

    [RelayCommand]
    private void ShowWindow()
    {
        _windowManager.ShowSettingsWindow();
    }

    [RelayCommand]
    private void ExitApplication()
    {
        _windowManager.ShutdownApplication();
    }

    [RelayCommand]
    private void GoToContent()
    {
        CurrentViewState = SettingsViewState.Content;
        HasStatusError = false;
        StatusMessageText = "你可以继续调整设置，保存后会立即生效。";
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        CurrentViewState = SettingsViewState.Loading;
        HasStatusError = false;
        StatusMessageText = "正在同步提醒配置...";

        try
        {
            var settings = await _settingsService.GetCurrentAsync();
            Apply(settings);

            CurrentViewState = settings.SaveStateText.Contains("默认", StringComparison.Ordinal)
                ? SettingsViewState.Empty
                : SettingsViewState.Content;

            StatusMessageText = CurrentViewState == SettingsViewState.Empty
                ? "首次启动已自动准备默认配置，你可以直接继续使用。"
                : "配置已同步，当前状态与托盘保持一致。";
        }
        catch (UnauthorizedAccessException ex)
        {
            AppDiagnostics.LogException("MainWindowViewModel.LoadAsync", ex);
            CurrentViewState = SettingsViewState.NoPermission;
            HasStatusError = true;
            StatusMessageText = "当前没有足够权限写入配置或更新开机自启动。";
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("MainWindowViewModel.LoadAsync", ex);
            CurrentViewState = SettingsViewState.Error;
            HasStatusError = true;
            StatusMessageText = "配置加载失败，请重试或检查本地配置文件。";
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!TryBuildSnapshot(out var snapshot, out var validationMessage))
        {
            HasStatusError = true;
            StatusMessageText = validationMessage;
            return;
        }

        try
        {
            var saved = await _settingsService.SaveAsync(snapshot);
            Apply(saved);
            CurrentViewState = SettingsViewState.Content;
            HasStatusError = false;
            StatusMessageText = "设置已保存并立即生效，托盘状态也已同步更新。";
        }
        catch (UnauthorizedAccessException ex)
        {
            AppDiagnostics.LogException("MainWindowViewModel.SaveAsync", ex);
            CurrentViewState = SettingsViewState.NoPermission;
            HasStatusError = true;
            StatusMessageText = "保存失败：没有足够权限写入配置或更新开机自启动。";
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("MainWindowViewModel.SaveAsync", ex);
            CurrentViewState = SettingsViewState.Error;
            HasStatusError = true;
            StatusMessageText = "保存失败：当前设置尚未生效，请稍后重试。";
        }
    }

    [RelayCommand]
    private async Task TestReminderAsync()
    {
        if (!TryBuildSnapshot(out var snapshot, out var validationMessage))
        {
            HasStatusError = true;
            StatusMessageText = validationMessage;
            return;
        }

        try
        {
            await _reminderPreviewService.ShowAsync(snapshot);
            HasStatusError = false;
            StatusMessageText = "提醒弹窗已显示，正式调度时间保持不变。";
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("MainWindowViewModel.TestReminderAsync", ex);
            HasStatusError = true;
            StatusMessageText = "提醒预览失败，详情已写入本地日志。";
        }
    }

    [RelayCommand]
    private async Task TogglePauseAsync()
    {
        try
        {
            var saved = await _settingsService.TogglePauseAsync();
            Apply(saved);
            CurrentViewState = SettingsViewState.Content;
            HasStatusError = false;
            StatusMessageText = IsPaused ? "提醒已暂停，自动弹窗已停止。" : "提醒已恢复，调度器会重新计算下次提醒时间。";
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("MainWindowViewModel.TogglePauseAsync", ex);
            HasStatusError = true;
            StatusMessageText = "提醒状态切换失败，请稍后再试。";
        }
    }

    private void OnSettingsChanged(object? sender, HydrationSettingsDto settings)
    {
        Dispatcher.UIThread.Post(() =>
        {
            Apply(settings);
            CurrentViewState = SettingsViewState.Content;
        });
    }

    partial void OnCurrentViewStateChanged(SettingsViewState value)
    {
        OnPropertyChanged(nameof(IsContentVisible));
        OnPropertyChanged(nameof(IsLoadingVisible));
        OnPropertyChanged(nameof(IsEmptyVisible));
        OnPropertyChanged(nameof(IsErrorVisible));
        OnPropertyChanged(nameof(IsNoPermissionVisible));
    }

    partial void OnReminderIntervalTextChanged(string value)
    {
        OnPropertyChanged(nameof(IntervalBadgeText));
        RefreshNextReminderPreview();
    }

    partial void OnAutoCloseSecondsTextChanged(string value)
    {
        RefreshNextReminderPreview();
    }

    partial void OnIsReminderEnabledChanged(bool value)
    {
        RefreshNextReminderPreview();
    }

    partial void OnIsPausedChanged(bool value)
    {
        RefreshNextReminderPreview();
    }

    private void Apply(HydrationSettingsDto settings)
    {
        _isApplyingSettings = true;

        IsReminderEnabled = settings.IsReminderEnabled;
        IsPaused = settings.IsPaused;
        IsAutoStartEnabled = settings.StartupSettings.IsEnabled;
        ReminderIntervalText = settings.ReminderIntervalMinutes.ToString();
        AutoCloseSecondsText = settings.AutoCloseSeconds.ToString();
        _lastReminderAt = settings.LastReminderAt;
        _nextReminderAt = settings.NextReminderAt;
        NextReminderText = FormatDateTime(settings.NextReminderAt, "等待下一次计算");
        LastReminderText = FormatDateTime(settings.LastReminderAt, "今天还没有提醒");
        SaveStateText = settings.SaveStateText;
        AutoStartStatusText = settings.StartupSettings.StatusText;

        _isApplyingSettings = false;

        OnPropertyChanged(nameof(HeroStateText));
        OnPropertyChanged(nameof(HeroHintText));
        OnPropertyChanged(nameof(IntervalBadgeText));
        OnPropertyChanged(nameof(PauseButtonText));
        OnPropertyChanged(nameof(PauseMenuText));
        OnPropertyChanged(nameof(TrayToolTipText));
    }

    private bool TryBuildSnapshot(out HydrationSettingsDto snapshot, out string validationMessage)
    {
        snapshot = default!;
        validationMessage = string.Empty;

        if (!int.TryParse(ReminderIntervalText, out var interval))
        {
            validationMessage = "提醒间隔必须是 1 到 240 之间的整数。";
            return false;
        }

        if (!int.TryParse(AutoCloseSecondsText, out var autoCloseSeconds))
        {
            validationMessage = "自动关闭秒数必须是 3 到 15 之间的整数。";
            return false;
        }

        if (interval < 1 || interval > 240)
        {
            validationMessage = "提醒间隔超出允许范围，请输入 1 到 240。";
            return false;
        }

        if (autoCloseSeconds < 3 || autoCloseSeconds > 15)
        {
            validationMessage = "自动关闭秒数超出允许范围，请输入 3 到 15。";
            return false;
        }

        snapshot = new HydrationSettingsDto
        {
            IsReminderEnabled = IsReminderEnabled,
            IsPaused = IsPaused,
            ReminderIntervalMinutes = interval,
            AutoCloseSeconds = autoCloseSeconds,
            LastReminderAt = _lastReminderAt,
            NextReminderAt = _nextReminderAt,
            SaveStateText = SaveStateText,
            StartupSettings = new StartupSettingsDto
            {
                IsEnabled = IsAutoStartEnabled,
                StatusText = IsAutoStartEnabled ? "已开启，开机自动运行" : "未开启"
            }
        };

        return true;
    }

    private static string FormatDateTime(DateTimeOffset? dateTime, string fallback)
    {
        return dateTime?.ToLocalTime().ToString("HH:mm") ?? fallback;
    }

    private void RefreshNextReminderPreview()
    {
        if (_isApplyingSettings || !_isInitialized)
        {
            return;
        }

        if (!IsReminderEnabled || IsPaused)
        {
            NextReminderText = "等待下一次计算";
            return;
        }

        if (!int.TryParse(ReminderIntervalText, out var interval))
        {
            NextReminderText = "待校验";
            return;
        }

        NextReminderText = DateTimeOffset.Now.AddMinutes(interval).ToLocalTime().ToString("HH:mm");
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
