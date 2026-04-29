using UBot.Core.Network;
using UBot.Protocol;
namespace UBot.Protocol.Handlers.Agent.StorageBox;
public class StorageBoxTakeItemResponse : IPacketHandler
{
    public ushort Opcode => 0xB558;
    public PacketDestination Destination => PacketDestination.Client;
    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(StorageBoxTakeItemResponse), packet);
    }
}
