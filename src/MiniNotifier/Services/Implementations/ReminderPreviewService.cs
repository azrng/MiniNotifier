using Microsoft.Extensions.DependencyInjection;
using MiniNotifier.Models.DTOs;
using MiniNotifier.Services.Interfaces;
using MiniNotifier.Views.Windows;

namespace MiniNotifier.Services.Implementations;

public sealed class ReminderPreviewService(IServiceProvider serviceProvider) : IReminderPreviewService
{
    public Task ShowAsync(HydrationSettingsDto settings, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var window = serviceProvider.GetRequiredService<HydrationReminderWindow>();
        window.Prepare(settings);
        window.Show();

        return Task.CompletedTask;
    }
}
