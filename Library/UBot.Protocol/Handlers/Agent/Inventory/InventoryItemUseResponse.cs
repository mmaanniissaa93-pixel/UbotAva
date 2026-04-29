using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Inventory;

public class InventoryItemUseResponse : IPacketHandler
{
    public ushort Opcode => 0xB04C;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var player = ProtocolRuntime.GameState?.Player as Player;
        if (player == null || packet.ReadByte() != 0x01)
            return;

        var sourceSlot = packet.ReadByte();
        var newAmount = packet.ReadUShort();

        player.Inventory.UpdateItemAmount(sourceSlot, newAmount);
        ProtocolRuntime.EventBus?.Fire("OnUseItem", sourceSlot);
    }
}
