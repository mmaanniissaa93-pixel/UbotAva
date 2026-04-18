using UBot.Core;
using UBot.Core.Event;

namespace UBot.Protection.Components.Town;

public class AmmunitionHandler : AbstractTownHandler
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
        EventManager.SubscribeEvent("OnUpdateAmmunition", OnUpdateAmmunition);
    }

    /// <summary>
    ///     Cores the on update ammunition.
    /// </summary>
    private static void OnUpdateAmmunition()
    {
        if (Kernel.Bot.Running)
            CheckForAmmunition();
    }

    internal static bool TryHandleStartPrecheck()
    {
        return CheckForAmmunition();
    }

    private static bool CheckForAmmunition()
    {
        if (!PlayerConfig.Get<bool>("UBot.Protection.checkNoArrows"))
            return false;

        var currentAmmunition = Game.Player.GetAmmunitionAmount(true);
        if (currentAmmunition == -1 || currentAmmunition > 10)
            return false;

        if (PlayerInTownScriptRegion())
            return false;

        Log.WarnLang("ReturnToTownNoAmmo");
        return Game.Player.UseReturnScroll();
    }
}
