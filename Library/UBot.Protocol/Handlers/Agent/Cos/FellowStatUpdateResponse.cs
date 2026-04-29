using UBot.Core.Network;
using UBot.Protocol;
namespace UBot.Protocol.Handlers.Agent.Cos;
public class FellowStatUpdateResponse : IPacketHandler
{
    public ushort Opcode => 0x3422;
    public PacketDestination Destination => PacketDestination.Client;
    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(FellowStatUpdateResponse), packet);
    }
}
