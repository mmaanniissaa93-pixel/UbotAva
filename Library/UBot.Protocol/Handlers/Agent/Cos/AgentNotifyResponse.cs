using UBot.Core.Network;
using UBot.Protocol;
namespace UBot.Protocol.Handlers.Agent.Cos;
public class AgentNotifyResponse : IPacketHandler
{
    public ushort Opcode => 0x300C;
    public PacketDestination Destination => PacketDestination.Client;
    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(AgentNotifyResponse), packet);
    }
}
