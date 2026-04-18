using UBot.Core;
using UBot.Core.Event;

namespace UBot.Protection.Components.Town;

public class PetInventoryFullHandler : AbstractTownHandler
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
        EventManager.SubscribeEvent("OnInventoryUpdate", OnUpdateInventory);
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
        if (!Kernel.Bot.Running)
            return false;

        if (!PlayerConfig.Get<bool>("UBot.Protection.checkFullPetInventory"))
            return false;

        if (Game.Player.AbilityPet == null)
            return false;

        if (!Game.Player.AbilityPet.Inventory.Full)
            return false;

        if (PlayerInTownScriptRegion())
            return false;

        Log.NotifyLang("ReturnToTownPetInventoryFull");
        return Game.Player.UseReturnScroll();
    }
}
