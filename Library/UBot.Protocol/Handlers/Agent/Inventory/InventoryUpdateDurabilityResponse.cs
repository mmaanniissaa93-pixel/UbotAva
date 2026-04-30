using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Inventory;

public class InventoryUpdateDurabilityResponse : IPacketHandler
{
    public ushort Opcode => 0x3052;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var slot = packet.ReadByte();
        var durability = packet.ReadUInt();
        dynamic player = UBot.Protocol.ProtocolRuntime.GameState?.Player;
        var item = player?.Inventory.GetItemAt(slot);

        if (item == null)
            return;

        item.Durability = durability;

        UBot.Protocol.ProtocolRuntime.GameState?.FireEvent("OnUpdateItemDurability", slot, durability);
    }
}

