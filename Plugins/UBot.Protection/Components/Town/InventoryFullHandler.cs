using UBot.Core;
using UBot.Core.Event;

namespace UBot.Protection.Components.Town;

public class InventoryFullHandler : AbstractTownHandler
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
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnInventoryUpdate", OnUpdateInventory);
    }

    /// <summary>
    /// </summary>
    private static void OnUpdateInventory()
    {
        CheckForInventoryFull();
    }

    internal static bool TryHandleStartPrecheck()
    {
        return CheckForInventoryFull();
    }

    private static bool CheckForInventoryFull()
    {
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running)
            return false;

        if (!UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.checkInventory"))
            return false;

        if (!UBot.Core.RuntimeAccess.Session.Player.Inventory.Full)
            return false;

        if (PlayerInTownScriptRegion())
            return false;

        Log.NotifyLang("ReturnToTownInventoryFull");

        return UBot.Core.RuntimeAccess.Session.Player.UseReturnScroll();
    }
}
