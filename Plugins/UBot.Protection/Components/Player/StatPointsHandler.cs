using System;
using System.Threading;
using System.Threading.Tasks;
using UBot.Core;
using UBot.Core.Event;
using UBot.Core.Network;

namespace UBot.Protection.Components.Player;

public class StatPointsHandler
{
    public static bool CancellationRequested;
    private static readonly object EventOwner = new();

    /// <summary>
    ///     Initializes this instance.
    /// </summary>
    public static void Initialize()
    {
        SubscribeEvents();
    }

    /// <summary>
    ///     Subscribes all events (idempotent - clears existing first).
    /// </summary>
    public static void SubscribeAll()
    {
        UnsubscribeAll();
        SubscribeEvents();
    }

    /// <summary>
    ///     Unsubscribes all events.
    /// </summary>
    public static void UnsubscribeAll()
    {
        UBot.Core.RuntimeAccess.Events.UnsubscribeOwner(EventOwner);
    }

    /// <summary>
    ///     Subscribes the events.
    /// </summary>
    private static void SubscribeEvents()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnLevelUp", new Action<byte>(OnPlayerLevelUp), EventOwner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnApplyStatPoints", OnApplyStatPoints, EventOwner);
    }

    private static void OnApplyStatPoints()
    {
        if (UBot.Core.RuntimeAccess.Session.Player == null)
            return;

        var incStr = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckIncStr");
        var incInt = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckIncInt");
        GetNormalizedDistribution(out var numStr, out var numInt);

        var pointsPerStep = (incStr ? numStr : 0) + (incInt ? numInt : 0);
        if (pointsPerStep <= 0)
            return;

        var available = (int)UBot.Core.RuntimeAccess.Session.Player.StatPoints;
        if (available <= 0)
            return;

        var stepCount = Math.Max(1, (int)Math.Ceiling(available / (double)pointsPerStep));
        Task.Run(() => IncreaseStatPoints(stepCount));
    }

    /// <summary>
    ///     Cores the on player level up.
    /// </summary>
    private static void OnPlayerLevelUp(byte oldLevel)
    {
        var enabledIfBotIsStopped = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckIncBotStopped", true);
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running && !enabledIfBotIsStopped)
            return;

        var levelUps = UBot.Core.RuntimeAccess.Session.Player.Level - oldLevel;

        Task.Run(() => IncreaseStatPoints(levelUps));
    }

    public static void IncreaseStatPoints(int stepCount)
    {
        var incStr = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckIncStr");
        var incInt = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckIncInt");
        GetNormalizedDistribution(out var numStr, out var numInt);

        for (var iLevelUp = 0; iLevelUp < stepCount; iLevelUp++)
        {
            if (CancellationRequested)
                return;
            if (incStr && numStr > 0)
                for (var i = 0; i < numStr; i++)
                {
                    if (CancellationRequested)
                        return;

                    Log.Notify($"Auto. increasing stat STR to {UBot.Core.RuntimeAccess.Session.Player.Strength + 1}");

                    IncreaseStr();

                    //Make sure the user has time to cancel, otherwise it's just too fast (but would still work due to the callback await)
                    Thread.Sleep(500);
                }

            if (incInt && numInt > 0)
                for (var i = 0; i < numInt; i++)
                {
                    if (CancellationRequested)
                        return;

                    Log.Notify($"Auto. increasing stat INT to {UBot.Core.RuntimeAccess.Session.Player.Intelligence + 1}");

                    IncreaseInt();

                    Thread.Sleep(500);
                }
        }
    }

    /// <summary>
    ///     Sends the STR increase packet to the server
    /// </summary>
    private static void IncreaseStr()
    {
        if (UBot.Core.RuntimeAccess.Session.Player.StatPoints == 0)
        {
            Log.Debug("Could not invest stat point: The player does not have enough points to invest.");

            return;
        }

        var callback = new AwaitCallback(null, 0xB050);

        var packet = new Packet(0x7050);

        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server, callback);
        callback.AwaitResponse(1000);
    }

    /// <summary>
    ///     Sends the STR increase packet to the server
    /// </summary>
    private static void IncreaseInt()
    {
        if (UBot.Core.RuntimeAccess.Session.Player.StatPoints == 0)
        {
            Log.Debug("Could not invest stat point: The player does not have enough points to invest.");

            return;
        }

        var callback = new AwaitCallback(null, 0xB051);

        var packet = new Packet(0x7051);

        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server, callback);
        callback.AwaitResponse(1000);
    }

    private static void GetNormalizedDistribution(out int numStr, out int numInt)
    {
        numInt = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.IncrementInt", 0), 0, 3);
        numStr = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.IncrementStr", 0), 0, 3);

        if (numInt + numStr > 3)
            numStr = Math.Max(0, 3 - numInt);
    }
}
