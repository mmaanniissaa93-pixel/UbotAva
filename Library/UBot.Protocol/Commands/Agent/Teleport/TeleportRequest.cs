using UBot.Core.Network;
using UBot.Protocol;
namespace UBot.Protocol.Commands.Agent.Teleport;
public class TeleportRequest : IPacketHandler
{
    public ushort Opcode => 0x705A;
    public PacketDestination Destination => PacketDestination.Server;
    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(TeleportRequest), packet);
    }
}
