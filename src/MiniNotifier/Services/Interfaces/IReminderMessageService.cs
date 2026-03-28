using MiniNotifier.Models;
using MiniNotifier.Models.DTOs;

namespace MiniNotifier.Services.Interfaces;

public interface IReminderMessageService
{
    ReminderMessage Create(HydrationSettingsDto settings, DateTimeOffset now);
}
