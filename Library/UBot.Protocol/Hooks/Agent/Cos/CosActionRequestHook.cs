using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Hooks.Agent.Cos;

public class CosActionRequestHook : IPacketHook
{
    public ushort Opcode => 0x705C;

    public PacketDestination Destination => PacketDestination.Server;

    public Packet ReplacePacket(Packet packet)
    {
        if (ProtocolRuntime.LegacyHandler == null)
            return packet;

        return (Packet)ProtocolRuntime.LegacyHandler.ReplacePacket(nameof(CosActionRequestHook), packet);
    }
}
