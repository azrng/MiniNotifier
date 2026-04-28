using MiniNotifier.Models.DTOs;

namespace MiniNotifier.Services.Interfaces;

public interface IApplicationUpdateService
{
    string ChannelName { get; }

    bool IsConfigured { get; }

    Task<AppUpdateOperationResult<AppUpdateCheckResultDto>> CheckForUpdatesAsync(
        string currentVersion,
        CancellationToken cancellationToken = default
    );

    Task<AppUpdateOperationResult<bool>> StartUpdateAsync(
        AppUpdateCheckResultDto updateInfo,
        CancellationToken cancellationToken = default
    );
}
