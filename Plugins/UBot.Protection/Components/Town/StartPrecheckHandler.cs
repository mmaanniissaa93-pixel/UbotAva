using System.Threading;
using UBot.Core;
using UBot.Core.Event;

namespace UBot.Protection.Components.Town;

internal static class StartPrecheckHandler
{
    public static void Initialize()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnStartBot", OnStartBot);
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
