using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Network;
using UBot.Core.Plugins;

namespace UBot.Core;

public static class Kernel
{
    private static readonly object _lifecycleLock = new();
    private static bool _initialized;
    private static bool _networkHandlersRegistered;

    /// <summary>
    ///     The updater token source
    /// </summary>
    private static CancellationTokenSource _updaterTokenSource;

    private static Task _updaterTask;

    /// <summary>
    ///     Gets the proxy.
    /// </summary>
    /// <value>
    ///     The proxy.
    /// </value>
    public static Proxy Proxy { get; set; }

    /// <summary>
    ///     Gets or sets the bot.
    /// </summary>
    /// <value>
    ///     The bot.
    /// </value>
    public static Bot Bot { get; set; }

    /// <summary>
    ///     The application language
    /// </summary>
    public static string Language { get; set; }

    /// <summary>
    ///     Launch mode set by command line arguments (launch-client, launch-clientless)
    /// </summary>
    public static string LaunchMode { get; set; }

    /// <summary>
    ///     Get environment fixed tick count
    /// </summary>
    public static int TickCount => Environment.TickCount & int.MaxValue;

    /// <summary>
    ///     Get environment base directory
    /// </summary>
    public static string BasePath => AppDomain.CurrentDomain.BaseDirectory;

    /// <summary>
    /// Returns a value indicating if the NavMeshApi should be used or not.
    /// </summary>
    public static bool EnableCollisionDetection
    {
        get => GlobalConfig.Get("UBot.EnableCollisionDetection", false);
        set => GlobalConfig.Set("UBot.EnableCollisionDetection", value);
    }

    /// <summary>
    /// Returns a value indicating if this is a debug environment.
    /// </summary>
    public static bool Debug
    {
        get
        {
#if DEBUG
            return true;
#else
            return GlobalConfig.Get("UBot.DebugEnvironment", false);
#endif
        }
        set => GlobalConfig.Set("UBot.DebugEnvironment", value);
    }

    /// <summary>
    ///     Initializes this instance.
    /// </summary>
    public static void Initialize()
    {
        lock (_lifecycleLock)
        {
            if (_initialized)
                return;

            Bot ??= new Bot();

            // Network handlers/hooks are process-wide registrations.
            if (!_networkHandlersRegistered)
            {
                NetworkHandlerRegistry.RegisterAll();
                _networkHandlersRegistered = true;
            }

            _updaterTokenSource = new CancellationTokenSource();
            _updaterTask = Task.Run(() => ComponentUpdaterAsync(_updaterTokenSource.Token));
            _initialized = true;
        }
    }

    public static void Shutdown()
    {
        CancellationTokenSource tokenSource;
        Task updaterTask;

        lock (_lifecycleLock)
        {
            if (!_initialized)
                return;

            tokenSource = _updaterTokenSource;
            updaterTask = _updaterTask;
            _updaterTokenSource = null;
            _updaterTask = null;
            _initialized = false;
        }

        try
        {
            ClientlessManager.Shutdown();

            tokenSource?.Cancel();

            if (updaterTask != null && !updaterTask.Wait(TimeSpan.FromSeconds(2)))
                Log.Warn("Kernel updater did not stop within the shutdown timeout.");
        }
        catch (AggregateException ex) when (ex.InnerExceptions.All(e => e is OperationCanceledException))
        {
            // Expected during shutdown.
        }
        catch (OperationCanceledException)
        {
            // Expected during shutdown.
        }
        catch (Exception ex)
        {
            Log.Warn($"Kernel shutdown failed while stopping updater: {ex.Message}");
        }
        finally
        {
            tokenSource?.Dispose();
        }
    }

    private static async Task ComponentUpdaterAsync(CancellationToken cancellationToken)
    {
        var cachedTickCount = Environment.TickCount & int.MaxValue;
        var lastTick = cachedTickCount;
        var lastClockTick = cachedTickCount;
        var lastPerfLogTick = cachedTickCount;
        var playerMissingWhileReadyNotified = false;

        var tickStopwatch = new Stopwatch();
        var tickCount = 0;
        var totalTickDurationMs = 0L;
        var maxTickDurationMs = 0L;

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);

                cachedTickCount = Environment.TickCount & int.MaxValue;

                if (cachedTickCount - lastClockTick >= 1000)
                {
                    lastClockTick = cachedTickCount;
                    EventManager.FireEvent("OnClock");
                }

                if (!Game.Ready)
                {
                    lastTick = cachedTickCount;
                    playerMissingWhileReadyNotified = false;
                    continue;
                }

                try
                {
                    tickStopwatch.Restart();

                    var elapsed = cachedTickCount - lastTick;
                    var player = Game.Player;

                    if (player == null)
                    {
                        if (!playerMissingWhileReadyNotified)
                        {
                            Log.Debug("ComponentUpdater skipped: game is marked ready but player is not initialized yet.");
                            playerMissingWhileReadyNotified = true;
                        }

                        lastTick = cachedTickCount;
                        continue;
                    }
                    playerMissingWhileReadyNotified = false;

                    player.Update(elapsed);
                    player.Transport?.Update(elapsed);
                    player.JobTransport?.Update(elapsed);
                    player.AbilityPet?.Update(elapsed);
                    player.Growth?.Update(elapsed);
                    player.Fellow?.Update(elapsed);

                    SpawnManager.Update(elapsed);

                    EventManager.FireEvent("OnTick");

                    tickStopwatch.Stop();
                    var tickDuration = tickStopwatch.ElapsedMilliseconds;
                    tickCount++;
                    totalTickDurationMs += tickDuration;
                    if (tickDuration > maxTickDurationMs)
                        maxTickDurationMs = tickDuration;

                    lastTick = cachedTickCount;

                    if (cachedTickCount - lastPerfLogTick >= 30000)
                    {
                        var avgTickDuration = tickCount > 0 ? totalTickDurationMs / tickCount : 0;
                        var onTickListenerCount = EventManager.GetListenerCount("OnTick");
                        Log.Debug(
                            $"[PerfTick] Ticks=[{tickCount}], AvgDuration=[{avgTickDuration}ms], MaxDuration=[{maxTickDurationMs}ms], " +
                            $"OnTickListeners=[{onTickListenerCount}], ElapsedSinceLastLog=[{(cachedTickCount - lastPerfLogTick) / 1000}s]");

                        tickCount = 0;
                        totalTickDurationMs = 0;
                        maxTickDurationMs = 0;
                        lastPerfLogTick = cachedTickCount;
                    }
                }
                catch (Exception e)
                {
                    Log.Fatal(e);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Expected during shutdown.
        }
    }
}
