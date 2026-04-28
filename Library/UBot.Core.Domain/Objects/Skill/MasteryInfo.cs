using UBot.Core.Abstractions;

namespace UBot.Core.Objects.Skill;

public class MasteryInfo
{
    public byte Level { get; set; }
    public uint Id { get; set; }

    public dynamic Record => ReferenceProvider.Instance?.GetRefSkillMastery(Id);
}
