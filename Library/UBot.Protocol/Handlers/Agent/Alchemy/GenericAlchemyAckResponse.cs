using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Alchemy;

public class GenericAlchemyAckResponse : IPacketHandler
{
    public ushort Opcode => 0;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(GenericAlchemyAckResponse), packet);
    }
}
