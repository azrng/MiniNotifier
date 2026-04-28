using System.Windows;
using System.Reflection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniNotifier.Helpers;
using MiniNotifier.Models;
using MiniNotifier.Models.DTOs;
using MiniNotifier.Services.Interfaces;
using MiniNotifier.ViewModels.Base;
using Wpf.Ui;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace MiniNotifier.ViewModels.Windows;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IHydrationSettingsService _settingsService;
    private readonly IReminderPreviewService _reminderPreviewService;
    private readonly IApplicationUpdateService _applicationUpdateService;
    private readonly ISnackbarService _snackbarService;
    private bool _isInitialized;
    private bool _isApplyingSettings;
    private DateTimeOffset? _lastReminderAt;
    private DateTimeOffset? _nextReminderAt;
    private AppUpdateCheckResultDto? _pendingUpdate;

    public MainWindowViewModel(
        IHydrationSettingsService settingsService,
        IReminderPreviewService reminderPreviewService,
        IApplicationUpdateService applicationUpdateService,
        ISnackbarService snackbarService
    )
    {
        _settingsService = settingsService;
        _reminderPreviewService = reminderPreviewService;
        _applicationUpdateService = applicationUpdateService;
        _snackbarService = snackbarService;

        _settingsService.SettingsChanged += OnSettingsChanged;
    }

    [ObservableProperty]
    private SettingsViewState _currentViewState = SettingsViewState.Loading;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeroStateText))]
    [NotifyPropertyChangedFor(nameof(HeroHintText))]
    private bool _isReminderEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HeroStateText))]
    [NotifyPropertyChangedFor(nameof(HeroHintText))]
    [NotifyPropertyChangedFor(nameof(PauseButtonText))]
    private bool _isPaused;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IntervalBadgeText))]
    private bool _isAutoStartEnabled;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IntervalBadgeText))]
    private double _reminderIntervalMinutes = 30;

    [ObservableProperty]
    private double _autoCloseSeconds = 5;

    [ObservableProperty]
    private bool _enableUpdateCheck = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CheckForUpdatesButtonText))]
    [NotifyPropertyChangedFor(nameof(UpdateActionVisibility))]
    private bool _isCheckingForUpdates;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(UpdateActionVisibility))]
    private bool _isUpdateAvailable;

    [ObservableProperty]
    private string _nextReminderText = "--";

    [ObservableProperty]
    private string _lastReminderText = "--";

    [ObservableProperty]
    private string _saveStateText = "等待加载";

    [ObservableProperty]
    private string _autoStartStatusText = "未开启";

    [ObservableProperty]
    private string _updateStatusText = "尚未检查更新。";

    [ObservableProperty]
    private string _lastUpdateCheckText = "尚未检查";

    [ObservableProperty]
    private string _latestAvailableVersion = "尚未检查";

    [ObservableProperty]
    private string _latestPublishedAtText = "--";

    public string HeroStateText =>
        !IsReminderEnabled ? "提醒已关闭" : IsPaused ? "提醒已暂停" : "提醒进行中";

    public string HeroHintText =>
        !IsReminderEnabled
            ? "你仍然可以从托盘打开设置并恢复提醒。"
            : IsPaused
                ? "托盘里可以一键恢复，当前不会触发新的弹窗。"
                : "当前会按你的设置节奏提醒，下次时间会显示在下方。";

    public string IntervalBadgeText => $"{ReminderIntervalMinutes:0} 分钟 / 次";

    public string PauseButtonText => IsPaused ? "恢复提醒" : "暂停提醒";

    public string ApplicationVersionText => $"当前版本 v{ResolveApplicationVersion()}";

    public string CurrentAppVersion => ResolveApplicationVersion();

    public string UpdateChannelName => _applicationUpdateService.ChannelName;

    public string CheckForUpdatesButtonText => IsCheckingForUpdates ? "检查中..." : "检查更新";

    public Visibility UpdateActionVisibility =>
        IsUpdateAvailable && !IsCheckingForUpdates ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ContentVisibility =>
        CurrentViewState == SettingsViewState.Content ? Visibility.Visible : Visibility.Collapsed;

    public Visibility LoadingVisibility =>
        CurrentViewState == SettingsViewState.Loading ? Visibility.Visible : Visibility.Collapsed;

    public Visibility EmptyVisibility =>
        CurrentViewState == SettingsViewState.Empty ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ErrorVisibility =>
        CurrentViewState == SettingsViewState.Error ? Visibility.Visible : Visibility.Collapsed;

    public Visibility NoPermissionVisibility =>
        CurrentViewState == SettingsViewState.NoPermission ? Visibility.Visible : Visibility.Collapsed;

    public async Task InitializeAsync()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        await LoadAsync();
        _ = CheckStartupUpdateAsync();
    }

    [RelayCommand]
    private async Task LoadAsync()
    {
        CurrentViewState = SettingsViewState.Loading;

        try
        {
            var settings = await _settingsService.GetCurrentAsync();
            Apply(settings);
            CurrentViewState = SettingsViewState.Content;
        }
        catch (UnauthorizedAccessException ex)
        {
            AppDiagnostics.LogException("MainWindowViewModel.LoadAsync", ex);
            CurrentViewState = SettingsViewState.NoPermission;
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("MainWindowViewModel.LoadAsync", ex);
            CurrentViewState = SettingsViewState.Error;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            var saved = await _settingsService.SaveAsync(CreateSnapshot());
            Apply(saved);
            CurrentViewState = SettingsViewState.Content;

            _snackbarService.Show(
                "设置已保存",
                "配置文件和开机自启动状态都已同步更新。",
                ControlAppearance.Success,
                TimeSpan.FromSeconds(3)
            );
        }
        catch (UnauthorizedAccessException ex)
        {
            AppDiagnostics.LogException("MainWindowViewModel.SaveAsync", ex);
            CurrentViewState = SettingsViewState.NoPermission;
            ShowError("保存失败", "没有足够权限写入配置或系统自启动项。");
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("MainWindowViewModel.SaveAsync", ex);
            CurrentViewState = SettingsViewState.Error;
            ShowError("保存失败", "本地配置或开机自启动更新时发生异常，请稍后重试。");
        }
    }

    [RelayCommand]
    private async Task CheckForUpdatesAsync()
    {
        if (IsCheckingForUpdates)
        {
            return;
        }

        await CheckForUpdatesCoreAsync(showLatestToast: true, silentFailure: false);
    }

    [RelayCommand]
    private async Task StartUpdateAsync()
    {
        if (_pendingUpdate is null || IsCheckingForUpdates)
        {
            return;
        }

        IsCheckingForUpdates = true;
        UpdateStatusText = $"发现新版本 {_pendingUpdate.LatestVersion}，正在启动更新程序...";

        try
        {
            var result = await _applicationUpdateService.StartUpdateAsync(_pendingUpdate);
            if (!result.IsSuccess)
            {
                var message = BuildUpdateFailureMessage(result);
                UpdateStatusText = message;
                ShowError("启动更新失败", message);
                return;
            }

            _snackbarService.Show(
                "更新程序已启动",
                "MiniNotifier 将退出，更新完成后会自动重新打开。",
                ControlAppearance.Success,
                TimeSpan.FromSeconds(4)
            );

            await Task.Delay(500);
            Application.Current.Shutdown();
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
    }

    [RelayCommand]
    private async Task TestReminderAsync()
    {
        try
        {
            await _reminderPreviewService.ShowAsync(CreateSnapshot(), preserveNextReminder: false);
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("MainWindowViewModel.TestReminderAsync", ex);
            ShowError("提醒预览失败", "弹窗创建时发生异常，详情已写入本地日志。");
        }
    }

    [RelayCommand]
    private async Task TogglePauseAsync()
    {
        try
        {
            var saved = await _settingsService.TogglePauseAsync();
            Apply(saved);

            _snackbarService.Show(
                IsPaused ? "提醒已暂停" : "提醒已恢复",
                "托盘和当前界面都已同步更新。",
                ControlAppearance.Info,
                TimeSpan.FromSeconds(2)
            );
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("MainWindowViewModel.TogglePauseAsync", ex);
            ShowError("状态切换失败", "提醒状态未能更新，请稍后重试。");
        }
    }

    [RelayCommand]
    private void SwitchState(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return;
        }

        CurrentViewState = Enum.TryParse<SettingsViewState>(state, true, out var parsedState)
            ? parsedState
            : SettingsViewState.Content;
    }

    private void OnSettingsChanged(object? sender, HydrationSettingsDto settings)
    {
        Application.Current.Dispatcher.Invoke(() => Apply(settings));
    }

    partial void OnCurrentViewStateChanged(SettingsViewState value)
    {
        OnPropertyChanged(nameof(ContentVisibility));
        OnPropertyChanged(nameof(LoadingVisibility));
        OnPropertyChanged(nameof(EmptyVisibility));
        OnPropertyChanged(nameof(ErrorVisibility));
        OnPropertyChanged(nameof(NoPermissionVisibility));
    }

    partial void OnIsReminderEnabledChanged(bool value)
    {
        RefreshNextReminderPreview();
    }

    partial void OnIsPausedChanged(bool value)
    {
        RefreshNextReminderPreview();
    }

    partial void OnReminderIntervalMinutesChanged(double value)
    {
        RefreshNextReminderPreview();
    }

    private void Apply(HydrationSettingsDto settings)
    {
        _isApplyingSettings = true;

        IsReminderEnabled = settings.IsReminderEnabled;
        IsPaused = settings.IsPaused;
        ReminderIntervalMinutes = settings.ReminderIntervalMinutes;
        AutoCloseSeconds = settings.AutoCloseSeconds;
        EnableUpdateCheck = settings.EnableUpdateCheck;
        _lastReminderAt = settings.LastReminderAt;
        _nextReminderAt = settings.NextReminderAt;
        IsAutoStartEnabled = settings.StartupSettings.IsEnabled;
        AutoStartStatusText = settings.StartupSettings.StatusText;
        NextReminderText = FormatDateTime(settings.NextReminderAt, "等待下一次计算");
        LastReminderText = FormatDateTime(settings.LastReminderAt, "今天还没提醒");
        SaveStateText = settings.SaveStateText;

        _isApplyingSettings = false;
    }

    private HydrationSettingsDto CreateSnapshot()
    {
        return new HydrationSettingsDto
        {
            IsReminderEnabled = IsReminderEnabled,
            IsPaused = IsPaused,
            ReminderIntervalMinutes = (int)Math.Round(ReminderIntervalMinutes, MidpointRounding.AwayFromZero),
            AutoCloseSeconds = (int)Math.Round(AutoCloseSeconds, MidpointRounding.AwayFromZero),
            EnableUpdateCheck = EnableUpdateCheck,
            LastReminderAt = _lastReminderAt,
            NextReminderAt = _nextReminderAt,
            SaveStateText = SaveStateText,
            StartupSettings = new StartupSettingsDto
            {
                IsEnabled = IsAutoStartEnabled,
                StatusText = IsAutoStartEnabled ? "已开启" : "未开启"
            }
        };
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

        var previewTime = DateTimeOffset.Now.AddMinutes(
            Math.Round(ReminderIntervalMinutes, MidpointRounding.AwayFromZero)
        );

        NextReminderText = previewTime.ToLocalTime().ToString("HH:mm");
    }

    private void ShowError(string title, string message)
    {
        _snackbarService.Show(
            title,
            message,
            ControlAppearance.Danger,
            TimeSpan.FromSeconds(4)
        );
    }

    private async Task CheckStartupUpdateAsync()
    {
        if (!EnableUpdateCheck || !_applicationUpdateService.IsConfigured)
        {
            return;
        }

        try
        {
            await Task.Delay(1200);
            await CheckForUpdatesCoreAsync(showLatestToast: false, silentFailure: true);
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("MainWindowViewModel.CheckStartupUpdateAsync", ex);
        }
    }

    private async Task CheckForUpdatesCoreAsync(bool showLatestToast, bool silentFailure)
    {
        if (!_applicationUpdateService.IsConfigured)
        {
            const string message = "尚未配置更新源，请检查 appsettings.json 中的 Update 节点。";
            UpdateStatusText = message;
            if (!silentFailure)
            {
                ShowError("检查更新失败", message);
            }

            return;
        }

        IsCheckingForUpdates = true;
        IsUpdateAvailable = false;
        _pendingUpdate = null;
        UpdateStatusText = $"正在通过 {UpdateChannelName} 检查更新...";

        try
        {
            var result = await _applicationUpdateService.CheckForUpdatesAsync(CurrentAppVersion);
            LastUpdateCheckText = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (!result.IsSuccess || result.Data is null)
            {
                var message = BuildUpdateFailureMessage(result);
                UpdateStatusText = message;
                if (!silentFailure)
                {
                    ShowError("检查更新失败", message);
                }

                return;
            }

            LatestAvailableVersion = result.Data.LatestVersion;
            LatestPublishedAtText = result.Data.PublishedAt?.ToLocalTime().ToString("yyyy-MM-dd HH:mm") ?? "--";

            if (!result.Data.HasUpdate)
            {
                UpdateStatusText = $"当前已是最新版本 {result.Data.CurrentVersion}。";
                if (showLatestToast)
                {
                    _snackbarService.Show(
                        "已是最新版本",
                        UpdateStatusText,
                        ControlAppearance.Success,
                        TimeSpan.FromSeconds(3)
                    );
                }

                return;
            }

            _pendingUpdate = result.Data;
            IsUpdateAvailable = true;
            UpdateStatusText = $"发现新版本 {result.Data.LatestVersion}，可立即更新。";
            _snackbarService.Show(
                "发现新版本",
                $"MiniNotifier {result.Data.LatestVersion} 已可用，可在设置窗口中立即更新。",
                ControlAppearance.Info,
                TimeSpan.FromSeconds(5)
            );
        }
        finally
        {
            IsCheckingForUpdates = false;
        }
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
