using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Objects;
using UBot.Core.Objects.Skill;

namespace UBot.Training.Bundle.Attack;

internal class AttackBundle : IBundle
{
    /// <summary>
    ///     The last tick count for checking func call
    /// </summary>
    private int _lastTick = UBot.Core.RuntimeAccess.Core.TickCount;

    /// <summary>
    ///     Invokes this instance.
    /// </summary>
    public void Invoke()
    {
        if (UBot.Core.RuntimeAccess.Session.SelectedEntity == null || !UBot.Core.RuntimeAccess.Session.Player.CanAttack)
            return;

        if (UBot.Core.RuntimeAccess.Session.SelectedEntity.IsBehindObstacle)
        {
            Log.Debug("Deselecting entity because it moved behind an obstacle!");

            if (UBot.Core.RuntimeAccess.Session.Player.InAction)
                SkillManager.CancelAction();

            UBot.Core.RuntimeAccess.Session.SelectedEntity?.TryDeselect();
            UBot.Core.RuntimeAccess.Session.SelectedEntity = null;

            return;
        }

        bool dontFollowMobs = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Training.checkBoxDontFollowMobs");
        if (dontFollowMobs && !UBot.Core.RuntimeAccess.Core.Bot.Botbase.Area.IsInSight(UBot.Core.RuntimeAccess.Session.SelectedEntity))
        {
            Log.Debug("Deselecting entity because it moved far away from training area!");

            if (UBot.Core.RuntimeAccess.Session.Player.InAction)
                SkillManager.CancelAction();

            UBot.Core.RuntimeAccess.Session.SelectedEntity?.TryDeselect();
            UBot.Core.RuntimeAccess.Session.SelectedEntity = null;

            double distance = UBot.Core.RuntimeAccess.Session.Player.Position.DistanceTo(Container.Bot.Area.Position);
            bool hasCollision = UBot.Core.RuntimeAccess.Session.Player.Position.HasCollisionBetween(Container.Bot.Area.Position);

            if (distance > Container.Bot.Area.Radius && !hasCollision)
                UBot.Core.RuntimeAccess.Session.Player.MoveTo(Container.Bot.Area.Position, false);

            return;
        }

        if (
            SkillManager.ImbueSkill != null
            && !UBot.Core.RuntimeAccess.Session.Player.State.HasActiveBuff(SkillManager.ImbueSkill, out _)
            && SkillManager.ImbueSkill.CanBeCasted
        )
            SkillManager.ImbueSkill.Cast(buff: true);

        if (UBot.Core.RuntimeAccess.Core.TickCount - _lastTick < 500)
            return;

        _lastTick = UBot.Core.RuntimeAccess.Core.TickCount;

        //if (UBot.Core.RuntimeAccess.Session.Player.InAction && !SkillManager.IsLastCastedBasic)
        //  return;

        var useTeleportSkill = UBot.Core.RuntimeAccess.Player.Get("UBot.Skills.checkUseTeleportSkill", false);
        if (useTeleportSkill && CastTeleportation())
            return;

        //var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        var skill = SkillManager.GetNextSkill();

        //Log.Debug($"Getnextskill: {stopwatch.ElapsedMilliseconds} Action:{UBot.Core.RuntimeAccess.Session.Player.InAction} Entity:{UBot.Core.RuntimeAccess.Session.SelectedEntity != null} LA:{SkillManager.IsLastCastedBasic} Skill:{skill}");

        if (!UBot.Core.RuntimeAccess.Session.Player.InAction)
            Log.Status("Attacking");

        if (skill == null)
        {
            if (UBot.Core.RuntimeAccess.Session.Player.InAction)
                return;

            if (UBot.Core.RuntimeAccess.Player.Get("UBot.Skills.checkUseDefaultAttack", true))
                SkillManager.CastAutoAttack();

            return;
        }

        if (UBot.Core.RuntimeAccess.Session.Player.InAction && SkillManager.IsLastCastedBasic)
            SkillManager.CancelAction();

        var uniqueId = UBot.Core.RuntimeAccess.Session.SelectedEntity?.UniqueId;
        if (uniqueId == null)
            return;

        skill?.Cast(uniqueId.Value);
    }

    /// <summary>
    ///     Refreshes this instance.
    /// </summary>
    public void Refresh()
    {
        //Nothing to do here
    }

    public void Stop()
    {
        //Nothing to do
    }

    /// <summary>
    ///     Casts the teleportation skill if it's set up.
    /// </summary>
    /// <returns></returns>
    private bool CastTeleportation()
    {
        if (SkillManager.TeleportSkill?.CanBeCasted != true || UBot.Core.RuntimeAccess.Session.SelectedEntity?.State.LifeState != LifeState.Alive)
            return false;

        var distanceToMonster = UBot.Core.RuntimeAccess.Session.SelectedEntity?.DistanceToPlayer;
        var availableDistance = GetTeleportTravelDistance(SkillManager.TeleportSkill);

        if (availableDistance <= 0)
        {
            Log.Warn("The selected teleportation skill does not have a distance. Is this really a teleport skill?");
        }
        else
        {
            var distanceAfterCasting = distanceToMonster - availableDistance;
            if (distanceAfterCasting < 0)
                distanceAfterCasting *= -1;

            if (distanceAfterCasting < distanceToMonster)
            {
                SkillManager.TeleportSkill.CastAt(UBot.Core.RuntimeAccess.Session.SelectedEntity.Position);

                Log.Debug(
                    $"Used teleportation skill [{SkillManager.TeleportSkill.Record.GetRealName()}] (before: {distanceToMonster}m, after: {distanceAfterCasting}m, traveled: {availableDistance}m)"
                );

                return true;
            }
        }

        return false;
    }

    private static double GetTeleportTravelDistance(SkillInfo skill)
    {
        var record = skill?.Record;
        if (record == null)
            return 0;

        var parameters = (System.Collections.Generic.List<int>)record.Params;
        if (parameters != null && parameters.Count > 0)
        {
            // tel3 marker used by movement skills such as Ghost Walk family.
            var tel3Index = parameters.FindIndex(value => value == 1952803891);
            if (tel3Index >= 0 && parameters.Count > tel3Index + 2)
            {
                var markerDistance = parameters[tel3Index + 2] / 10d;
                if (markerDistance > 0)
                    return markerDistance;
            }

            if (parameters.Count > 3)
            {
                var legacyDistance = parameters[3] / 10d;
                if (legacyDistance > 0)
                    return legacyDistance;
            }
        }

        var actionRangeDistance = record.Action_Range / 10d;
        return actionRangeDistance > 0 ? actionRangeDistance : 0;
    }
}


