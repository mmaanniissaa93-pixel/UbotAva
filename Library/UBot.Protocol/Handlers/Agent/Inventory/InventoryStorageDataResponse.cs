using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Inventory;

public class InventoryStorageDataResponse : IPacketHandler
{
    public ushort Opcode => 0x3049;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(InventoryStorageDataResponse), packet);
    }
}
