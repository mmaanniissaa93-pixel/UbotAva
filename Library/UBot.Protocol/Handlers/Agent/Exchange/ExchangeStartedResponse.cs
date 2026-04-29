using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Exchange;

public class ExchangeStartedResponse : IPacketHandler
{
    public ushort Opcode => 0x3085;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(ExchangeStartedResponse), packet);
    }
}
