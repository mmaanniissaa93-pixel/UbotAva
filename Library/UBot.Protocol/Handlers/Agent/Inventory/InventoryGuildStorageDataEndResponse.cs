using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Inventory;

public class InventoryGuildStorageDataEndResponse : IPacketHandler
{
    public ushort Opcode => 0x3254;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(InventoryGuildStorageDataEndResponse), packet);
    }
}
