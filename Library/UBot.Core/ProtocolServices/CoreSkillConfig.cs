using System.Collections.Generic;
using UBot.Core.Abstractions.Services;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreSkillConfig : ISkillConfig
{
    public bool UseSkillsInOrder => PlayerConfig.Get("UBot.Skills.checkUseSkillsInOrder", false);

    public IEnumerable<uint> GetBaseSkills()
    {
        return Game.ReferenceManager?.GetBaseSkills();
    }

    public object GetRefSkill(uint id)
    {
        return Game.ReferenceManager?.GetRefSkill(id);
    }
}
