using UBot.Core.Network;
using UBot.Protocol;
namespace UBot.Protocol.Handlers.Agent.Cos;
public class CosDataResponse : IPacketHandler
{
    public ushort Opcode => 0x30C8;
    public PacketDestination Destination => PacketDestination.Client;
    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(CosDataResponse), packet);
    }
}
