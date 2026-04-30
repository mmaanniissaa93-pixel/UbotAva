using UBot.Core;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Protection.Components.Player;

public class HealthManaRecoveryHandler
{
    private static readonly object EventOwner = new();

    /// <summary>
    ///     Initialize the <see cref="HealthRecoveryHandler" />
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
        UBot.Core.RuntimeAccess.Events.UnsubscribeOwner(EventOwner);
    }

    /// <summary>
    ///     Subscribes the events.
    /// </summary>
    private static void SubscribeEvents()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnTick", OnTick, EventOwner);
    }

    /// <summary>
    ///     Cores the on player HP update.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    private static void OnUpdateHP()
    {
        var autoHealth = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.checkUseHPPotionsPlayer", true);
        if (!autoHealth)
            return;

        if ((UBot.Core.RuntimeAccess.Session.Player.BadEffect & BadEffect.Zombie) == BadEffect.Zombie)
            return;

        var minHealth = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.numPlayerHPPotionMin", 75);

        var healthPercent = 100.0 * UBot.Core.RuntimeAccess.Session.Player.Health / UBot.Core.RuntimeAccess.Session.Player.MaximumHealth;
        if (healthPercent <= minHealth)
            UBot.Core.RuntimeAccess.Session.Player.UseHealthPotion();
    }

    /// <summary>
    ///     Cores the on player MP update.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    private static void OnUpdateMP()
    {
        var autoMana = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.checkUseMPPotionsPlayer", true);
        if (!autoMana)
            return;

        var minMana = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.numPlayerMPPotionMin", 75);

        var manaPercent = 100.0 * UBot.Core.RuntimeAccess.Session.Player.Mana / UBot.Core.RuntimeAccess.Session.Player.MaximumMana;
        if (manaPercent <= minMana)
            UBot.Core.RuntimeAccess.Session.Player.UseManaPotion();
    }

    /// <summary>
    ///     On tick
    /// </summary>
    private static void OnTick()
    {
        OnUpdateHP();
        OnUpdateMP();
    }
}
