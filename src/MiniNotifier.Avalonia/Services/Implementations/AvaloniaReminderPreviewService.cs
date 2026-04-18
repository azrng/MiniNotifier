using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using MiniNotifier.Avalonia.Views;
using MiniNotifier.Models.DTOs;
using MiniNotifier.Services.Interfaces;

namespace MiniNotifier.Avalonia.Services.Implementations;

public sealed class AvaloniaReminderPreviewService(
    IServiceProvider serviceProvider,
    IHydrationSettingsService settingsService) : IReminderPreviewService
{
    public async Task ShowAsync(
        HydrationSettingsDto settings,
        bool preserveNextReminder = true,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            var window = serviceProvider.GetRequiredService<ReminderWindow>();
            window.Prepare(settings);
            window.Show();
        });

        await settingsService.RecordReminderShownAsync(preserveNextReminder, cancellationToken);
    }
}
