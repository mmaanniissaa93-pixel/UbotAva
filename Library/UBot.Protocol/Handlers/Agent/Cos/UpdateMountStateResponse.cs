using UBot.Core.Network;
using UBot.Protocol;
namespace UBot.Protocol.Handlers.Agent.Cos;
public class UpdateMountStateResponse : IPacketHandler
{
    public ushort Opcode => 0xB0CB;
    public PacketDestination Destination => PacketDestination.Client;
    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(UpdateMountStateResponse), packet);
    }
}
