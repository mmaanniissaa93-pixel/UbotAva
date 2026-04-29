using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Job;

public class JobUpdateExperienceResponse : IPacketHandler
{
    public ushort Opcode => 0x30E6;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(JobUpdateExperienceResponse), packet);
    }
}
