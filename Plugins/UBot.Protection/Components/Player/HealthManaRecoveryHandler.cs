using UBot.Core;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Protection.Components.Player;

public class HealthManaRecoveryHandler
{
    /// <summary>
    ///     Initialize the <see cref="HealthRecoveryHandler" />
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
        EventManager.SubscribeEvent("OnTick", OnTick);
    }

    /// <summary>
    ///     Cores the on player HP update.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    private static void OnUpdateHP()
    {
        var autoHealth = PlayerConfig.Get<bool>("UBot.Protection.checkUseHPPotionsPlayer", true);
        if (!autoHealth)
            return;

        if ((Game.Player.BadEffect & BadEffect.Zombie) == BadEffect.Zombie)
            return;

        var minHealth = PlayerConfig.Get("UBot.Protection.numPlayerHPPotionMin", 75);

        var healthPercent = 100.0 * Game.Player.Health / Game.Player.MaximumHealth;
        if (healthPercent <= minHealth)
            Game.Player.UseHealthPotion();
    }

    /// <summary>
    ///     Cores the on player MP update.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    private static void OnUpdateMP()
    {
        var autoMana = PlayerConfig.Get<bool>("UBot.Protection.checkUseMPPotionsPlayer", true);
        if (!autoMana)
            return;

        var minMana = PlayerConfig.Get("UBot.Protection.numPlayerMPPotionMin", 75);

        var manaPercent = 100.0 * Game.Player.Mana / Game.Player.MaximumMana;
        if (manaPercent <= minMana)
            Game.Player.UseManaPotion();
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
