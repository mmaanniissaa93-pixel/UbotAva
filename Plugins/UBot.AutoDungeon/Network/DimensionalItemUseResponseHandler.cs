using UBot.Core.Network;

namespace UBot.AutoDungeon.Network;

internal class DimensionalItemUseResponseHandler : IPacketHandler
{
    public ushort Opcode => 0xB04C;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var success = packet.ReadByte() == 0x01;
        AutoDungeonState.OnDimensionalItemUseResponse(success);
    }
}
