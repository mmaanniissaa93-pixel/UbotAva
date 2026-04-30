using System.Collections.Generic;
using System.Linq;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Extensions;
using UBot.Core.Objects;
using UBot.Core.Objects.Spawn;

namespace UBot.Training.Bundle.Target;

internal class TargetBundle : IBundle
{
    private const int BLACKLIST_TIMEOUT = 5_000;

    #region Fields

    private Dictionary<uint, int> _blacklist;

    #endregion Fields

    #region Constructor

    public TargetBundle()
    {
        SubscribeEvents();
    }

    #endregion Constructor

    #region Events

    private void OnTargetBehindObstacle()
    {
        if (UBot.Core.RuntimeAccess.Session.SelectedEntity == null)
            return;

        var selectedEntityUniqueId = UBot.Core.RuntimeAccess.Session.SelectedEntity.UniqueId;
        UBot.Core.RuntimeAccess.Session.SelectedEntity?.TryDeselect();
        UBot.Core.RuntimeAccess.Session.SelectedEntity = null;

        Bundles.Movement.LastEntityWasBehindObstacle = true;

        if (_blacklist?.TryAdd(selectedEntityUniqueId, UBot.Core.RuntimeAccess.Core.TickCount) == true)
            Log.Debug($"Add mob [{selectedEntityUniqueId} to blacklist for {BLACKLIST_TIMEOUT}ms");
    }

    #endregion Events

    #region Methods

    private void SubscribeEvents()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnTargetBehindObstacle", OnTargetBehindObstacle);
    }

    /// <summary>
    ///     Invokes this instance.
    /// </summary>
    public void Invoke()
    {
        _blacklist?.RemoveAll(
            (uniqueId, tick) =>
            {
                var flag = UBot.Core.RuntimeAccess.Core.TickCount - tick > BLACKLIST_TIMEOUT;
                if (flag)
                    Log.Debug($"Removed mob [{uniqueId} from blacklist!");

                return flag;
            }
        );

        var attacker = GetFromCurrentAttackers();
        if (attacker != null && UBot.Core.RuntimeAccess.Session.SelectedEntity == null)
        {
            Log.Debug("[TargetBundle] Emergency situation: Attacking the weaker mob first!");

            if (attacker.TrySelect())
                Bundles.Movement.LastEntityWasBehindObstacle = false;

            return;
        }

        if (
            attacker != null
            && SpawnManager.TryGetEntity<SpawnedMonster>(UBot.Core.RuntimeAccess.Session.SelectedEntity.UniqueId, out var selectedMonster)
            && (byte)attacker.Rarity < (byte)selectedMonster.Rarity
        )
        {
            Log.Debug("[TargetBundle] Emergency situation: Found a weaker mob to attack first, switching target!");

            if (attacker.TrySelect())
                Bundles.Movement.LastEntityWasBehindObstacle = false;

            return;
        }

        var warlockModeEnabled = UBot.Core.RuntimeAccess.Player.Get("UBot.Skills.checkWarlockMode", false);
        if (warlockModeEnabled && UBot.Core.RuntimeAccess.Session.SelectedEntity?.State.HasTwoDots() == true)
            return;

        if (UBot.Core.RuntimeAccess.Session.SelectedEntity != null && UBot.Core.RuntimeAccess.Session.SelectedEntity is not SpawnedMonster)
            UBot.Core.RuntimeAccess.Session.SelectedEntity = null;

        if (UBot.Core.RuntimeAccess.Session.SelectedEntity?.State.LifeState == LifeState.Alive)
            return;

        var monster = GetNearestEnemy();
        if (monster == null)
            return;

        if (!Container.Bot.Area.IsInSight(monster))
            return;

        if (monster.TrySelect())
            Bundles.Movement.LastEntityWasBehindObstacle = false;
    }

    private SpawnedMonster GetFromCurrentAttackers()
    {
        var attackWeakerFirst = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Training.checkAttackWeakerFirst");
        if (!attackWeakerFirst || !IsEmergencySituation())
            return null;

        if (
            !SpawnManager.TryGetEntities<SpawnedMonster>(
                e => e.AttackingPlayer && e.State.LifeState == LifeState.Alive,
                out var entities
            )
        )
            return null;

        return entities
            .OrderBy(e => (byte)e.Rarity)
            .OrderBy(e => e.Record.Level)
            .OrderByDescending(e => e.Position.DistanceToPlayer())
            .FirstOrDefault();
    }

    private bool IsEmergencySituation()
    {
        return SpawnManager.Any<SpawnedMonster>(e =>
            e.AttackingPlayer && e.State.LifeState == LifeState.Alive && Bundles.Avoidance.AvoidMonster(e.Rarity)
        );
    }

    /// <summary>
    ///     Gets the nearest enemy.
    /// </summary>
    /// <returns></returns>
    private SpawnedMonster GetNearestEnemy()
    {
        var warlockModeEnabled = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Skills.checkWarlockMode");
        var ignorePillar = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Training.checkBoxDimensionPillar");

        if (
            !SpawnManager.TryGetEntities<SpawnedMonster>(
                m =>
                    m.State.LifeState == LifeState.Alive
                    && //Only alive
                    !(warlockModeEnabled && m.State.HasTwoDots())
                    && //Has two Dots?
                    m.IsBehindObstacle == false
                    && //Is not behind obstacle
                    (_blacklist == null || !_blacklist.ContainsKey(m.UniqueId))
                    && //Is not blacklisted
                    (m.AttackingPlayer || !Bundles.Avoidance.AvoidMonster(m.Rarity))
                    && //Is attacking player or shouldn't be avoided
                    Container.Bot.Area.IsInSight(m)
                    && //Is in training area
                    !m.Record.IsPandora
                    && //Isn't pandora box
                    !(m.Record.IsDimensionPillar && ignorePillar)
                    && //Isn't dimension pillar
                    !m.Record.IsSummonFlower,
                out var entities
            )
        )
            return default;

        return entities
            .OrderBy(m => m.Movement.Source.DistanceTo(Container.Bot.Area.Position))
            .OrderBy(m => Bundles.Avoidance.PreferMonster(m.Rarity))
            .OrderByDescending(m => m.AttackingPlayer)
            .FirstOrDefault();
    }

    /// <summary>
    ///     Refreshes this instance.
    /// </summary>
    public void Refresh()
    {
        _blacklist = new Dictionary<uint, int>(8);
    }

    public void Stop()
    {
        _blacklist = null;
    }

    #endregion Methods
}
