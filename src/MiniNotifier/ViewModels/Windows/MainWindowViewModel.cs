using System.Windows;
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
    private readonly ISnackbarService _snackbarService;
    private bool _isInitialized;
    private DateTimeOffset? _lastReminderAt;
    private DateTimeOffset? _nextReminderAt;

    public MainWindowViewModel(
        IHydrationSettingsService settingsService,
        IReminderPreviewService reminderPreviewService,
        ISnackbarService snackbarService
    )
    {
        _settingsService = settingsService;
        _reminderPreviewService = reminderPreviewService;
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
    private string _nextReminderText = "--";

    [ObservableProperty]
    private string _lastReminderText = "--";

    [ObservableProperty]
    private string _saveStateText = "等待加载";

    [ObservableProperty]
    private string _autoStartStatusText = "未开启";

    public string HeroStateText =>
        !IsReminderEnabled ? "提醒已关闭" : IsPaused ? "提醒已暂停" : "提醒进行中";

    public string HeroHintText =>
        !IsReminderEnabled
            ? "你仍然可以从托盘打开设置并恢复提醒。"
            : IsPaused
                ? "托盘里可以一键恢复，当前不会触发新的弹窗。"
                : "当前节奏平稳运行中，下一次会按设置时间触发。";

    public string IntervalBadgeText => $"{ReminderIntervalMinutes:0} 分钟 / 次";

    public string PauseButtonText => IsPaused ? "恢复提醒" : "暂停提醒";

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
                "配置文件和开机自启动状态都已经同步更新。",
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
    private async Task TestReminderAsync()
    {
        try
        {
            await _reminderPreviewService.ShowAsync(CreateSnapshot());
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
                "托盘状态和界面状态已经同步刷新。",
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

    private void Apply(HydrationSettingsDto settings)
    {
        IsReminderEnabled = settings.IsReminderEnabled;
        IsPaused = settings.IsPaused;
        ReminderIntervalMinutes = settings.ReminderIntervalMinutes;
        AutoCloseSeconds = settings.AutoCloseSeconds;
        _lastReminderAt = settings.LastReminderAt;
        _nextReminderAt = settings.NextReminderAt;
        IsAutoStartEnabled = settings.StartupSettings.IsEnabled;
        AutoStartStatusText = settings.StartupSettings.StatusText;
        NextReminderText = FormatDateTime(settings.NextReminderAt, "等待下一次计算");
        LastReminderText = FormatDateTime(settings.LastReminderAt, "今天还没提醒");
        SaveStateText = settings.SaveStateText;
    }

    private HydrationSettingsDto CreateSnapshot()
    {
        return new HydrationSettingsDto
        {
            IsReminderEnabled = IsReminderEnabled,
            IsPaused = IsPaused,
            ReminderIntervalMinutes = (int)Math.Round(ReminderIntervalMinutes, MidpointRounding.AwayFromZero),
            AutoCloseSeconds = (int)Math.Round(AutoCloseSeconds, MidpointRounding.AwayFromZero),
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

    private void ShowError(string title, string message)
    {
        _snackbarService.Show(
            title,
            message,
            ControlAppearance.Danger,
            TimeSpan.FromSeconds(4)
        );
    }
}
