using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using MiniNotifier.Helpers;
using MiniNotifier.Models;
using MiniNotifier.Services.Interfaces;

namespace MiniNotifier.Services.Implementations;

public sealed class MouseActivityService : IMouseActivityService, IDisposable
{
    private const int WhMouseLowLevel = 14;
    private const int WmLeftButtonDown = 0x0201;
    private const int WmRightButtonDown = 0x0204;
    private const int WmMiddleButtonDown = 0x0207;
    private const int WmXButtonDown = 0x020B;

    private readonly object _syncRoot = new();
    private readonly Queue<DateTimeOffset> _clicks = new();
    private readonly LowLevelMouseProc _mouseProc;

    private nint _hookHandle;
    private bool _initialized;

    public MouseActivityService()
    {
        _mouseProc = HookCallback;
    }

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _hookHandle = SetHook(_mouseProc);
        _initialized = true;
    }

    public MouseActivitySnapshot GetSnapshot()
    {
        lock (_syncRoot)
        {
            PruneExpiredClicks(DateTimeOffset.Now);

            var clicksLastMinute = CountClicksWithin(TimeSpan.FromMinutes(1));
            var clicksLastFiveMinutes = _clicks.Count;

            return new MouseActivitySnapshot(
                clicksLastMinute,
                clicksLastFiveMinutes,
                ResolveWorkState(clicksLastMinute, clicksLastFiveMinutes)
            );
        }
    }

    public void Dispose()
    {
        if (_hookHandle != 0)
        {
            UnhookWindowsHookEx(_hookHandle);
            _hookHandle = 0;
        }
    }

    private static nint SetHook(LowLevelMouseProc proc)
    {
        using var currentProcess = Process.GetCurrentProcess();
        using var currentModule = currentProcess.MainModule;
        var moduleName = currentModule?.ModuleName;
        var moduleHandle = string.IsNullOrWhiteSpace(moduleName) ? 0 : GetModuleHandle(moduleName);

        var hookHandle = SetWindowsHookEx(WhMouseLowLevel, proc, moduleHandle, 0);
        if (hookHandle == 0)
        {
            throw new InvalidOperationException("全局鼠标钩子初始化失败。");
        }

        return hookHandle;
    }

    private nint HookCallback(int nCode, nint wParam, nint lParam)
    {
        if (nCode >= 0 && IsClickMessage((int)wParam))
        {
            try
            {
                RecordClick(DateTimeOffset.Now);
            }
            catch (Exception ex)
            {
                AppDiagnostics.LogException("MouseActivityService.HookCallback", ex);
            }
        }

        return CallNextHookEx(_hookHandle, nCode, wParam, lParam);
    }

    private void RecordClick(DateTimeOffset time)
    {
        lock (_syncRoot)
        {
            _clicks.Enqueue(time);
            PruneExpiredClicks(time);
        }
    }

    private void PruneExpiredClicks(DateTimeOffset now)
    {
        var threshold = now.AddMinutes(-5);
        while (_clicks.Count > 0 && _clicks.Peek() < threshold)
        {
            _clicks.Dequeue();
        }
    }

    private int CountClicksWithin(TimeSpan window)
    {
        var threshold = DateTimeOffset.Now.Subtract(window);
        var count = 0;

        foreach (var click in _clicks)
        {
            if (click >= threshold)
            {
                count++;
            }
        }

        return count;
    }

    private static WorkIntensityState ResolveWorkState(int clicksLastMinute, int clicksLastFiveMinutes)
    {
        if (clicksLastMinute >= 30 || clicksLastFiveMinutes >= 110)
        {
            return WorkIntensityState.RapidFire;
        }

        if (clicksLastMinute >= 12 || clicksLastFiveMinutes >= 45)
        {
            return WorkIntensityState.ActiveHandling;
        }

        if (clicksLastMinute <= 2 && clicksLastFiveMinutes <= 10)
        {
            return WorkIntensityState.DeepFocus;
        }

        return WorkIntensityState.SteadyFlow;
    }

    private static bool IsClickMessage(int message)
    {
        return message is WmLeftButtonDown or WmRightButtonDown or WmMiddleButtonDown or WmXButtonDown;
    }

    private delegate nint LowLevelMouseProc(int nCode, nint wParam, nint lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern nint SetWindowsHookEx(
        int idHook,
        LowLevelMouseProc lpfn,
        nint hMod,
        uint dwThreadId
    );

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(nint hhk);

    [DllImport("user32.dll")]
    private static extern nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern nint GetModuleHandle(string? lpModuleName);
}
