using System.IO;
using System.Text.Json;
using MiniNotifier.Helpers;
using MiniNotifier.Models.Entities;
using MiniNotifier.Models.Serialization;
using MiniNotifier.Repositories.Interfaces;

namespace MiniNotifier.Repositories.Implementations;

public sealed class JsonHydrationSettingsRepository : IHydrationSettingsRepository
{
    public async Task<HydrationSettingsDocument?> LoadAsync(CancellationToken cancellationToken = default)
    {
        var filePath = AppPaths.SettingsFilePath;
        if (!File.Exists(filePath))
        {
            return null;
        }

        await using var stream = new FileStream(
            filePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read
        );

        var settings = await JsonSerializer.DeserializeAsync(
            stream,
            HydrationSettingsJsonContext.Default.HydrationSettingsDocument,
            cancellationToken
        );

        return settings ?? throw new InvalidDataException("提醒配置文件内容无效。");
    }

    public async Task SaveAsync(HydrationSettingsDocument settings, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(AppPaths.AppDataDirectory);

        var tempPath = $"{AppPaths.SettingsFilePath}.tmp";

        try
        {
            await using (var stream = new FileStream(
                tempPath,
                FileMode.Create,
                FileAccess.Write,
                FileShare.None
            ))
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    settings,
                    HydrationSettingsJsonContext.Default.HydrationSettingsDocument,
                    cancellationToken
                );

                await stream.FlushAsync(cancellationToken);
            }

            File.Move(tempPath, AppPaths.SettingsFilePath, true);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }
}
