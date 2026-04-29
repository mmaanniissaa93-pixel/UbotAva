using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Party;

public class PartyAutoRefuseResponse : IPacketHandler
{
    public ushort Opcode => 0xB067;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(PartyAutoRefuseResponse), packet);
    }
}
