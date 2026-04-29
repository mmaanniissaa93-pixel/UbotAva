using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Commands.Agent.Inventory;

public class InventoryOperationRequest : IPacketHandler
{
    public ushort Opcode => 0xB034;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(InventoryOperationRequest), packet);
    }
}
