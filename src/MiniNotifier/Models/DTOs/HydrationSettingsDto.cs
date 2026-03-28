namespace MiniNotifier.Models.DTOs;

public sealed record HydrationSettingsDto
{
    public bool IsReminderEnabled { get; init; }

    public bool IsPaused { get; init; }

    public int ReminderIntervalMinutes { get; init; }

    public int AutoCloseSeconds { get; init; }

    public DateTimeOffset? LastReminderAt { get; init; }

    public DateTimeOffset? NextReminderAt { get; init; }

    public string SaveStateText { get; init; } = string.Empty;

    public StartupSettingsDto StartupSettings { get; init; } = new();
}
