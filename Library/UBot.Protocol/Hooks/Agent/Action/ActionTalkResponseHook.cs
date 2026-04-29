using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Hooks.Agent.Action;

public class ActionTalkResponseHook : IPacketHook
{
    public ushort Opcode => 0xB046;

    public PacketDestination Destination => PacketDestination.Client;

    public Packet ReplacePacket(Packet packet)
    {
        if (ProtocolRuntime.LegacyHandler == null)
            return packet;

        return (Packet)ProtocolRuntime.LegacyHandler.ReplacePacket(nameof(ActionTalkResponseHook), packet);
    }
}
