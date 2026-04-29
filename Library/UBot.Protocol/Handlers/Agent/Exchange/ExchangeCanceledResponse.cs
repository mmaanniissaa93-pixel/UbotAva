using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Exchange;

public class ExchangeCanceledResponse : IPacketHandler
{
    public ushort Opcode => 0x3088;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(ExchangeCanceledResponse), packet);
    }
}
