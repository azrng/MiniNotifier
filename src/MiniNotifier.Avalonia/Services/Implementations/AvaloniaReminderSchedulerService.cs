using System.Threading;
using MiniNotifier.Helpers;
using MiniNotifier.Models.DTOs;
using MiniNotifier.Services.Interfaces;

namespace MiniNotifier.Avalonia.Services.Implementations;

public sealed class AvaloniaReminderSchedulerService : IReminderSchedulerService, IDisposable
{
    private static readonly TimeSpan MinimumDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaximumIdleDelay = TimeSpan.FromMinutes(5);

    private readonly IHydrationSettingsService _settingsService;
    private readonly IReminderPreviewService _reminderPreviewService;
    private readonly CancellationTokenSource _shutdownTokenSource = new();
    private readonly object _wakeSignalLock = new();

    private TaskCompletionSource _wakeSignal = CreateWakeSignal();
    private Task? _backgroundTask;
    private int _isChecking;
    private bool _isInitialized;
    private bool _isDisposed;

    public AvaloniaReminderSchedulerService(
        IHydrationSettingsService settingsService,
        IReminderPreviewService reminderPreviewService)
    {
        _settingsService = settingsService;
        _reminderPreviewService = reminderPreviewService;
    }

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _isInitialized = true;
        _settingsService.SettingsChanged += OnSettingsChanged;
        _backgroundTask = Task.Run(() => RunLoopAsync(_shutdownTokenSource.Token));
    }

    private async Task RunLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await CheckReminderAsync(cancellationToken);

            var delay = await CalculateNextDelayAsync(cancellationToken);
            await WaitForNextRunAsync(delay, cancellationToken);
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
            var settings = await _settingsService.GetCurrentAsync(cancellationToken);

            if (!settings.IsReminderEnabled || settings.IsPaused || settings.NextReminderAt is null)
            {
                return;
            }

            if (settings.NextReminderAt > DateTimeOffset.Now)
            {
                return;
            }

            await _reminderPreviewService.ShowAsync(settings, preserveNextReminder: false, cancellationToken);
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

    private async Task<TimeSpan> CalculateNextDelayAsync(CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _settingsService.GetCurrentAsync(cancellationToken);

            if (!settings.IsReminderEnabled || settings.IsPaused || settings.NextReminderAt is null)
            {
                return MaximumIdleDelay;
            }

            var delay = settings.NextReminderAt.Value - DateTimeOffset.Now;

            if (delay <= TimeSpan.Zero)
            {
                return MinimumDelay;
            }

            return delay < MaximumIdleDelay ? delay : MaximumIdleDelay;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return MinimumDelay;
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("AvaloniaReminderSchedulerService.CalculateNextDelayAsync", ex);
            return MaximumIdleDelay;
        }
    }

    private async Task WaitForNextRunAsync(TimeSpan delay, CancellationToken cancellationToken)
    {
        Task wakeTask;

        lock (_wakeSignalLock)
        {
            wakeTask = _wakeSignal.Task;
        }

        try
        {
            var delayTask = Task.Delay(delay, cancellationToken);
            var completedTask = await Task.WhenAny(delayTask, wakeTask);

            if (completedTask == delayTask)
            {
                await delayTask;
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    private void OnSettingsChanged(object? sender, HydrationSettingsDto settings)
    {
        WakeScheduler();
    }

    private void WakeScheduler()
    {
        TaskCompletionSource signalToComplete;

        lock (_wakeSignalLock)
        {
            signalToComplete = _wakeSignal;
            _wakeSignal = CreateWakeSignal();
        }

        signalToComplete.TrySetResult();
    }

    private static TaskCompletionSource CreateWakeSignal()
    {
        return new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _settingsService.SettingsChanged -= OnSettingsChanged;
        _shutdownTokenSource.Cancel();
        WakeScheduler();
        _shutdownTokenSource.Dispose();
    }
}
