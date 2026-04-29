using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Job;

public class JobUpdateTradeScaleResponse : IPacketHandler
{
    public ushort Opcode => 0x30E8;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(JobUpdateTradeScaleResponse), packet);
    }
}
