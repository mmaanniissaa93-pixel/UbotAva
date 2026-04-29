using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Skill;

public class SkillMasteryWithdrawResponse : IPacketHandler
{
    public ushort Opcode => 0xB203;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var player = ProtocolRuntime.GameState?.Player as Player;
        if (player == null || packet.ReadByte() != 0x01)
            return;

        var masteryId = packet.ReadUInt();
        var level = packet.ReadByte();

        player.Skills.UpdateMasteryLevel(masteryId, level);
        ProtocolRuntime.Feedback?.Notify(
            $"The mastery [{player.Skills.GetMasteryInfoById(masteryId).Record.Name}] was withdrawn to [lv.{level}]"
        );
        ProtocolRuntime.EventBus?.Fire("OnWithdrawMastery", masteryId, level);
    }
}
