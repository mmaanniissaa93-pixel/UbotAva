using UBot.Core.Network;
using UBot.Protocol;
namespace UBot.Protocol.Handlers.Agent.Alchemy;
public class ElixirAckResponseHandler : IPacketHandler
{
    public ushort Opcode => 0xB150;
    public PacketDestination Destination => PacketDestination.Client;
    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(ElixirAckResponseHandler), packet);
    }
}
