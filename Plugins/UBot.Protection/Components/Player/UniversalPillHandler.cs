using UBot.Core;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Protection.Components.Player;

public class UniversalPillHandler
{
    private static readonly object EventOwner = new();

    /// <summary>
    ///     Initialize the <see cref="UniversalPillHandler" />
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
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnTick", OnPlayerBadEffect, EventOwner);
    }

    /// <summary>
    ///     Cores the on player bad effect.
    /// </summary>
    private static void OnPlayerBadEffect()
    {
        var useUniversalPill = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckUseUniversalPills", true);
        if (!useUniversalPill)
            return;

        if ((UBot.Core.RuntimeAccess.Session.Player.BadEffect & BadEffectAll.UniversallPillEffects) != 0)
            UBot.Core.RuntimeAccess.Session.Player.UseUniversalPill();

        if ((UBot.Core.RuntimeAccess.Session.Player.BadEffect & BadEffectAll.PurificationPillEffects) != 0)
            UBot.Core.RuntimeAccess.Session.Player.UsePurificationPill();
    }
}
