using UBot.Core;
using UBot.Core.Event;

namespace UBot.Inventory.Subscriber;

internal static class InventoryUpdateSubscriber
{
    private static object _lock;

    /// <summary>
    ///     Subscribes the events.
    /// </summary>
    public static void SubscribeEvents()
    {
        _lock = new object();
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnInventoryUpdate", OnInventoryUpdate);
    }

    private static void OnInventoryUpdate()
    {
        var autoSort = UBot.Core.RuntimeAccess.Player.Get("UBot.Inventory.AutoSort", false);
        if (!autoSort)
            return;

        lock (_lock)
        {
            UBot.Core.RuntimeAccess.Session.Player.Inventory.Sort();
        }
    }
}
