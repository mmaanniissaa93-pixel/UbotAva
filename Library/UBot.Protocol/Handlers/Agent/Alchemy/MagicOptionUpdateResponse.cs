using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Alchemy;

public class MagicOptionUpdateResponse : IPacketHandler
{
    public ushort Opcode => 0x34AA;
    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var result = packet.ReadByte();

        if (result == 2)
        {
            var errorCode = packet.ReadUShort();

            UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnMagicOptionUpdateError", errorCode);
        }

        var unkByte = packet.ReadByte(); //planned counter?
        if (unkByte != 0)
        {
            var slot = packet.ReadByte();

            var oldItem = CoreGame.Player.Inventory.GetItemAt(slot);
            var item = packet.ReadInventoryItem(slot);

            UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnMagicOptionUpdate", oldItem, item);
        }
    }
}






