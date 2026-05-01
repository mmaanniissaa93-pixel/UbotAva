using System.Linq;
using System.Threading.Tasks;
using UBot.Core;
using UBot.Core.Event;
using UBot.Core.Network;
using UBot.Core.Objects;

namespace UBot.Protection.Components.Town;

public class DeadHandler : AbstractTownHandler
{
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
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnPlayerDied", OnPlayerDied, EventOwner);
    }

    internal static bool TryHandleStartPrecheck()
    {
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running)
            return false;

        if (UBot.Core.RuntimeAccess.Session.Player?.State?.LifeState == LifeState.Dead)
        {
            OnPlayerDied();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Cores the entity life state changed.
    /// </summary>
    /// <param name="uniqueId">The unique identifier.</param>
    private static async void OnPlayerDied()
    {
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running)
            return;

        if (UBot.Core.RuntimeAccess.Session.Player.Level < 10)
        {
            await Task.Delay(5000);
            var upPacket = new Packet(0x3053);
            upPacket.WriteByte(2);
            UBot.Core.RuntimeAccess.Packets.SendPacket(upPacket, PacketDestination.Server);
            return;
        }

        if (!UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckDead"))
            return;

        if (UBot.Core.RuntimeAccess.Session.Player.State.LifeState != LifeState.Dead)
            return;

        var itemsToUse = UBot.Core.RuntimeAccess.Player.GetArray<string>("UBot.Inventory.AutoUseAccordingToPurpose");
        var inventoryItem = UBot.Core.RuntimeAccess.Session.Player.Inventory.GetItem(
            new TypeIdFilter(3, 3, 13, 6),
            p => itemsToUse.Contains(p.Record.CodeName)
        );
        if (inventoryItem != null)
        {
            inventoryItem.Use();
            return;
        }

        var timeOut = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.TimeoutDead", 30);

        Log.WarnLang("ResurrectSPointSeconds", timeOut);

        await Task.Delay(timeOut * 1000);

        if (UBot.Core.RuntimeAccess.Session.Player.State.LifeState != LifeState.Dead)
            return;

        var packet = new Packet(0x3053);
        packet.WriteByte(1);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server); //Only works if not teleporting at that moment
    }
}
