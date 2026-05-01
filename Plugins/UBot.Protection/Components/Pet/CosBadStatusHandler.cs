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
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnTick", OnTick, EventOwner);
    }

    /// <summary>
    ///     <inheritdoc />
    /// </summary>
    private static void OnTick()
    {
        if (!UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckUseAbnormalStatePotion", true))
            return;

        if (
            UBot.Core.RuntimeAccess.Session.Player.Growth != null
            && (
                (UBot.Core.RuntimeAccess.Session.Player.Growth.BadEffect & BadEffectAll.UniversallPillEffects) != 0
                || (UBot.Core.RuntimeAccess.Session.Player.Growth.BadEffect & BadEffectAll.PurificationPillEffects) != 0
            )
        )
            UBot.Core.RuntimeAccess.Session.Player.Growth.UseBadStatusPotion();

        if (UBot.Core.RuntimeAccess.Session.Player.Fellow != null && (UBot.Core.RuntimeAccess.Session.Player.Fellow.BadEffect & BadEffectAll.UniversallPillEffects) != 0)
            UBot.Core.RuntimeAccess.Session.Player.Fellow.UseBadStatusPotion(); //PurificationPillEffects are not removed on fellow by pills

        if (
            UBot.Core.RuntimeAccess.Session.Player.Transport != null
            && (
                (UBot.Core.RuntimeAccess.Session.Player.Transport.BadEffect & BadEffectAll.UniversallPillEffects) != 0
                || (UBot.Core.RuntimeAccess.Session.Player.Transport.BadEffect & BadEffectAll.PurificationPillEffects) != 0
            )
        )
            UBot.Core.RuntimeAccess.Session.Player.Transport.UseBadStatusPotion();

        if (
            UBot.Core.RuntimeAccess.Session.Player.JobTransport != null
            && (
                (UBot.Core.RuntimeAccess.Session.Player.JobTransport.BadEffect & BadEffectAll.UniversallPillEffects) != 0
                || (UBot.Core.RuntimeAccess.Session.Player.JobTransport.BadEffect & BadEffectAll.PurificationPillEffects) != 0
            )
        )
            UBot.Core.RuntimeAccess.Session.Player.JobTransport.UseBadStatusPotion();
    }
}
