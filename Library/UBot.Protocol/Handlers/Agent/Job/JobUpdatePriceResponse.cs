using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Job;

public class JobUpdatePriceResponse : IPacketHandler
{
    public ushort Opcode => 0x30E0;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(JobUpdatePriceResponse), packet);
    }
}
