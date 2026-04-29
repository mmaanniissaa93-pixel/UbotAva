using UBot.Core.Network;
using UBot.Protocol;
namespace UBot.Protocol.Commands.Agent.Alchemy;
public class StoneFuseRequestHandler : IPacketHandler
{
    public ushort Opcode => 0x7151;
    public PacketDestination Destination => PacketDestination.Server;
    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(StoneFuseRequestHandler), packet);
    }
}
