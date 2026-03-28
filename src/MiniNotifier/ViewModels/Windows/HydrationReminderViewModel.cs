using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniNotifier.Models.DTOs;
using MiniNotifier.Services.Interfaces;
using MiniNotifier.ViewModels.Base;

namespace MiniNotifier.ViewModels.Windows;

public partial class HydrationReminderViewModel : ViewModelBase
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };
    private readonly IReminderMessageService _reminderMessageService;

    public HydrationReminderViewModel(IReminderMessageService reminderMessageService)
    {
        _reminderMessageService = reminderMessageService;
        _timer.Tick += OnTimerTick;
    }

    public event EventHandler? RequestClose;

    [ObservableProperty]
    private string _title = "喝水提醒";

    [ObservableProperty]
    private string _headline = "主人，该喝水了";

    [ObservableProperty]
    private string _message = "起来活动一下，喝几口水，让状态重新上线。";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CountdownText))]
    private int _secondsRemaining = 5;

    public string CountdownText => $"{SecondsRemaining} 秒后自动关闭";

    public void Initialize(HydrationSettingsDto settings)
    {
        _timer.Stop();

        var reminderMessage = _reminderMessageService.Create(settings, DateTimeOffset.Now);
        Title = reminderMessage.Title;
        Headline = reminderMessage.Headline;
        Message = reminderMessage.Message;
        SecondsRemaining = Math.Max(settings.AutoCloseSeconds, 1);

        _timer.Start();
    }

    [RelayCommand]
    private void Dismiss()
    {
        _timer.Stop();
        RequestClose?.Invoke(this, EventArgs.Empty);
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        if (SecondsRemaining <= 1)
        {
            _timer.Stop();
            RequestClose?.Invoke(this, EventArgs.Empty);
            return;
        }

        SecondsRemaining--;
    }
}
