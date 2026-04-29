using UBot.Core.Network;
using UBot.Protocol;
namespace UBot.Protocol.Handlers.Agent.Cos;
public class CosUpdateResponse : IPacketHandler
{
    public ushort Opcode => 0x30C9;
    public PacketDestination Destination => PacketDestination.Client;
    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(CosUpdateResponse), packet);
    }
}
