using System.Threading;
using MiniNotifier.Helpers;

namespace MiniNotifier.Avalonia;

internal sealed class SingleInstanceCoordinator : IDisposable
{
    private const string MutexName = "Local\\MiniNotifier.Avalonia.SingleInstance";
    private const string ActivationEventName = "Local\\MiniNotifier.Avalonia.Activate";

    private readonly CancellationTokenSource _shutdownTokenSource = new();
    private readonly EventWaitHandle _activationEvent;
    private readonly Mutex _mutex;
    private readonly bool _ownsMutex;
    private bool _isDisposed;

    private SingleInstanceCoordinator(Mutex mutex, bool ownsMutex, EventWaitHandle activationEvent)
    {
        _mutex = mutex;
        _ownsMutex = ownsMutex;
        _activationEvent = activationEvent;
    }

    public static event EventHandler? ActivationRequested;

    public static SingleInstanceCoordinator CreatePrimary()
    {
        var mutex = new Mutex(false, MutexName);
        var ownsMutex = false;

        try
        {
            ownsMutex = mutex.WaitOne(0);

            if (!ownsMutex)
            {
                SignalPrimaryInstance();
                return new SingleInstanceCoordinator(mutex, ownsMutex, CreateActivationEvent());
            }

            var coordinator = new SingleInstanceCoordinator(mutex, ownsMutex, CreateActivationEvent());
            coordinator.StartActivationListener();
            return coordinator;
        }
        catch
        {
            if (ownsMutex)
            {
                mutex.ReleaseMutex();
            }

            mutex.Dispose();
            throw;
        }
    }

    public bool IsPrimary => _ownsMutex;

    private void StartActivationListener()
    {
        Task.Run(() => ListenForActivationAsync(_shutdownTokenSource.Token));
    }

    private async Task ListenForActivationAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Run(() => _activationEvent.WaitOne(), cancellationToken);

                if (!cancellationToken.IsCancellationRequested)
                {
                    ActivationRequested?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
            }
            catch (Exception ex)
            {
                AppDiagnostics.LogException("SingleInstanceCoordinator.ListenForActivationAsync", ex);
            }
        }
    }

    private static EventWaitHandle CreateActivationEvent()
    {
        return new EventWaitHandle(false, EventResetMode.AutoReset, ActivationEventName);
    }

    private static void SignalPrimaryInstance()
    {
        try
        {
            using var activationEvent = EventWaitHandle.OpenExisting(ActivationEventName);
            activationEvent.Set();
        }
        catch (WaitHandleCannotBeOpenedException)
        {
        }
        catch (Exception ex)
        {
            AppDiagnostics.LogException("SingleInstanceCoordinator.SignalPrimaryInstance", ex);
        }
    }

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;
        _shutdownTokenSource.Cancel();
        _activationEvent.Set();
        _activationEvent.Dispose();
        _shutdownTokenSource.Dispose();

        if (_ownsMutex)
        {
            _mutex.ReleaseMutex();
        }

        _mutex.Dispose();
    }
}
