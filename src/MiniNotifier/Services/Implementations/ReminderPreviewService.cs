using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using MiniNotifier.Models.DTOs;
using MiniNotifier.Services.Interfaces;
using MiniNotifier.Views.Windows;

namespace MiniNotifier.Services.Implementations;

public sealed class ReminderPreviewService(
    IServiceProvider serviceProvider,
    IHydrationSettingsService settingsService
) : IReminderPreviewService
{
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
}
