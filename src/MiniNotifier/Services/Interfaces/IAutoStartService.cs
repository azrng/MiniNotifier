using MiniNotifier.Models.DTOs;

namespace MiniNotifier.Services.Interfaces;

public interface IAutoStartService
{
    Task<StartupSettingsDto> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<StartupSettingsDto> SetEnabledAsync(bool isEnabled, CancellationToken cancellationToken = default);
}
