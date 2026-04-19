namespace MiniNotifier.Models.DTOs;

public sealed record StartupSettingsDto
{
    public bool IsEnabled { get; init; }

    public string StatusText { get; init; } = string.Empty;
}
