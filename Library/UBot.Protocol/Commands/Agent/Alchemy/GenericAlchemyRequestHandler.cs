using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Commands.Agent.Alchemy;

public class GenericAlchemyRequestHandler : IPacketHandler
{
    public ushort Opcode => 0;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(GenericAlchemyRequestHandler), packet);
    }
}
