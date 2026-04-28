using MiniNotifier.Models.DTOs;

namespace MiniNotifier.Services.Interfaces;

public interface IReminderPreviewService
{
    void WarmUp();

    Task ShowAsync(
        HydrationSettingsDto settings,
        bool preserveNextReminder = true,
        CancellationToken cancellationToken = default
    );
}
