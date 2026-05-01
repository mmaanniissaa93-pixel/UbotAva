using System;
using UBot.Core;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Protection.Components.Town;

public class NoHealthPotionsHandler : AbstractTownHandler
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
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnUseItem", new Action<byte>(OnUseItem), EventOwner);
    }

    /// <summary>
    ///     Cores the on use item.
    /// </summary>
    private static void OnUseItem(byte slot)
    {
        if (UBot.Core.RuntimeAccess.Core.Bot.Running)
            CheckForHpPotions();
    }

    internal static bool TryHandleStartPrecheck()
    {
        return CheckForHpPotions();
    }

    private static bool CheckForHpPotions()
    {
        if (!UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckNoHPPotions"))
            return false;

        if (PlayerInTownScriptRegion())
            return false;

        if (UBot.Core.RuntimeAccess.Session.Player.State.LifeState == LifeState.Dead)
            return false;

        var typeIdFilter = new TypeIdFilter(3, 3, 1, 1);
        if (
            UBot.Core.RuntimeAccess.Session.Player.Inventory.GetSumAmount(typeIdFilter)
            > UBot.Core.RuntimeAccess.Player.Get<int>("UBot.Protection.ThresholdHPPotionsLeft")
        )
            return false;

        var used = UBot.Core.RuntimeAccess.Session.Player.UseReturnScroll();

        Log.WarnLang("ReturnToTownNoHealth");
        return used;
    }
}
