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
        EventManager.SubscribeEvent("OnInventoryUpdate", OnInventoryUpdate);
    }

    private static void OnInventoryUpdate()
    {
        var autoSort = PlayerConfig.Get("UBot.Inventory.AutoSort", false);
        if (!autoSort)
            return;

        lock (_lock)
        {
            Game.Player.Inventory.Sort();
        }
    }
}
