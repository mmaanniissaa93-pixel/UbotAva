using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Skill;

public class SkillMasteryLearnResponse : IPacketHandler
{
    public ushort Opcode => 0xB0A2;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var player = UBot.Protocol.ProtocolRuntime.GameState?.Player as Player;
        if (player == null || packet.ReadByte() != 1)
            return;

        var masteryId = packet.ReadUInt();
        var level = packet.ReadByte();

        player.Skills.UpdateMasteryLevel(masteryId, level);
        UBot.Protocol.ProtocolRuntime.EventBus?.Fire("OnLearnSkillMastery", player.Skills.GetMasteryInfoById(masteryId));
    }
}
