using UBot.Core.Network;
using UBot.Protocol;
namespace UBot.Protocol.Commands.Agent.Action;
public class ActionTalkRequest : IPacketHandler
{
    public ushort Opcode => 0x7046;
    public PacketDestination Destination => PacketDestination.Server;
    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(ActionTalkRequest), packet);
    }
}
