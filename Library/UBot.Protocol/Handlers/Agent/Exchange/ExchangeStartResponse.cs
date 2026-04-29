using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Exchange;

public class ExchangeStartResponse : IPacketHandler
{
    public ushort Opcode => 0xB081;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(ExchangeStartResponse), packet);
    }
}
