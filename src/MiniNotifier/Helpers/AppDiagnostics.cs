using System.IO;
using System.Text;

namespace MiniNotifier.Helpers;

public static class AppDiagnostics
{
    private static readonly string LogDirectory =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MiniNotifier",
            "logs"
        );

    private static readonly string LogPath = Path.Combine(LogDirectory, "app.log");

    public static void LogException(string source, Exception exception)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);

            var builder = new StringBuilder();
            builder.AppendLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] {source}");
            builder.AppendLine(exception.ToString());
            builder.AppendLine(new string('-', 80));

            File.AppendAllText(LogPath, builder.ToString(), Encoding.UTF8);
        }
        catch
        {
            // Swallow logging failures to avoid cascading app crashes.
        }
    }
}
