using System.Windows.Threading;
using MiniNotifier.Helpers;
using MiniNotifier.Services.Interfaces;

namespace MiniNotifier.Services.Implementations;

public sealed class ReminderSchedulerService(
    IHydrationSettingsService settingsService,
    IReminderPreviewService reminderPreviewService
) : IReminderSchedulerService, IDisposable
{
    private readonly DispatcherTimer _timer = new()
    {
        Interval = TimeSpan.FromSeconds(15)
    };

    private bool _isInitialized;
    private bool _isChecking;

    public void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }

        _timer.Tick += OnTimerTick;
        _timer.Start();
        _isInitialized = true;

        _ = CheckReminderAsync();
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Tick -= OnTimerTick;
    }

    private async void OnTimerTick(object? sender, EventArgs e)
    {
        await CheckReminderAsync();
    }

    private async Task CheckReminderAsync()
    {
        if (_isChecking)
        {
            return;
        }

        _isChecking = true;

        try
        {
            var settings = await settingsService.GetCurrentAsync();

            if (!settings.IsReminderEnabled || settings.IsPaused || settings.NextReminderAt is null)
            {
                return;
            }

            if (settings.NextReminderAt > DateTimeOffset.Now)
            {
                return;
            }

            await reminderPreviewService.ShowAsync(settings, preserveNextReminder: false);
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("ReminderSchedulerService.CheckReminderAsync", ex);
        }
        finally
        {
            _isChecking = false;
        }
    }
}
