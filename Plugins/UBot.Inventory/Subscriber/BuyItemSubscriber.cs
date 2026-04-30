using System;
using UBot.Core;
using UBot.Core.Event;

namespace UBot.Inventory.Subscriber;

internal class BuyItemSubscriber
{
    /// <summary>
    ///     Subscribes the events.
    /// </summary>
    public static void SubscribeEvents()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnBuyItem", new Action<byte, uint>(OnBuyItem));
    }

    private static void OnBuyItem(byte slot, uint entityId)
    {
        if (entityId != UBot.Core.RuntimeAccess.Session.Player.UniqueId)
            return;

        var itemAtSlot = UBot.Core.RuntimeAccess.Session.Player.Inventory.GetItemAt(slot);

        //Only stackable items
        if (itemAtSlot?.Record.MaxStack == 1 || itemAtSlot?.Record.MaxStack == 0)
            return;

        var itemsOfSameKind = UBot.Core.RuntimeAccess.Session.Player.Inventory.GetNormalPartItems(itemAtSlot.ItemId);
        if (itemsOfSameKind.Count == 1)
            return;

        foreach (var item in itemsOfSameKind)
        {
            if (item.Slot == slot)
                continue;

            if (item.Record.MaxStack - item.Amount >= itemAtSlot.Amount)
            {
                Log.Debug(
                    $"Merging item {itemAtSlot.Record.GetRealName()} ({itemAtSlot.Amount}) with {item.Record.GetRealName()} ({item.Amount})"
                );
                UBot.Core.RuntimeAccess.Session.Player.Inventory.MoveItem(slot, item.Slot, item.Amount);
                break;
            }
        }
    }
}
