using UBot.Core.Network;
using UBot.Core.Event;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Alchemy;

internal class MagicOptionGrantResponse
{
    public ushort Opcode => 0x34A9;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var slot = packet.ReadByte();
        var attribute = packet.ReadString();

        EventManager.FireEvent("OnMagicOptionGranted", slot, attribute);
    }
}


