using System;
using System.Threading;
using System.Threading.Tasks;
using UBot.Core.Event;
using UBot.Core.Network;

namespace UBot.Core.Components;

public class ClientlessManager
{
    private static readonly object _keepAliveLock = new();
    private static CancellationTokenSource _keepAliveCancellationTokenSource;
    private static Task _keepAliveTask;

    private static readonly object _reloginLock = new();
    private static CancellationTokenSource _reloginCancellationTokenSource;
    private static Task _reloginTask;

    /// <summary>
    ///     Subscribes the events.
    /// </summary>
    internal static void Initialize()
    {
        EventManager.SubscribeEvent("OnAgentServerDisconnected", OnAgentServerDisconnected);
        EventManager.SubscribeEvent("OnAgentServerConnected", OnAgentServerConnected);
    }

    /// <summary>
    ///     Kills the client.
    /// </summary>
    public static void GoClientless()
    {
        Kernel.Proxy?.Client?.Shutdown();

        Game.Clientless = true;

        StartKeepAlivePacketWorker();
    }

    public static void Shutdown()
    {
        StopKeepAlivePacketWorker(waitForStop: true);
        StopReloginWorker(waitForStop: true);
    }

    /// <summary>
    ///     Requests the server list.
    /// </summary>
    public static void RequestServerList()
    {
        if (!Kernel.Proxy.IsConnectedToGatewayserver)
            return;

        PacketManager.SendPacket(new Packet(0x6101, true), PacketDestination.Server);
    }

    /// <summary>
    ///     Called when [agent server disconnected].
    /// </summary>
    private static void OnAgentServerDisconnected()
    {
        StopKeepAlivePacketWorker();

        if (!Game.Clientless)
            return;

        StartReloginWorker();
    }

    private static async Task ReloginAfterDisconnectAsync(CancellationToken cancellationToken)
    {
        try
        {
            int delay = 10000;
            if (GlobalConfig.Get("UBot.General.EnableWaitAfterDC", false))
                delay = GlobalConfig.Get<int>("UBot.General.WaitAfterDC") * 60 * 1000;

            Log.Warn($"Attempting relogin in {delay / 1000} seconds...");
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

            if (cancellationToken.IsCancellationRequested || !Game.Clientless)
                return;

            Game.Start();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
            Log.Error($"OnAgentServerDisconnected failed: {e.Message}");
        }
    }

    /// <summary>
    ///     Called when [agent server connected].
    /// </summary>
    private static void OnAgentServerConnected()
    {
        if (!Game.Clientless)
            return;

        StopReloginWorker();
        StartKeepAlivePacketWorker();
    }

    /// <summary>
    ///     Pings the server.
    /// </summary>
    private static async Task KeepAlivePacketWorkerAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested && Kernel.Proxy?.IsConnectedToAgentserver == true)
            {
                await Task.Delay(10000, cancellationToken).ConfigureAwait(false);

                PacketManager.SendPacket(new Packet(0x2002), PacketDestination.Server);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception e)
        {
            Log.Error($"KeepAlivePacketWorker failed: {e.Message}");
        }
    }

    private static void StartKeepAlivePacketWorker()
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

    private static void StopKeepAlivePacketWorker(bool waitForStop = false)
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

    private static void StartReloginWorker()
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

    private static void StopReloginWorker(bool waitForStop = false)
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
                Log.Warn($"{workerName} worker did not stop within the shutdown timeout.");
        }
        catch (AggregateException ex) when (ex.InnerException is OperationCanceledException)
        {
        }
        catch (OperationCanceledException)
        {
        }
    }
}
