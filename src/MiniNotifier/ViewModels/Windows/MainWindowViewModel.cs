using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
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

        var settings = await _settingsService.GetCurrentAsync();
        Apply(settings);
        CurrentViewState = SettingsViewState.Content;
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var saved = await _settingsService.SaveAsync(CreateSnapshot());
        Apply(saved);

        _snackbarService.Show(
            "设置已保存",
            "Mock 数据已经更新，阶段 2 会接入真实持久化与开机自启动。",
            ControlAppearance.Success,
            TimeSpan.FromSeconds(3)
        );
    }

    [RelayCommand]
    private async Task TestReminderAsync()
    {
        await _reminderPreviewService.ShowAsync(CreateSnapshot());
    }

    [RelayCommand]
    private async Task TogglePauseAsync()
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
            LastReminderAt = DateTimeOffset.Now.AddMinutes(-6),
            NextReminderAt = IsReminderEnabled && !IsPaused ? DateTimeOffset.Now.AddMinutes(ReminderIntervalMinutes) : null,
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
}
