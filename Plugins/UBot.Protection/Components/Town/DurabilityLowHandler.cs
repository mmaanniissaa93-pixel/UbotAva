using System;
using System.Linq;
using UBot.Core;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Protection.Components.Town;

public class DurabilityLowHandler : AbstractTownHandler
{
    /// <summary>
    ///     The last tick count
    /// </summary>
    private static long _lastTick = UBot.Core.RuntimeAccess.Core.TickCount;

    /// <summary>
    /// Indicates whether the system is currently busy processing an operation.
    /// </summary>
    /// <remarks>This field is intended for internal use to track the busy state of the system. It should not
    /// be accessed directly outside of the class.</remarks>
    private static bool _isBusy = false;

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
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnUpdateItemDurability", new Action<byte, uint>(OnUpdateItemDurability));
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnTick", OnTick);
    }

    internal static bool TryHandleStartPrecheck()
    {
        return CheckDurability(forceImmediate: true);
    }

    /// <summary>
    ///     Check the equiped items durabilities
    /// </summary>
    /// <param name="slot">The slot.</param>
    /// <param name="durability">The durability.</param>
    private static void OnTick()
    {
        CheckDurability(forceImmediate: false);
    }

    private static bool CheckDurability(bool forceImmediate)
    {
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running)
            return false;

        if (!UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckDurability"))
            return false;

        if (!forceImmediate && UBot.Core.RuntimeAccess.Core.TickCount - _lastTick < 10000)
            return false;

        if (PlayerInTownScriptRegion())
            return false;

        if (_isBusy)
            return false;

        _isBusy = true;
        try
        {
            _lastTick = UBot.Core.RuntimeAccess.Core.TickCount;

            for (byte slot = 0; slot < 8; slot++)
            {
                var item = UBot.Core.RuntimeAccess.Session.Player.Inventory.GetItemAt(slot);
                if (item == null || !item.Record.IsEquip || item.Durability > 6)
                    continue;

                var itemsToUse = UBot.Core.RuntimeAccess.Player.GetArray<string>("UBot.Inventory.AutoUseAccordingToPurpose");
                var inventoryItem = UBot.Core.RuntimeAccess.Session.Player.Inventory.GetItem(
                    new TypeIdFilter(3, 3, 13, 7),
                    p => itemsToUse.Contains(p.Record.CodeName)
                );
                if (inventoryItem != null)
                {
                    inventoryItem.Use();
                    return true;
                }

                if (UBot.Core.RuntimeAccess.Session.Player.UseReturnScroll())
                    Log.WarnLang("ReturnToTownDurLow", item.Record.GetRealName());

                return true;
            }
        }
        finally
        {
            _isBusy = false;
        }

        return false;
    }

    /// <summary>
    ///     Cores the on update item.
    /// </summary>
    /// <param name="slot">The slot.</param>
    /// <param name="durability">The durability.</param>
    private static void OnUpdateItemDurability(byte slot, uint durability)
    {
        var item = UBot.Core.RuntimeAccess.Session.Player.Inventory.GetItemAt(slot);
        if (item == null)
            return;

        item.Durability = durability;
    }
}
