using UBot.Core;
using UBot.Core.Event;

namespace UBot.Protection.Components.Town;

public class AmmunitionHandler : AbstractTownHandler
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
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnUpdateAmmunition", OnUpdateAmmunition, EventOwner);
    }

    /// <summary>
    ///     Cores the on update ammunition.
    /// </summary>
    private static void OnUpdateAmmunition()
    {
        if (UBot.Core.RuntimeAccess.Core.Bot.Running)
            CheckForAmmunition();
    }

    internal static bool TryHandleStartPrecheck()
    {
        return CheckForAmmunition();
    }

    private static bool CheckForAmmunition()
    {
        if (!UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckNoArrows"))
            return false;

        var currentAmmunition = UBot.Core.RuntimeAccess.Session.Player.GetAmmunitionAmount(true);
        if (currentAmmunition == -1 || currentAmmunition > 10)
            return false;

        if (PlayerInTownScriptRegion())
            return false;

        Log.WarnLang("ReturnToTownNoAmmo");
        return UBot.Core.RuntimeAccess.Session.Player.UseReturnScroll();
    }
}
