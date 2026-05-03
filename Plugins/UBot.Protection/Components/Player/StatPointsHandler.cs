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
    private static long _lastDistribution;
    private static bool _wasAppliedThisTick;

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
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnTick", OnTick, EventOwner);
    }

    private static void OnTick()
    {
        if (!IsAutoDistributionEnabled())
            return;

        var player = UBot.Core.RuntimeAccess.Session.Player;
        if (player == null)
            return;

        var available = (int)player.StatPoints;
        if (available <= 0)
        {
            _wasAppliedThisTick = false;
            return;
        }

        if (_wasAppliedThisTick)
            return;

        var now = UBot.Core.RuntimeAccess.Core.TickCount;
        if (now - _lastDistribution < 2000)
            return;

        _lastDistribution = now;
        _wasAppliedThisTick = true;
        OnApplyStatPoints();
    }

    private static bool IsAutoDistributionEnabled()
    {
        return UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.StatPoints.Enabled", false);
    }

    private static void OnApplyStatPoints()
    {
        if (!IsAutoDistributionEnabled())
            return;

        if (UBot.Core.RuntimeAccess.Session.Player == null)
            return;

        GetNormalizedDistribution(out var numStr, out var numInt);
        if (numStr + numInt <= 0)
            return;

        var available = (int)UBot.Core.RuntimeAccess.Session.Player.StatPoints;
        if (available <= 0)
            return;

        var stepCount = Math.Max(1, (int)Math.Ceiling(available / (double)(numStr + numInt)));
        Task.Run(() => IncreaseStatPoints(stepCount));
    }

    /// <summary>
    ///     Cores the on player level up.
    /// </summary>
    private static void OnPlayerLevelUp(byte oldLevel)
    {
        if (!IsAutoDistributionEnabled())
            return;

        var enabledIfBotIsStopped = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckIncBotStopped", true);
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running && !enabledIfBotIsStopped)
            return;

        var levelUps = UBot.Core.RuntimeAccess.Session.Player.Level - oldLevel;

        Task.Run(() => IncreaseStatPoints(levelUps));
    }

    public static void IncreaseStatPoints(int stepCount)
    {
        if (!IsAutoDistributionEnabled())
            return;

        GetNormalizedDistribution(out var numStr, out var numInt);

        for (var iLevelUp = 0; iLevelUp < stepCount; iLevelUp++)
        {
            if (CancellationRequested)
                return;
            if (numStr > 0)
                for (var i = 0; i < numStr; i++)
                {
                    if (CancellationRequested)
                        return;

                    Log.Notify($"Auto. increasing stat STR to {UBot.Core.RuntimeAccess.Session.Player.Strength + 1}");

                    IncreaseStr();

                    //Make sure the user has time to cancel, otherwise it's just too fast (but would still work due to the callback await)
                    Thread.Sleep(500);
                }

            if (numInt > 0)
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
