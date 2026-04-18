using System;
using UBot.Core;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Protection.Components.Town;

public class NoHealthPotionsHandler : AbstractTownHandler
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
        EventManager.SubscribeEvent("OnUseItem", new Action<byte>(OnUseItem));
    }

    /// <summary>
    ///     Cores the on use item.
    /// </summary>
    private static void OnUseItem(byte slot)
    {
        if (Kernel.Bot.Running)
            CheckForHpPotions();
    }

    internal static bool TryHandleStartPrecheck()
    {
        return CheckForHpPotions();
    }

    private static bool CheckForHpPotions()
    {
        if (!PlayerConfig.Get<bool>("UBot.Protection.checkNoHPPotions"))
            return false;

        if (PlayerInTownScriptRegion())
            return false;

        if (Game.Player.State.LifeState == LifeState.Dead)
            return false;

        var typeIdFilter = new TypeIdFilter(3, 3, 1, 1);
        if (
            Game.Player.Inventory.GetSumAmount(typeIdFilter)
            > PlayerConfig.Get<int>("UBot.Protection.numHPPotionsLeft")
        )
            return false;

        var used = Game.Player.UseReturnScroll();

        Log.WarnLang("ReturnToTownNoHealth");
        return used;
    }
}
