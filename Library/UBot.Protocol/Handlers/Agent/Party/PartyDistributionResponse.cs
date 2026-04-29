using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Party;

public class PartyDistributionResponse : IPacketHandler
{
    public ushort Opcode => 0x3068;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(PartyDistributionResponse), packet);
    }
}
