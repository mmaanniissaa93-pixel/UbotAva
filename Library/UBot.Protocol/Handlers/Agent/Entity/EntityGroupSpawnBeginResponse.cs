using UBot.Protocol;

namespace UBot.Core.Network.Handler.Agent.Entity;

public class EntityGroupSpawnBeginResponse : IPacketHandler
{
    public ushort Opcode => 0x3017;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.SpawnController?.BeginGroup(packet.ReadByte(), packet.ReadUShort());
    }
}
