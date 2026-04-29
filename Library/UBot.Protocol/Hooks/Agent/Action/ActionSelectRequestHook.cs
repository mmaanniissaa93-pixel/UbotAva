using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Hooks.Agent.Action;

public class ActionSelectRequestHook : IPacketHook
{
    public ushort Opcode => 0x7045;

    public PacketDestination Destination => PacketDestination.Server;

    public Packet ReplacePacket(Packet packet)
    {
        if (ProtocolRuntime.LegacyHandler == null)
            return packet;

        return (Packet)ProtocolRuntime.LegacyHandler.ReplacePacket(nameof(ActionSelectRequestHook), packet);
    }
}
