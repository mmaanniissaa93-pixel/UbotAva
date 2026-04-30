using System.Collections.Generic;
using UBot.Core.Abstractions.Services;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreSkillConfig : ISkillConfig
{
    public bool UseSkillsInOrder => UBot.Core.RuntimeAccess.Player.Get("UBot.Skills.checkUseSkillsInOrder", false);

    public IEnumerable<uint> GetBaseSkills()
    {
        return UBot.Core.RuntimeAccess.Session.ReferenceManager?.GetBaseSkills();
    }

    public object GetRefSkill(uint id)
    {
        return UBot.Core.RuntimeAccess.Session.ReferenceManager?.GetRefSkill(id);
    }
}
