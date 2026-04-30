using UBot.Core.Network;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Alchemy;

public class MagicOptionGrantResponse : IPacketHandler
{
    public ushort Opcode => 0x34A9;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var slot = packet.ReadByte();
        var attribute = packet.ReadString();

        UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnMagicOptionGranted", slot, attribute);
    }
}


