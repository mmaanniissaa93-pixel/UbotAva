using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Entity;

public class EntityAnimationPromoteResponse : IPacketHandler
{
    public ushort Opcode => 0x3054;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var player = ProtocolRuntime.GameState?.Player as Player;
        if (player == null)
            return;

        var uniqueId = packet.ReadUInt();

        if (player.HasActiveAttackPet && uniqueId == player.Growth.UniqueId)
            ProtocolRuntime.EventBus?.Fire("OnGrowthLevelUp");
        else if (player.HasActiveFellowPet && uniqueId == player.Fellow.UniqueId)
            ProtocolRuntime.EventBus?.Fire("OnFellowLevelUp");
    }
}
