using UBot.Core;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Protection.Components.Player;

internal class VigorRecoveryHandler
{
    private static readonly object EventOwner = new();

    /// <summary>
    ///     Initialize the <see cref="VigorRecoveryHandler" />
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
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnUpdateHPMP", OnUpdateHPMP, EventOwner);
    }

    /// <summary>
    ///     Cores the on player HPMP update.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    private static void OnUpdateHPMP()
    {
        if ((UBot.Core.RuntimeAccess.Session.Player.BadEffect & BadEffect.Zombie) == BadEffect.Zombie)
            return;

        var useManaPotion = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckUseVigorMP");
        if (useManaPotion)
        {
            var minMana = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.ThresholdPlayerMPVigorPotionMin", 50);

            var manaPercent = 100.0 * UBot.Core.RuntimeAccess.Session.Player.Mana / UBot.Core.RuntimeAccess.Session.Player.MaximumMana;
            if (manaPercent <= minMana && UBot.Core.RuntimeAccess.Session.Player.UseVigorPotion())
                return;
        }

        var useHealthPotion = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckUseVigorHP");
        if (!useHealthPotion)
            return;

        var minHealth = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.ThresholdPlayerHPVigorPotionMin", 50);

        var healthPercent = 100.0 * UBot.Core.RuntimeAccess.Session.Player.Health / UBot.Core.RuntimeAccess.Session.Player.MaximumHealth;
        if (healthPercent <= minHealth)
            UBot.Core.RuntimeAccess.Session.Player.UseVigorPotion();
    }
}
