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
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnUpdateHPMP", OnUpdateHPMP);
    }

    /// <summary>
    ///     Cores the on player HPMP update.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    private static void OnUpdateHPMP()
    {
        if ((UBot.Core.RuntimeAccess.Session.Player.BadEffect & BadEffect.Zombie) == BadEffect.Zombie)
            return;

        var useManaPotion = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.checkUseVigorMP");
        if (useManaPotion)
        {
            var minMana = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.numPlayerMPVigorPotionMin", 50);

            var manaPercent = 100.0 * UBot.Core.RuntimeAccess.Session.Player.Mana / UBot.Core.RuntimeAccess.Session.Player.MaximumMana;
            if (manaPercent <= minMana && UBot.Core.RuntimeAccess.Session.Player.UseVigorPotion())
                return;
        }

        var useHealthPotion = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.checkUseVigorHP");
        if (!useHealthPotion)
            return;

        var minHealth = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.numPlayerHPVigorPotionMin", 50);

        var healthPercent = 100.0 * UBot.Core.RuntimeAccess.Session.Player.Health / UBot.Core.RuntimeAccess.Session.Player.MaximumHealth;
        if (healthPercent <= minHealth)
            UBot.Core.RuntimeAccess.Session.Player.UseVigorPotion();
    }
}
