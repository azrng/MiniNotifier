using MiniNotifier.Models.Entities;

namespace MiniNotifier.Repositories.Interfaces;

public interface IHydrationSettingsRepository
{
    Task<HydrationSettingsDocument?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(HydrationSettingsDocument settings, CancellationToken cancellationToken = default);
}
