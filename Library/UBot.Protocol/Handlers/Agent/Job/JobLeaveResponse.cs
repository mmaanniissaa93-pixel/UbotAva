using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Job;

public class JobLeaveResponse : IPacketHandler
{
    public ushort Opcode => 0xB0E2;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(JobLeaveResponse), packet);
    }
}
