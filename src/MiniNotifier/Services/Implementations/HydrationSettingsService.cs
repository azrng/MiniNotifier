using System.Threading;
using MiniNotifier.Helpers;
using MiniNotifier.Models.DTOs;
using MiniNotifier.Models.Entities;
using MiniNotifier.Repositories.Interfaces;
using MiniNotifier.Services.Interfaces;

namespace MiniNotifier.Services.Implementations;

public sealed class HydrationSettingsService : IHydrationSettingsService, IDisposable
{
    private readonly IHydrationSettingsRepository _repository;
    private readonly IAutoStartService _autoStartService;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private HydrationSettingsDocument? _settingsDocument;
    private string _saveStateText = "等待加载";

    public HydrationSettingsService(
        IHydrationSettingsRepository repository,
        IAutoStartService autoStartService
    )
    {
        _repository = repository;
        _autoStartService = autoStartService;
    }

    public event EventHandler<HydrationSettingsDto>? SettingsChanged;

    public async Task<HydrationSettingsDto> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            var isFirstLoad = await EnsureLoadedAsync(cancellationToken);
            _saveStateText = isFirstLoad ? "已加载默认配置" : NormalizeStateText(_saveStateText, "配置已载入");

            return await BuildDtoAsync(_settingsDocument!, _saveStateText, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<HydrationSettingsDto> SaveAsync(
        HydrationSettingsDto settings,
        CancellationToken cancellationToken = default
    )
    {
        HydrationSettingsDto result;

        await _gate.WaitAsync(cancellationToken);

        try
        {
            await EnsureLoadedAsync(cancellationToken);

            var previousStartup = await _autoStartService.GetCurrentAsync(cancellationToken);
            var requestedDocument = Sanitize(new HydrationSettingsDocument
            {
                IsReminderEnabled = settings.IsReminderEnabled,
                IsPaused = settings.IsPaused,
                ReminderIntervalMinutes = settings.ReminderIntervalMinutes,
                AutoCloseSeconds = settings.AutoCloseSeconds,
                LastReminderAt = settings.LastReminderAt ?? _settingsDocument?.LastReminderAt,
                NextReminderAt = settings.NextReminderAt
            });

            var shouldRollbackStartup = previousStartup.IsEnabled != settings.StartupSettings.IsEnabled;

            if (shouldRollbackStartup)
            {
                await _autoStartService.SetEnabledAsync(
                    settings.StartupSettings.IsEnabled,
                    cancellationToken
                );
            }

            try
            {
                _settingsDocument = requestedDocument with
                {
                    NextReminderAt = BuildNextReminder(requestedDocument, DateTimeOffset.Now)
                };

                await _repository.SaveAsync(_settingsDocument, cancellationToken);
            }
            catch
            {
                if (shouldRollbackStartup)
                {
                    try
                    {
                        await _autoStartService.SetEnabledAsync(previousStartup.IsEnabled, cancellationToken);
                    }
                    catch (Exception rollbackException)
                    {
                        AppDiagnostics.LogException(
                            "HydrationSettingsService.RollbackAutoStart",
                            rollbackException
                        );
                    }
                }

                throw;
            }

            _saveStateText = "刚刚保存";
            result = await BuildDtoAsync(_settingsDocument, _saveStateText, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }

        SettingsChanged?.Invoke(this, result);
        return result;
    }

    public async Task<HydrationSettingsDto> TogglePauseAsync(CancellationToken cancellationToken = default)
    {
        HydrationSettingsDto result;

        await _gate.WaitAsync(cancellationToken);

        try
        {
            await EnsureLoadedAsync(cancellationToken);

            var toggled = _settingsDocument! with
            {
                IsPaused = !_settingsDocument.IsPaused
            };

            _settingsDocument = toggled with
            {
                NextReminderAt = BuildNextReminder(toggled, DateTimeOffset.Now)
            };

            await _repository.SaveAsync(_settingsDocument, cancellationToken);

            _saveStateText = _settingsDocument.IsPaused ? "已暂停提醒" : "已恢复提醒";
            result = await BuildDtoAsync(_settingsDocument, _saveStateText, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }

        SettingsChanged?.Invoke(this, result);
        return result;
    }

    public async Task<HydrationSettingsDto> RecordReminderShownAsync(
        bool preserveNextReminder = true,
        CancellationToken cancellationToken = default
    )
    {
        HydrationSettingsDto result;

        await _gate.WaitAsync(cancellationToken);

        try
        {
            await EnsureLoadedAsync(cancellationToken);

            var now = DateTimeOffset.Now;
            var nextReminderAt = preserveNextReminder
                ? _settingsDocument!.NextReminderAt
                : BuildNextReminder(_settingsDocument!, now);

            _settingsDocument = _settingsDocument! with
            {
                LastReminderAt = now,
                NextReminderAt = nextReminderAt
            };

            await _repository.SaveAsync(_settingsDocument, cancellationToken);

            _saveStateText = "刚刚提醒过";
            result = await BuildDtoAsync(_settingsDocument, _saveStateText, cancellationToken);
        }
        finally
        {
            _gate.Release();
        }

        SettingsChanged?.Invoke(this, result);
        return result;
    }

    private async Task<bool> EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_settingsDocument is not null)
        {
            return false;
        }

        var stored = await _repository.LoadAsync(cancellationToken);

        if (stored is null)
        {
            _settingsDocument = CreateDefaultDocument();
            await _repository.SaveAsync(_settingsDocument, cancellationToken);
            return true;
        }

        _settingsDocument = Normalize(stored, DateTimeOffset.Now);
        return false;
    }

    private async Task<HydrationSettingsDto> BuildDtoAsync(
        HydrationSettingsDocument settings,
        string saveStateText,
        CancellationToken cancellationToken
    )
    {
        var startup = await _autoStartService.GetCurrentAsync(cancellationToken);

        return new HydrationSettingsDto
        {
            IsReminderEnabled = settings.IsReminderEnabled,
            IsPaused = settings.IsPaused,
            ReminderIntervalMinutes = settings.ReminderIntervalMinutes,
            AutoCloseSeconds = settings.AutoCloseSeconds,
            LastReminderAt = settings.LastReminderAt,
            NextReminderAt = settings.NextReminderAt,
            SaveStateText = saveStateText,
            StartupSettings = startup
        };
    }

    private static HydrationSettingsDocument CreateDefaultDocument()
    {
        var now = DateTimeOffset.Now;
        var defaults = new HydrationSettingsDocument();
        return defaults with
        {
            NextReminderAt = now.AddMinutes(defaults.ReminderIntervalMinutes)
        };
    }

    private static HydrationSettingsDocument Normalize(
        HydrationSettingsDocument settings,
        DateTimeOffset now
    )
    {
        var sanitized = Sanitize(settings);
        return sanitized with
        {
            NextReminderAt = NormalizeNextReminder(sanitized, now)
        };
    }

    private static HydrationSettingsDocument Sanitize(HydrationSettingsDocument settings)
    {
        return settings with
        {
            ReminderIntervalMinutes = Math.Clamp(settings.ReminderIntervalMinutes, 1, 240),
            AutoCloseSeconds = Math.Clamp(settings.AutoCloseSeconds, 3, 15)
        };
    }

    private static DateTimeOffset? NormalizeNextReminder(
        HydrationSettingsDocument settings,
        DateTimeOffset now
    )
    {
        if (!settings.IsReminderEnabled || settings.IsPaused)
        {
            return null;
        }

        if (settings.NextReminderAt is null || settings.NextReminderAt <= now)
        {
            return now.AddMinutes(settings.ReminderIntervalMinutes);
        }

        return settings.NextReminderAt;
    }

    private static DateTimeOffset? BuildNextReminder(
        HydrationSettingsDocument settings,
        DateTimeOffset now
    )
    {
        if (!settings.IsReminderEnabled || settings.IsPaused)
        {
            return null;
        }

        return now.AddMinutes(settings.ReminderIntervalMinutes);
    }

    private static string NormalizeStateText(string current, string fallback)
    {
        return string.IsNullOrWhiteSpace(current) ? fallback : current;
    }

    public void Dispose()
    {
        _gate.Dispose();
    }
}
