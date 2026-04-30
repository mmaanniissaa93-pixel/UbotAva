using System;
using System.Threading;
using System.Threading.Tasks;
using UBot.Core.Abstractions.Services;
using UBot.Core.Services;

namespace UBot.Core.Components;

public class ClientlessManager
{
    private static IClientlessService _service = new ClientlessService();

    public static void Initialize()
    {
        Initialize(ServiceRuntime.Clientless ?? new ClientlessService());
    }

    public static void Initialize(IClientlessService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        ServiceRuntime.Clientless = _service;
        _service.Initialize();
    }

    public static void GoClientless() => _service.GoClientless();
    public static void Shutdown() => _service.Shutdown();
    public static void RequestServerList() => _service.RequestServerList();
    public static void OnAgentServerDisconnected() => _service.OnAgentServerDisconnected();
    public static void OnAgentServerConnected() => _service.OnAgentServerConnected();
}

public sealed class ClientlessService : IClientlessService
{
    private readonly IClientConnectionRuntime _runtime;
    private readonly IServiceLog _log;
    private readonly object _keepAliveLock = new();
    private CancellationTokenSource _keepAliveCancellationTokenSource;
    private Task _keepAliveTask;

    private readonly object _reloginLock = new();
    private CancellationTokenSource _reloginCancellationTokenSource;
    private Task _reloginTask;

    public ClientlessService()
    {
    }

    public ClientlessService(IClientConnectionRuntime runtime, IServiceLog log)
    {
        _runtime = runtime;
        _log = log;
    }

    public void Initialize()
    {
        Log?.Debug("Initialized [ClientlessManager]!");
    }

    public void GoClientless()
    {
        Runtime?.ShutdownClientConnection();

        if (Runtime != null)
            Runtime.IsClientless = true;

        StartKeepAlivePacketWorker();
    }

    public void Shutdown()
    {
        StopKeepAlivePacketWorker(waitForStop: true);
        StopReloginWorker(waitForStop: true);
    }

    public void RequestServerList()
    {
        if (Runtime?.IsConnectedToGatewayServer != true)
            return;

        Runtime.SendServerListRequest();
    }

    public void OnAgentServerDisconnected()
    {
        StopKeepAlivePacketWorker();

        if (Runtime?.IsClientless != true)
            return;

        StartReloginWorker();
    }

    public void OnAgentServerConnected()
    {
        if (Runtime?.IsClientless != true)
            return;

        StopReloginWorker();
        StartKeepAlivePacketWorker();
    }

    private async Task ReloginAfterDisconnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            var delay = Runtime?.GetReloginDelayMilliseconds() ?? 10000;

            Log?.Warn($"Attempting relogin in {delay / 1000} seconds...");
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested || Runtime?.IsClientless != true)
                return;

            Runtime.StartGame();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
            Log?.Warn($"OnAgentServerDisconnected failed: {e.Message}");
        }
    }

    private async Task KeepAlivePacketWorkerAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && Runtime?.IsConnectedToAgentServer == true)
            {
                await Task.Delay(10000, cancellationToken).ConfigureAwait(false);

                Runtime?.SendKeepAlive();
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
            Log?.Warn($"KeepAlivePacketWorker failed: {e.Message}");
        }
    }

    private void StartKeepAlivePacketWorker()
    {
        lock (_keepAliveLock)
        {
            if (_keepAliveTask is { IsCompleted: false })
                return;

            _keepAliveCancellationTokenSource?.Dispose();
            _keepAliveCancellationTokenSource = new CancellationTokenSource();
            _keepAliveTask = Task.Run(() => KeepAlivePacketWorkerAsync(_keepAliveCancellationTokenSource.Token));
        }
    }

    private void StopKeepAlivePacketWorker(bool waitForStop = false)
    {
        CancellationTokenSource tokenSource;
        Task task;

        lock (_keepAliveLock)
        {
            tokenSource = _keepAliveCancellationTokenSource;
            task = _keepAliveTask;
            tokenSource?.Cancel();
        }

        WaitForWorker(task, waitForStop, "clientless keep-alive");

        lock (_keepAliveLock)
        {
            if (_keepAliveTask == task && (task == null || task.IsCompleted))
            {
                _keepAliveTask = null;
                _keepAliveCancellationTokenSource = null;
                tokenSource?.Dispose();
            }
        }
    }

    private void StartReloginWorker()
    {
        lock (_reloginLock)
        {
            if (_reloginTask is { IsCompleted: false })
                return;

            _reloginCancellationTokenSource?.Dispose();
            _reloginCancellationTokenSource = new CancellationTokenSource();
            _reloginTask = Task.Run(() => ReloginAfterDisconnectAsync(_reloginCancellationTokenSource.Token));
        }
    }

    private void StopReloginWorker(bool waitForStop = false)
    {
        CancellationTokenSource tokenSource;
        Task task;

        lock (_reloginLock)
        {
            tokenSource = _reloginCancellationTokenSource;
            task = _reloginTask;
            tokenSource?.Cancel();
        }

        WaitForWorker(task, waitForStop, "clientless relogin");

        lock (_reloginLock)
        {
            if (_reloginTask == task && (task == null || task.IsCompleted))
            {
                _reloginTask = null;
                _reloginCancellationTokenSource = null;
                tokenSource?.Dispose();
            }
        }
    }

    private static void WaitForWorker(Task task, bool waitForStop, string workerName)
    {
        if (!waitForStop || task == null || task.IsCompleted)
            return;

        try
        {
            if (!task.Wait(TimeSpan.FromSeconds(1)))
                ServiceRuntime.Log?.Warn($"{workerName} worker did not stop within the shutdown timeout.");
        }
        catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
        {
        }
        catch (OperationCanceledException)
        {
        }
    }

    private IClientConnectionRuntime Runtime => _runtime ?? ServiceRuntime.ClientConnectionRuntime;
    private IServiceLog Log => _log ?? ServiceRuntime.Log;
}
