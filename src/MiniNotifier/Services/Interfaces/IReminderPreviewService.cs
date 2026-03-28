using MiniNotifier.Models.DTOs;

namespace MiniNotifier.Services.Interfaces;

public interface IReminderPreviewService
{
    Task ShowAsync(HydrationSettingsDto settings, CancellationToken cancellationToken = default);
}
