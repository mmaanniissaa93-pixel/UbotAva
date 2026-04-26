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
    ///     Unsubscribes all events.
    /// </summary>
    public static void UnsubscribeAll()
    {
        EventManager.UnsubscribeOwner(EventOwner);
    }

    /// <summary>
    ///     Subscribes the events.
    /// </summary>
    private static void SubscribeEvents()
    {
        EventManager.SubscribeEvent("OnTick", OnPlayerBadEffect, EventOwner);
    }

    /// <summary>
    ///     Cores the on player bad effect.
    /// </summary>
    private static void OnPlayerBadEffect()
    {
        var useUniversalPill = PlayerConfig.Get<bool>("UBot.Protection.checkUseUniversalPills", true);
        if (!useUniversalPill)
            return;

        if ((Game.Player.BadEffect & BadEffectAll.UniversallPillEffects) != 0)
            Game.Player.UseUniversalPill();

        if ((Game.Player.BadEffect & BadEffectAll.PurificationPillEffects) != 0)
            Game.Player.UsePurificationPill();
    }
}
