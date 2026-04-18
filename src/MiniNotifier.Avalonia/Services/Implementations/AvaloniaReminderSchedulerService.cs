using System.Threading;
using MiniNotifier.Helpers;
using MiniNotifier.Services.Interfaces;

namespace MiniNotifier.Avalonia.Services.Implementations;

public sealed class AvaloniaReminderSchedulerService(
    IHydrationSettingsService settingsService,
    IReminderPreviewService reminderPreviewService) : IReminderSchedulerService, IDisposable
{
    private readonly CancellationTokenSource _shutdownTokenSource = new();
    private Task? _backgroundTask;
    private int _isChecking;
    private bool _isInitialized;

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        _backgroundTask = Task.Run(() => RunLoopAsync(_shutdownTokenSource.Token));
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));

        await CheckReminderAsync(cancellationToken);

        while (await timer.WaitForNextTickAsync(cancellationToken))
        {
            await CheckReminderAsync(cancellationToken);
        }
    }

    private async Task CheckReminderAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _isChecking, 1) == 1)
        {
            return;
        }

        try
        {
            var settings = await settingsService.GetCurrentAsync(cancellationToken);

            if (!settings.IsReminderEnabled || settings.IsPaused || settings.NextReminderAt is null)
            {
                return;
            }

            if (settings.NextReminderAt > DateTimeOffset.Now)
            {
                return;
            }

            await reminderPreviewService.ShowAsync(settings, preserveNextReminder: false, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("AvaloniaReminderSchedulerService.CheckReminderAsync", ex);
        }
        finally
        {
            Interlocked.Exchange(ref _isChecking, 0);
        }
    }

    public void Dispose()
    {
        _shutdownTokenSource.Cancel();
        _shutdownTokenSource.Dispose();
    }
}
