using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Alchemy;

internal class MagicOptionUpdateResponse
{
    public ushort Opcode => 0x34AA;
    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var result = packet.ReadByte();

        if (result == 2)
        {
            var errorCode = packet.ReadUShort();

            EventManager.FireEvent("OnMagicOptionUpdateError", errorCode);
        }

        var unkByte = packet.ReadByte(); //planned counter?
        if (unkByte != 0)
        {
            var slot = packet.ReadByte();

            var oldItem = CoreGame.Player.Inventory.GetItemAt(slot);
            var item = packet.ReadInventoryItem(slot);

            EventManager.FireEvent("OnMagicOptionUpdate", oldItem, item);
        }
    }
}






