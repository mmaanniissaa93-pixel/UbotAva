using System;
using UBot.Core;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Protection.Components.Pet;

public class CosReviveHandler
{
    /// <summary>
    ///     Initializes this instance.
    /// </summary>
    public static void Initialize()
    {
        SubscribeEvents();
    }

    /// <summary>
    ///     Subscribes the events.
    /// </summary>
    private static void SubscribeEvents()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnUpdateInventoryItem", new Action<byte>(OnItemUpdate));
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnStartBot", OnStartBot);
    }

    /// <summary>
    /// </summary>
    /// <param name="slot">The slot.</param>
    private static void OnItemUpdate(byte slot)
    {
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running)
            return;

        if (!UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.checkReviveAttackPet"))
            return;

        var item = UBot.Core.RuntimeAccess.Session.Player.Inventory.GetItemAt(slot);
        if (item == null)
            return;

        var itemRecord = item.Record;

        if (!itemRecord.IsPet)
            return;

        if (item.State != InventoryItemState.Dead)
            return;

        System.Threading.Thread.Sleep(5000);

        if (itemRecord.IsGrowthPet)
            UBot.Core.RuntimeAccess.Session.Player.ReviveGrowth();

        if (item.Record.IsFellowPet)
            UBot.Core.RuntimeAccess.Session.Player.ReviveFellow();
    }

    /// <summary>
    /// </summary>
    private static void OnStartBot()
    {
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running)
            return;

        if (!UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.checkReviveAttackPet"))
            return;

        if (UBot.Core.RuntimeAccess.Session.Player.ReviveFellow())
            return;

        UBot.Core.RuntimeAccess.Session.Player.ReviveGrowth();
    }
}
