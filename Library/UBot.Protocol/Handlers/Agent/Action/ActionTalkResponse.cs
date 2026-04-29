using UBot.Core.Network;
using UBot.Protocol;
namespace UBot.Protocol.Handlers.Agent.Action;
public class ActionTalkResponse : IPacketHandler
{
    public ushort Opcode => 0xB046;
    public PacketDestination Destination => PacketDestination.Client;
    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(ActionTalkResponse), packet);
    }
}
