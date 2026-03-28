using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniNotifier.Models.DTOs;
using MiniNotifier.ViewModels.Base;

namespace MiniNotifier.ViewModels.Windows;

public partial class HydrationReminderViewModel : ViewModelBase
{
    private readonly DispatcherTimer _timer = new() { Interval = TimeSpan.FromSeconds(1) };

    public HydrationReminderViewModel()
    {
        _timer.Tick += OnTimerTick;
    }

    public event EventHandler? RequestClose;

    [ObservableProperty]
    private string _title = "Drink Water";

    [ObservableProperty]
    private string _message = "起来活动一下，喝几口水，让状态重新上线。";

    [ObservableProperty]
    private string _currentTimeText = DateTime.Now.ToString("HH:mm");

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CountdownText))]
    private int _secondsRemaining = 5;

    public string CountdownText => $"{SecondsRemaining} 秒后自动关闭";

    public void Initialize(HydrationSettingsDto settings)
    {
        _timer.Stop();

        Title = "Drink Water";
        Message = settings.IsPaused
            ? "提醒目前处于暂停模式，这是一条界面预览弹窗。"
            : "起来活动一下，喝几口水，让状态重新上线。";
        CurrentTimeText = DateTime.Now.ToString("HH:mm");
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
        CurrentTimeText = DateTime.Now.ToString("HH:mm");

        if (SecondsRemaining <= 1)
        {
            _timer.Stop();
            RequestClose?.Invoke(this, EventArgs.Empty);
            return;
        }

        SecondsRemaining--;
    }
}
