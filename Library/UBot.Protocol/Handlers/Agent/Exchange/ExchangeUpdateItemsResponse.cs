using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Exchange;

public class ExchangeUpdateItemsResponse : IPacketHandler
{
    public ushort Opcode => 0x308C;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(ExchangeUpdateItemsResponse), packet);
    }
}
