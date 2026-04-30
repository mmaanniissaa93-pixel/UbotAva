using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Skill;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Skill;

public class SkillWithdrawResponse : IPacketHandler
{
    public ushort Opcode => 0xB202;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var player = UBot.Protocol.ProtocolRuntime.GameState?.Player as Player;
        if (player == null)
            return;

        if (packet.ReadByte() != 1)
        {
            player.Skills.PendingWithdrawSkill = 0;
            return;
        }

        var skillId = packet.ReadUInt();
        var oldSkill = player.Skills.GetSkillInfoById(player.Skills.PendingWithdrawSkill);

        if (oldSkill == null)
            return;

        var newSkill = new SkillInfo(skillId, true);
        player.Skills.RemoveSkillById(oldSkill.Id);

        if (skillId != oldSkill.Id)
            player.Skills.KnownSkills.Add(newSkill);

        player.Skills.PendingWithdrawSkill = 0;
        UBot.Protocol.ProtocolRuntime.EventBus?.Fire("OnWithdrawSkill", oldSkill, newSkill);
    }
}
