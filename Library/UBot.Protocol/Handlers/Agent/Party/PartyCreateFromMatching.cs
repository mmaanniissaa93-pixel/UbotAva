using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Party;

public class PartyCreateFromMatching : IPacketHandler
{
    public ushort Opcode => 0x3065;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(PartyCreateFromMatching), packet);
    }
}
