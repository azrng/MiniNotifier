using MiniNotifier.Models.DTOs;

namespace MiniNotifier.Services.Interfaces;

public interface IHydrationSettingsService
{
    event EventHandler<HydrationSettingsDto>? SettingsChanged;

    Task<HydrationSettingsDto> GetCurrentAsync(CancellationToken cancellationToken = default);

    Task<HydrationSettingsDto> SaveAsync(HydrationSettingsDto settings, CancellationToken cancellationToken = default);

    Task<HydrationSettingsDto> TogglePauseAsync(CancellationToken cancellationToken = default);
}
