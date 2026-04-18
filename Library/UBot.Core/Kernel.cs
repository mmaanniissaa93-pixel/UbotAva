using System;
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
    /// <summary>
    ///     The updater token source
    /// </summary>
    private static CancellationTokenSource _updaterTokenSource;

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
        Bot = new Bot();

        //Network handlers/hooks
        RegisterNetworkHandlers();
        RegisterNetworkHooks();

        _updaterTokenSource = new CancellationTokenSource();

        Task.Factory.StartNew(
            ComponentUpdaterAsync,
            _updaterTokenSource.Token,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Current
        );
    }

    private static async Task ComponentUpdaterAsync()
    {
        var lastTick = TickCount;
        var lastClockTick = TickCount;
        var playerMissingWhileReadyNotified = false;

        while (!_updaterTokenSource.IsCancellationRequested)
        {
            await Task.Delay(10);

            if (TickCount - lastClockTick >= 1000)
            {
                lastClockTick = TickCount;
                EventManager.FireEvent("OnClock");
            }

            if (!Game.Ready)
            {
                lastTick = TickCount;
                playerMissingWhileReadyNotified = false;
                continue;
            }

            try
            {
                var elapsed = TickCount - lastTick;
                var player = Game.Player;

                if (player == null)
                {
                    // Some servers may send state updates before character data is fully available.
                    // Avoid crashing the updater loop while waiting for a valid player object.
                    if (!playerMissingWhileReadyNotified)
                    {
                        Log.Debug("ComponentUpdater skipped: game is marked ready but player is not initialized yet.");
                        playerMissingWhileReadyNotified = true;
                    }

                    lastTick = TickCount;
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

                lastTick = TickCount;
            }
            catch (Exception e)
            {
                Log.Fatal(e);
            }
        }
    }

    /// <summary>
    ///     Registers the network handler.
    /// </summary>
    private static void RegisterNetworkHandlers()
    {
        var type = typeof(IPacketHandler);
        var types = AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => type.IsAssignableFrom(p) && !p.IsInterface)
            .ToArray();

        foreach (var handler in types)
        {
            var instance = (IPacketHandler)Activator.CreateInstance(handler);

            PacketManager.RegisterHandler(instance);
        }
    }

    /// <summary>
    ///     Registers the network hooks.
    /// </summary>
    private static void RegisterNetworkHooks()
    {
        var type = typeof(IPacketHook);
        var types = AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => type.IsAssignableFrom(p) && !p.IsInterface)
            .ToArray();

        foreach (var hook in types)
        {
            var instance = (IPacketHook)Activator.CreateInstance(hook);

            PacketManager.RegisterHook(instance);
        }
    }
}
