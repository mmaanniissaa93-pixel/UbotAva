using UBot.Core;
using UBot.Core.Event;

namespace UBot.Protection.Components.Town;

public class PetInventoryFullHandler : AbstractTownHandler
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
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnInventoryUpdate", OnUpdateInventory, EventOwner);
    }

    /// <summary>
    /// </summary>
    private static void OnUpdateInventory()
    {
        CheckForPetInventoryFull();
    }

    internal static bool TryHandleStartPrecheck()
    {
        return CheckForPetInventoryFull();
    }

    private static bool CheckForPetInventoryFull()
    {
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running)
            return false;

        if (!UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckFullPetInventory"))
            return false;

        if (UBot.Core.RuntimeAccess.Session.Player.AbilityPet == null)
            return false;

        if (!UBot.Core.RuntimeAccess.Session.Player.AbilityPet.Inventory.Full)
            return false;

        if (PlayerInTownScriptRegion())
            return false;

        Log.NotifyLang("ReturnToTownPetInventoryFull");
        return UBot.Core.RuntimeAccess.Session.Player.UseReturnScroll();
    }
}
