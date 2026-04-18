using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniNotifier.Models.DTOs;
using MiniNotifier.Services.Interfaces;

namespace MiniNotifier.Avalonia.ViewModels;

public partial class ReminderWindowViewModel : ViewModelBase, IDisposable
{
    private readonly IReminderMessageService _reminderMessageService;
    private DispatcherTimer? _countdownTimer;
    private Action? _dismissAction;
    private int _remainingSeconds;

    public ReminderWindowViewModel(IReminderMessageService reminderMessageService)
    {
        _reminderMessageService = reminderMessageService;
    }

    [ObservableProperty]
    private string _title = "喝水提醒";

    [ObservableProperty]
    private string _headline = "主人，该喝水了";

    [ObservableProperty]
    private string _message = "起来活动一下，喝几口水，让状态重新上线。";

    [ObservableProperty]
    private string _countdownText = "15 秒后自动关闭";

    public void Prepare(HydrationSettingsDto settings, Action dismissAction)
    {
        _dismissAction = dismissAction;

        var reminderMessage = _reminderMessageService.Create(settings, DateTimeOffset.Now);
        Title = reminderMessage.Title;
        Headline = reminderMessage.Headline;
        Message = reminderMessage.Message;

        _remainingSeconds = Math.Clamp(settings.AutoCloseSeconds, 3, 15);
        UpdateCountdownText();

        if (_countdownTimer is not null)
        {
            _countdownTimer.Stop();
            _countdownTimer.Tick -= OnCountdownTick;
        }

        _countdownTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _countdownTimer.Tick += OnCountdownTick;
        _countdownTimer.Start();
    }

    [RelayCommand]
    private void Dismiss()
    {
        _dismissAction?.Invoke();
    }

    private void OnCountdownTick(object? sender, EventArgs e)
    {
        _remainingSeconds--;

        if (_remainingSeconds <= 0)
        {
            _countdownTimer?.Stop();
            _dismissAction?.Invoke();
            return;
        }

        UpdateCountdownText();
    }

    private void UpdateCountdownText()
    {
        CountdownText = $"{_remainingSeconds} 秒后自动关闭";
    }

    public void Dispose()
    {
        if (_countdownTimer is not null)
        {
            _countdownTimer.Stop();
            _countdownTimer.Tick -= OnCountdownTick;
            _countdownTimer = null;
        }
    }
}
