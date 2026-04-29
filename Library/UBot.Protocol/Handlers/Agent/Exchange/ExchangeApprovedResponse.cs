using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Exchange;

public class ExchangeApprovedResponse : IPacketHandler
{
    public ushort Opcode => 0x3087;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(ExchangeApprovedResponse), packet);
    }
}
