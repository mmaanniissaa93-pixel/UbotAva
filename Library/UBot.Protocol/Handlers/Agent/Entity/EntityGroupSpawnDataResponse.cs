using UBot.Protocol;

namespace UBot.Core.Network.Handler.Agent.Entity;

public class EntityGroupSpawnDataResponse : IPacketHandler
{
    public ushort Opcode => 0x3019;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.SpawnController?.AppendGroupData(packet.GetBytes());
    }
}
