using UBot.Core.Network;

namespace UBot.PacketInspector;

internal class WildcardServerHook : IPacketHook
{
    public ushort Opcode => 0x0000;
    public PacketDestination Destination => PacketDestination.Server;

    public Packet ReplacePacket(Packet packet)
    {
        PacketCaptureStore.Capture(packet, Destination);
        return packet;
    }
}
