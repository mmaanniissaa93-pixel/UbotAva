using UBot.Core;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Protection.Components.Pet;

public class AutoSummonAttackPet
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
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnKillSelectedEnemy", OnKillSelectedEnemy, EventOwner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnStartBot", OnStartBot, EventOwner);
    }

    /// <summary>
    /// </summary>
    /// <param name="uniqueId">The unique identifier.</param>
    private static void OnKillSelectedEnemy()
    {
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running)
            return;

        if (UBot.Core.RuntimeAccess.Session.Player.State.LifeState == LifeState.Dead)
            return;

        if (UBot.Core.RuntimeAccess.Session.Player.Growth != null || UBot.Core.RuntimeAccess.Session.Player.Fellow != null)
            return;

        if (!UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckAutoSummonAttackPet"))
            return;

        //if (UBot.Core.RuntimeAccess.Session.Player.State.BattleState != BattleState.InPeace)
        //    return;

        if (UBot.Core.RuntimeAccess.Session.Player.SummonFellow())
            return;

        UBot.Core.RuntimeAccess.Session.Player.SummonGrowth();
    }

    /// <summary>
    /// </summary>
    private static void OnStartBot()
    {
        if (UBot.Core.RuntimeAccess.Session.Player.Growth != null || UBot.Core.RuntimeAccess.Session.Player.Fellow != null)
            return;

        if (!UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckAutoSummonAttackPet"))
            return;

        if (UBot.Core.RuntimeAccess.Session.Player.State.BattleState != BattleState.InPeace)
            return;

        if (UBot.Core.RuntimeAccess.Session.Player.SummonFellow())
            return;

        UBot.Core.RuntimeAccess.Session.Player.SummonGrowth();
    }
}
