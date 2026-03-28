using Microsoft.Win32;
using MiniNotifier.Models.DTOs;
using MiniNotifier.Services.Interfaces;

namespace MiniNotifier.Services.Implementations;

public sealed class RegistryAutoStartService : IAutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "MiniNotifier";

    public Task<StartupSettingsDto> GetCurrentAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
        var value = runKey?.GetValue(AppName)?.ToString();
        return Task.FromResult(CreateStatus(!string.IsNullOrWhiteSpace(value)));
    }

    public Task<StartupSettingsDto> SetEnabledAsync(
        bool isEnabled,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();

        using var runKey = Registry.CurrentUser.OpenSubKey(RunKeyPath, true)
            ?? Registry.CurrentUser.CreateSubKey(RunKeyPath, true);

        if (isEnabled)
        {
            runKey.SetValue(AppName, BuildStartupCommand(), RegistryValueKind.String);
        }
        else
        {
            runKey.DeleteValue(AppName, false);
        }

        return Task.FromResult(CreateStatus(isEnabled));
    }

    private static StartupSettingsDto CreateStatus(bool isEnabled)
    {
        return new StartupSettingsDto
        {
            IsEnabled = isEnabled,
            StatusText = isEnabled ? "已开启，开机自动运行" : "未开启"
        };
    }

    private static string BuildStartupCommand()
    {
        var processPath = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(processPath))
        {
            throw new InvalidOperationException("无法确定当前程序路径，无法设置开机自启动。");
        }

        return $"\"{processPath}\"";
    }
}
