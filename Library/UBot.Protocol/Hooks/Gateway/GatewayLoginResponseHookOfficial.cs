using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Hooks.Gateway;

public class GatewayLoginResponseHookOfficial : IPacketHook
{
    public ushort Opcode => 0xA10A;

    public PacketDestination Destination => PacketDestination.Client;

    public Packet ReplacePacket(Packet packet)
    {
        if (ProtocolRuntime.LegacyHandler == null)
            return packet;

        return (Packet)ProtocolRuntime.LegacyHandler.ReplacePacket(nameof(GatewayLoginResponseHookOfficial), packet);
    }
}
