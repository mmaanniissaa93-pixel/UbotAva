using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Hooks.Gateway;

public class GatewayLoginResponseHook : IPacketHook
{
    public ushort Opcode => 0xA102;

    public PacketDestination Destination => PacketDestination.Client;

    public Packet ReplacePacket(Packet packet)
    {
        if (ProtocolRuntime.LegacyHandler == null)
            return packet;

        return (Packet)ProtocolRuntime.LegacyHandler.ReplacePacket(nameof(GatewayLoginResponseHook), packet);
    }
}
