using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using MiniNotifier.Helpers;
using MiniNotifier.Models.DTOs;
using MiniNotifier.Services.Interfaces;
using MiniNotifier.Views.Windows;

namespace MiniNotifier.Services.Implementations;

public sealed class ReminderPreviewService(
    IServiceProvider serviceProvider,
    IHydrationSettingsService settingsService
) : IReminderPreviewService
{
    private int _warmUpStarted;

    public void WarmUp()
    {
        if (Interlocked.Exchange(ref _warmUpStarted, 1) != 0)
        {
            return;
        }

        var dispatcher = Application.Current?.Dispatcher;
        dispatcher?.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(WarmUpReminderWindow));
    }

    public async Task ShowAsync(
        HydrationSettingsDto settings,
        bool preserveNextReminder = true,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            var window = serviceProvider.GetRequiredService<HydrationReminderWindow>();
            window.Prepare(settings);
            window.Show();
        }).Task;

        await settingsService.RecordReminderShownAsync(preserveNextReminder, cancellationToken);
    }

    private void WarmUpReminderWindow()
    {
        try
        {
            var window = serviceProvider.GetRequiredService<HydrationReminderWindow>();
            window.WarmUpLayout();
            window.Close();
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("ReminderPreviewService.WarmUp", ex);
        }
    }
}
