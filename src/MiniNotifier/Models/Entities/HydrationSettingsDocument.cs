namespace MiniNotifier.Models.Entities;

public sealed record HydrationSettingsDocument
{
    public bool IsReminderEnabled { get; init; } = true;

    public bool IsPaused { get; init; }

    public int ReminderIntervalMinutes { get; init; } = 30;

    public int AutoCloseSeconds { get; init; } = 10;

    public DateTimeOffset? LastReminderAt { get; init; }

    public DateTimeOffset? NextReminderAt { get; init; }
}
