using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Protocol;

namespace UBot.Protocol.Commands.Agent.Skill;

public class SkillWithdrawRequest : IPacketHandler
{
    public ushort Opcode => 0x7202;

    public PacketDestination Destination => PacketDestination.Server;

    public void Invoke(Packet packet)
    {
        var player = ProtocolRuntime.GameState?.Player as Player;
        if (player == null)
            return;

        packet.ReadUInt();
        var skillId = packet.ReadUInt();
        var level = packet.ReadByte();

        player.Skills.PendingWithdrawSkill = skillId;
        ProtocolRuntime.EventBus?.Fire("OnWithdrawSkillRequest", skillId, level);
    }
}

