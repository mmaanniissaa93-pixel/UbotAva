using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Skill;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Skill;

public class SkillLearnResponse : IPacketHandler
{
    public ushort Opcode => 0xB0A1;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var player = ProtocolRuntime.GameState?.Player as Player;
        if (player == null || packet.ReadByte() != 1)
            return;

        var skillId = packet.ReadUInt();
        dynamic skill = ProtocolRuntime.GameState?.GetReference("RefSkill", skillId);
        if (skill == null)
            return;

        var existingSkill = player.Skills.GetSkillRecordByName(skill.GetRealName());
        if (existingSkill == null)
        {
            var skillInfo = new SkillInfo(skillId, true);
            player.Skills.KnownSkills.Add(skillInfo);
            ProtocolRuntime.EventBus?.Fire("OnSkillLearned", skillInfo);
        }
        else
        {
            var oldSkill = new SkillInfo(existingSkill.Id, false);
            existingSkill.Id = skillId;
            ProtocolRuntime.EventBus?.Fire("OnSkillUpgraded", oldSkill, existingSkill);
        }
    }
}
