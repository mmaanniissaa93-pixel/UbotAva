using UBot.Core;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Protection.Components.Pet;

internal class CosBadStatusHandler
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
        EventManager.SubscribeEvent("OnTick", OnTick, EventOwner);
    }

    /// <summary>
    ///     <inheritdoc />
    /// </summary>
    private static void OnTick()
    {
        if (!PlayerConfig.Get<bool>("UBot.Protection.checkUseAbnormalStatePotion", true))
            return;

        if (
            Game.Player.Growth != null
            && (
                (Game.Player.Growth.BadEffect & BadEffectAll.UniversallPillEffects) != 0
                || (Game.Player.Growth.BadEffect & BadEffectAll.PurificationPillEffects) != 0
            )
        )
            Game.Player.Growth.UseBadStatusPotion();

        if (Game.Player.Fellow != null && (Game.Player.Fellow.BadEffect & BadEffectAll.UniversallPillEffects) != 0)
            Game.Player.Fellow.UseBadStatusPotion(); //PurificationPillEffects are not removed on fellow by pills

        if (
            Game.Player.Transport != null
            && (
                (Game.Player.Transport.BadEffect & BadEffectAll.UniversallPillEffects) != 0
                || (Game.Player.Transport.BadEffect & BadEffectAll.PurificationPillEffects) != 0
            )
        )
            Game.Player.Transport.UseBadStatusPotion();

        if (
            Game.Player.JobTransport != null
            && (
                (Game.Player.JobTransport.BadEffect & BadEffectAll.UniversallPillEffects) != 0
                || (Game.Player.JobTransport.BadEffect & BadEffectAll.PurificationPillEffects) != 0
            )
        )
            Game.Player.JobTransport.UseBadStatusPotion();
    }
}
