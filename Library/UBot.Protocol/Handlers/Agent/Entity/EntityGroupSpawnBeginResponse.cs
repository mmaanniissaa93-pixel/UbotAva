using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Entity;

public class EntityGroupSpawnBeginResponse : IPacketHandler
{
    public ushort Opcode => 0x3017;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        UBot.Protocol.ProtocolRuntime.SpawnController?.BeginGroup(packet.ReadByte(), packet.ReadUShort());
    }
}

