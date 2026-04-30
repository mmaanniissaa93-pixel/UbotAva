using UBot.Core.Network;
using UBot.Protocol.Legacy;

namespace UBot.Core.Objects;

public static class InventoryItemCollectionPacketExtensions
{
    public static InventoryItemCollection ReadInventoryItemCollection(this Packet packet)
    {
        var collection = new InventoryItemCollection(0);
        collection.Deserialize(packet);
        return collection;
    }

    public static void Move(this InventoryItemCollection collection, Packet packet)
    {
        var sourceSlot = packet.ReadByte();
        var destinationSlot = packet.ReadByte();
        var amount = packet.ReadUShort();
        collection.Move(sourceSlot, destinationSlot, amount);
    }

    public static void MoveTo(this InventoryItemCollection collection, InventoryItemCollection inventory, Packet packet)
    {
        var sourceSlot = packet.ReadByte();
        var destinationSlot = packet.ReadByte();
        collection.MoveTo(inventory, sourceSlot, destinationSlot);
    }

    public static void Deserialize(this InventoryItemCollection collection, Packet packet)
    {
        collection.Capacity = packet.ReadByte();
        if (collection.Capacity <= 0)
            return;

        var amount = packet.ReadByte();
        for (var i = 0; i < amount; i++)
        {
            var item = packet.ReadInventoryItem();
            if (item != null)
                collection.Add(item);
        }
    }
}
