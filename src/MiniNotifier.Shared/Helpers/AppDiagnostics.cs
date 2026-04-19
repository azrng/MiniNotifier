using System.IO;
using System.Text;

namespace MiniNotifier.Helpers;

public static class AppDiagnostics
{
    public static void LogException(string source, Exception exception)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.LogDirectory);

            var builder = new StringBuilder();
            builder.AppendLine($"[{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss}] {source}");
            builder.AppendLine(exception.ToString());
            builder.AppendLine(new string('-', 80));

            File.AppendAllText(AppPaths.LogFilePath, builder.ToString(), Encoding.UTF8);
        }
        catch
        {
            // Swallow logging failures to avoid cascading app crashes.
        }
    }
}
