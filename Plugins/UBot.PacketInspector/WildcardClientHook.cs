using UBot.Core.Network;

namespace UBot.PacketInspector;

internal class WildcardClientHook : IPacketHook
{
    public ushort Opcode => 0x0000;
    public PacketDestination Destination => PacketDestination.Client;

    public Packet ReplacePacket(Packet packet)
    {
        PacketCaptureStore.Capture(packet, Destination);
        return packet;
    }
}
