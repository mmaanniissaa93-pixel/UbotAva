using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Job;

public class JobJoinResponse : IPacketHandler
{
    public ushort Opcode => 0xB0E1;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(JobJoinResponse), packet);
    }
}
