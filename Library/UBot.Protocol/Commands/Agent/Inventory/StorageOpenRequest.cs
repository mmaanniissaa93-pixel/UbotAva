using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Commands.Agent.Inventory;

public class StorageOpenRequest : IPacketHandler
{
    public ushort Opcode => 0x703C;

    public PacketDestination Destination => PacketDestination.Server;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(StorageOpenRequest), packet);
    }
}
