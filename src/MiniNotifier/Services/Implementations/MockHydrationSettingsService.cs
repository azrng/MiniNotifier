using MiniNotifier.Models.DTOs;
using MiniNotifier.Services.Interfaces;

namespace MiniNotifier.Services.Implementations;

public sealed class MockHydrationSettingsService : IHydrationSettingsService
{
    private HydrationSettingsDto _settings = new()
    {
        IsReminderEnabled = true,
        IsPaused = false,
        ReminderIntervalMinutes = 30,
        AutoCloseSeconds = 5,
        LastReminderAt = DateTimeOffset.Now.AddMinutes(-6),
        NextReminderAt = DateTimeOffset.Now.AddMinutes(24),
        SaveStateText = "Mock 配置已载入",
        StartupSettings = new StartupSettingsDto
        {
            IsEnabled = false,
            StatusText = "未开启"
        }
    };

    public event EventHandler<HydrationSettingsDto>? SettingsChanged;

    public async Task<HydrationSettingsDto> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(320, cancellationToken);
        return _settings;
    }

    public async Task<HydrationSettingsDto> SaveAsync(
        HydrationSettingsDto settings,
        CancellationToken cancellationToken = default
    )
    {
        await Task.Delay(260, cancellationToken);

        var now = DateTimeOffset.Now;
        _settings = settings with
        {
            SaveStateText = "刚刚保存",
            NextReminderAt = BuildNextReminder(settings, now)
        };

        SettingsChanged?.Invoke(this, _settings);
        return _settings;
    }

    public async Task<HydrationSettingsDto> TogglePauseAsync(CancellationToken cancellationToken = default)
    {
        await Task.Delay(120, cancellationToken);

        var paused = !_settings.IsPaused;
        _settings = _settings with
        {
            IsPaused = paused,
            SaveStateText = paused ? "已暂停提醒" : "已恢复提醒",
            NextReminderAt = BuildNextReminder(_settings with { IsPaused = paused }, DateTimeOffset.Now)
        };

        SettingsChanged?.Invoke(this, _settings);
        return _settings;
    }

    private static DateTimeOffset? BuildNextReminder(HydrationSettingsDto settings, DateTimeOffset now)
    {
        if (!settings.IsReminderEnabled || settings.IsPaused)
        {
            return null;
        }

        return now.AddMinutes(settings.ReminderIntervalMinutes);
    }
}
