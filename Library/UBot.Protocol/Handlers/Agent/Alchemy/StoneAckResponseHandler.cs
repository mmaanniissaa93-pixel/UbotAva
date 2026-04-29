using UBot.Core.Network;
using UBot.Protocol;
namespace UBot.Protocol.Handlers.Agent.Alchemy;
public class StoneAckResponseHandler : IPacketHandler
{
    public ushort Opcode => 0xB151;
    public PacketDestination Destination => PacketDestination.Client;
    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(StoneAckResponseHandler), packet);
    }
}
