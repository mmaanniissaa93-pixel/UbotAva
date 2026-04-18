using UBot.Core;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Protection.Components.Player;

internal class VigorRecoveryHandler
{
    /// <summary>
    ///     Initialize the <see cref="VigorRecoveryHandler" />
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
        EventManager.SubscribeEvent("OnUpdateHPMP", OnUpdateHPMP);
    }

    /// <summary>
    ///     Cores the on player HPMP update.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    private static void OnUpdateHPMP()
    {
        if ((Game.Player.BadEffect & BadEffect.Zombie) == BadEffect.Zombie)
            return;

        var useManaPotion = PlayerConfig.Get<bool>("UBot.Protection.checkUseVigorMP");
        if (useManaPotion)
        {
            var minMana = PlayerConfig.Get("UBot.Protection.numPlayerMPVigorPotionMin", 50);

            var manaPercent = 100.0 * Game.Player.Mana / Game.Player.MaximumMana;
            if (manaPercent <= minMana && Game.Player.UseVigorPotion())
                return;
        }

        var useHealthPotion = PlayerConfig.Get<bool>("UBot.Protection.checkUseVigorHP");
        if (!useHealthPotion)
            return;

        var minHealth = PlayerConfig.Get("UBot.Protection.numPlayerHPVigorPotionMin", 50);

        var healthPercent = 100.0 * Game.Player.Health / Game.Player.MaximumHealth;
        if (healthPercent <= minHealth)
            Game.Player.UseVigorPotion();
    }
}
