using System.Collections.Generic;

namespace UBot.Core.Abstractions.Services;

public interface ISkillConfig
{
    bool UseSkillsInOrder { get; }
    IEnumerable<uint> GetBaseSkills();
    object GetRefSkill(uint id);
}
