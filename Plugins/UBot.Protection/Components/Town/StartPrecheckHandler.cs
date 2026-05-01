using System.Threading;
using UBot.Core;
using UBot.Core.Event;

namespace UBot.Protection.Components.Town;

internal static class StartPrecheckHandler
{
    private static readonly object EventOwner = new();

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

    private static void SubscribeEvents()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnStartBot", OnStartBot, EventOwner);
    }

    private static void OnStartBot()
    {
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running)
            return;

        if (!TryHandleTownReturnPrecheck())
            return;

        // Give the return packet a small head start before combat loop ticks.
        Thread.Sleep(350);
    }

    private static bool TryHandleTownReturnPrecheck()
    {
        if (DeadHandler.TryHandleStartPrecheck())
            return true;

        if (AmmunitionHandler.TryHandleStartPrecheck())
            return true;

        if (NoHealthPotionsHandler.TryHandleStartPrecheck())
            return true;

        if (NoManaPotionsHandler.TryHandleStartPrecheck())
            return true;

        if (InventoryFullHandler.TryHandleStartPrecheck())
            return true;

        if (PetInventoryFullHandler.TryHandleStartPrecheck())
            return true;

        if (DurabilityLowHandler.TryHandleStartPrecheck())
            return true;

        return false;
    }
}
