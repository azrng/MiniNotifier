using System.IO;

namespace MiniNotifier.Helpers;

public static class AppPaths
{
    public static string AppDataDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MiniNotifier"
        );

    public static string SettingsFilePath => Path.Combine(AppDataDirectory, "hydration-settings.json");

    public static string LogDirectory => Path.Combine(AppDataDirectory, "logs");

    public static string LogFilePath => Path.Combine(LogDirectory, "app.log");
}
