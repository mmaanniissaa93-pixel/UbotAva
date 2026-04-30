using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Objects.Spawn;
using UBot.Lure.Components;

namespace UBot.Lure.Bundle;

internal class TargetBundle
{
    public static void Tick()
    {
        if (!LureConfig.UseAttackingSkills && !LureConfig.UseNormalAttack)
            return;

        SpawnManager.TryGetEntity<SpawnedMonster>(
            f =>
                f.AttackingPlayer == false
                && UBot.Core.RuntimeAccess.Session.SelectedEntity?.UniqueId != f.UniqueId
                && f.Position.DistanceTo(LureConfig.Area.Position) > 15
                && f.Position.DistanceTo(LureConfig.Area.Position) <= LureConfig.Area.Radius,
            out var mob
        );

        if (UBot.Core.RuntimeAccess.Session.Player.InAction)
            SkillManager.CancelAction();

        mob?.TrySelect();
    }
}
