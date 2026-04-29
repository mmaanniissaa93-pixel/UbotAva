using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Inventory;

public class InventoryStorageDataBeginResponse : IPacketHandler
{
    public ushort Opcode => 0x3047;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(InventoryStorageDataBeginResponse), packet);
    }
}
