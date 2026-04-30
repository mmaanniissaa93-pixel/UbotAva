using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Objects;

namespace UBot.Training.Bundle.Protection;

internal class ProtectionBundle : IBundle
{
    public void Invoke()
    {
        if (HealthRecovery.Active && HealthRecovery.Value > HealthRecovery.Current && !IsActive(HealthRecovery.SkillId))
            if ((UBot.Core.RuntimeAccess.Session.Player.BadEffect & BadEffect.Zombie) != BadEffect.Zombie) // is it need?
                SkillManager.CastBuff(UBot.Core.RuntimeAccess.Session.Player.Skills.GetSkillInfoById(HealthRecovery.SkillId));

        if (ManaRecovery.Active && ManaRecovery.Value > ManaRecovery.Current && !IsActive(ManaRecovery.SkillId))
            SkillManager.CastBuff(UBot.Core.RuntimeAccess.Session.Player.Skills.GetSkillInfoById(ManaRecovery.SkillId));

        if (BadStateRecovery.Active)
        {
            if (BadStateRecovery.IsUniversall && !IsActive(BadStateRecovery.SkillIdForUniversall))
                SkillManager.CastBuff(UBot.Core.RuntimeAccess.Session.Player.Skills.GetSkillInfoById(BadStateRecovery.SkillIdForUniversall));

            if (BadStateRecovery.IsPurification && !IsActive(BadStateRecovery.SkillIdForPurification))
                SkillManager.CastBuff(UBot.Core.RuntimeAccess.Session.Player.Skills.GetSkillInfoById(BadStateRecovery.SkillIdForPurification));
        }
    }

    private bool IsActive(uint skillId)
    {
        var skill = UBot.Core.RuntimeAccess.Session.Player.Skills.GetSkillInfoById(skillId);
        return UBot.Core.RuntimeAccess.Session.Player.State.HasActiveBuff(skill, out _);
    }

    public void Refresh()
    {
        //Nothing to do
    }

    public void Stop()
    {
        //Nothing to do
    }
}
