using UBot.Core;
using UBot.Core.Components;
using UBot.Lure.Components;

namespace UBot.Lure.Bundle;

internal static class AttackBundle
{
    private static uint _lastTargetId;

    public static void Tick()
    {
        if (
            UBot.Core.RuntimeAccess.Session.SelectedEntity == null
            || UBot.Core.RuntimeAccess.Session.Player.InAction
            || !UBot.Core.RuntimeAccess.Session.Player.CanAttack
            || UBot.Core.RuntimeAccess.Session.SelectedEntity.IsBehindObstacle
        )
            return;

        if (_lastTargetId == UBot.Core.RuntimeAccess.Session.SelectedEntity.UniqueId)
        {
            if (UBot.Core.RuntimeAccess.Session.Player.InAction)
                SkillManager.CancelAction();

            return;
        }

        if (LureConfig.UseAttackingSkills)
        {
            var skill = SkillManager.GetNextSkill();

            if (skill == null && !LureConfig.UseNormalAttack)
                return;

            Log.Status("Attacking");
            SkillManager.CancelAction();

            var uniqueId = UBot.Core.RuntimeAccess.Session.SelectedEntity?.UniqueId;
            if (uniqueId == null)
                return;

            skill?.Cast(uniqueId.Value);
            _lastTargetId = uniqueId.Value;
        }

        if (LureConfig.UseNormalAttack && !UBot.Core.RuntimeAccess.Session.Player.InAction)
            SkillManager.CastAutoAttack();
    }
}
