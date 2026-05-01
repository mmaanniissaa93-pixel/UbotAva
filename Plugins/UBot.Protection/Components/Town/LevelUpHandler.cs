using UBot.Core;
using UBot.Core.Event;

namespace UBot.Protection.Components.Town;

public class LevelUpHandler : AbstractTownHandler
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
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnLevelUp", OnPlayerLevelUp, EventOwner);
    }

    /// <summary>
    ///     Cores the on player level up.
    /// </summary>
    private static void OnPlayerLevelUp()
    {
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running)
            return;

        if (!UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckLevelUp"))
            return;

        if (PlayerInTownScriptRegion())
            return;

        Log.NotifyLang("ReturnToTownLevelUpAchieved");

        UBot.Core.RuntimeAccess.Session.Player.UseReturnScroll();
    }
}
