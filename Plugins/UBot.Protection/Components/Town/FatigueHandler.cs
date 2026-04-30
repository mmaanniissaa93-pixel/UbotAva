using System;
using System.Threading;
using UBot.Core;
using UBot.Core.Event;

namespace UBot.Protection.Components.Town;

public class FatigueHandler : AbstractTownHandler
{
    public static int ShardFatigueFullExpSeconds { get; set; }

    private static Timer _disconnectTimer;

    /// <summary>
    ///     Initializes this instance.
    /// </summary>
    public static void Initialize()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnFatigueTimeUpdate", OnFatigueTimeUpdate);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnAgentServerDisconnected", OnAgentServerDisconnected);
    }

    private static void OnFatigueTimeUpdate()
    {
        _disconnectTimer?.Dispose();
        _disconnectTimer = null;

        int shardFatigueSecondsToDC = UBot.Core.RuntimeAccess.Player.Get<int>("UBot.Protection.numShardFatigueMinToDC") * 60;

        int secondsToDC = ShardFatigueFullExpSeconds - shardFatigueSecondsToDC;

        if (secondsToDC <= 0)
        {
            Log.WarnLang("FatigueTimeExceeded");
            return;
        }

        if (UBot.Core.RuntimeAccess.Core.Bot.Running && UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.checkShardFatigue"))
        {
            TimeSpan remaining = TimeSpan.FromSeconds(secondsToDC);
            Log.Debug(
                $"You will be teleported and disconnected after {remaining.Hours}hr {remaining.Minutes}min {remaining.Seconds}s"
            );
        }

        _disconnectTimer = new Timer(
            _ =>
            {
                ReturnToTown();
            },
            null,
            secondsToDC * 1000,
            Timeout.Infinite
        );
    }

    private static void ReturnToTown()
    {
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running)
        {
            Log.WarnLang("FatigueBotIsNotRunning");
            return;
        }

        if (!UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.checkShardFatigue"))
            return;

        UBot.Core.RuntimeAccess.Core.Bot.Stop();
        UBot.Core.RuntimeAccess.Session.Player.UseReturnScroll();
        Log.WarnLang("ReturnToTownAndDC");

        Thread.Sleep(40000); // for slowest return scrolls and teleportation lag
        UBot.Core.RuntimeAccess.Core.Proxy?.Shutdown();
    }

    private static void OnAgentServerDisconnected()
    {
        _disconnectTimer?.Dispose();
        _disconnectTimer = null;
    }
}
