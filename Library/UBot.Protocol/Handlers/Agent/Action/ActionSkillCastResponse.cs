using UBot.Core.Network;
using UBot.Protocol;
namespace UBot.Protocol.Handlers.Agent.Action;
public class ActionSkillCastResponse : IPacketHandler
{
    public ushort Opcode => 0xB070;
    public PacketDestination Destination => PacketDestination.Client;
    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(ActionSkillCastResponse), packet);
    }
}
