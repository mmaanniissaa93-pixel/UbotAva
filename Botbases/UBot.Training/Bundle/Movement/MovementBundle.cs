using System.Threading;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Objects.Spawn;

namespace UBot.Training.Bundle.Movement;

internal class MovementBundle : IBundle
{
    /// <summary>
    ///     Gets or sets the configuration.
    /// </summary>
    /// <value>
    ///     The configuration.
    /// </value>
    public MovementConfig Config { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether [last entity was behind obstacle].
    ///     Used to move around even though the player is being attacked.
    /// </summary>
    /// <value>
    ///     <c>true</c> if [last entity was behind obstacle]; otherwise, <c>false</c>.
    /// </value>
    public bool LastEntityWasBehindObstacle { get; set; }

    /// <summary>
    ///     Invokes this instance.
    /// </summary>
    public void Invoke()
    {
        if (UBot.Core.RuntimeAccess.Session.SelectedEntity != null && !LastEntityWasBehindObstacle)
            return;

        var playerUnderAttack = SpawnManager.Any<SpawnedMonster>(m =>
            m.AttackingPlayer && Container.Bot.Area.IsInSight(m)
        );
        if (playerUnderAttack && !LastEntityWasBehindObstacle)
            return;

        if (UBot.Core.RuntimeAccess.Session.Player.Movement.Moving)
            return;

        if (
            UBot.Core.RuntimeAccess.Player.Get("UBot.Party.AlwaysFollowPartyMaster", false)
            && UBot.Core.RuntimeAccess.Session.Party.IsInParty
            && !UBot.Core.RuntimeAccess.Session.Party.IsLeader
        )
        {
            if (UBot.Core.RuntimeAccess.Session.Player.InAction)
                return;

            var player = UBot.Core.RuntimeAccess.Session.Party.Leader?.Player;
            if (player != null && player.Position.DistanceToPlayer() >= 10)
                UBot.Core.RuntimeAccess.Session.Player.MoveTo(player.Position);

            return;
        }

        var distance = UBot.Core.RuntimeAccess.Session.Player.Position.DistanceTo(Container.Bot.Area.Position);
        var hasCollision = UBot.Core.RuntimeAccess.Session.Player.Position.HasCollisionBetween(Container.Bot.Area.Position);

        //Go back if the player is out of the radius
        if ((distance > Container.Bot.Area.Radius || (Config.WalkToCenter && distance > 3)) && !hasCollision)
        {
            Log.Status("Walking to center");
            UBot.Core.RuntimeAccess.Session.Player.MoveTo(Container.Bot.Area.Position);

            return;
        }

        if (Config.WalkToCenter)
            return;

        Log.Status("Walking around");

        //Find a not colliding position. Do it in a while loop to prevent the bot from processing it in the next cycle (tick).
        //This is how we can find our next position very fast instead of waiting for the next circle to come.
        var destination = Container.Bot.Area.GetRandomPosition();

        var attempt = 0;
        while (UBot.Core.RuntimeAccess.Session.Player.Position.HasCollisionBetween(destination) && distance < Container.Bot.Area.Radius)
        {
            destination = Container.Bot.Area.GetRandomPosition();

            if (attempt++ > 3)
                break;
        }

        UBot.Core.RuntimeAccess.Session.Player.MoveTo(destination, false);
    }

    /// <summary>
    ///     Refreshes this instance.
    /// </summary>
    public void Refresh()
    {
        Config = new MovementConfig
        {
            WalkAround = UBot.Core.RuntimeAccess.Player.Get("UBot.Training.radioWalkAround", true),
            WalkToCenter = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Training.radioCenter"),
        };
    }

    public void Stop()
    {
        LastEntityWasBehindObstacle = false;
    }
}
