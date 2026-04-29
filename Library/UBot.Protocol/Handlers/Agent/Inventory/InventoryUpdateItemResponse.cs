using System.Collections.Generic;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Item;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Inventory;

public class InventoryUpdateItemResponse : IPacketHandler
{
    public ushort Opcode => 0x3040;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var player = ProtocolRuntime.GameState?.Player as Player;
        if (player == null)
            return;

        var sourceSlot = packet.ReadByte();
        var itemUpdateFlag = (ItemUpdateFlag)packet.ReadByte();
        var item = player.Inventory.GetItemAt(sourceSlot);

        if (item == null)
            return;

        if (itemUpdateFlag.HasFlag(ItemUpdateFlag.RefObjID))
            item.ItemId = packet.ReadUInt();

        if (itemUpdateFlag.HasFlag(ItemUpdateFlag.OptLevel))
            item.OptLevel = packet.ReadByte();

        if (itemUpdateFlag.HasFlag(ItemUpdateFlag.Variance))
            item.Attributes = new ItemAttributesInfo(packet.ReadULong());

        if (itemUpdateFlag.HasFlag(ItemUpdateFlag.Quanity))
            item.Amount = packet.ReadUShort();

        if (itemUpdateFlag.HasFlag(ItemUpdateFlag.Durability))
            item.Durability = packet.ReadUInt();

        if (itemUpdateFlag.HasFlag(ItemUpdateFlag.State))
            item.State = (InventoryItemState)packet.ReadByte();

        if (itemUpdateFlag.HasFlag(ItemUpdateFlag.MagParams))
        {
            item.MagicOptions = new List<MagicOptionInfo>();
            var magParamCount = packet.ReadByte();

            for (var i = 0; i < magParamCount; i++)
                item.MagicOptions.Add(packet.ReadMagicOptionInfo());
        }

        ProtocolRuntime.EventBus?.Fire("OnUpdateInventoryItem", sourceSlot);
    }
}
