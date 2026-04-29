using UBot.Protocol;

namespace UBot.Core.Network.Handler.Agent.Inventory;

public class InventoryUpdateSizeResponse : IPacketHandler
{
    public ushort Opcode => 0x3092;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var type = packet.ReadByte();
        var size = packet.ReadByte();
        dynamic player = ProtocolRuntime.GameState?.Player;
        if (player == null)
            return;

        switch (type)
        {
            case 1:
                player.Inventory.Capacity = size;
                ProtocolRuntime.GameState?.LogDebug($"Inventory size has been updated to [{size}] slots");
                ProtocolRuntime.GameState?.FireEvent("OnUpdateInventorySize");
                break;

            case 2:
                if (player.Storage != null)
                    player.Storage.Capacity = size;
                ProtocolRuntime.GameState?.LogDebug($"Storage size has been updated to [{size}] slots");
                ProtocolRuntime.GameState?.FireEvent("OnUpdateStorageSize");
                break;

            default:
                ProtocolRuntime.GameState?.LogDebug($"InventorySizeUpdateResponse: Unknown update type [{type}] ({size})");
                break;
        }
    }
}
