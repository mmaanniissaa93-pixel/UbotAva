using UBot.Core.Objects.Job;
using UBot.Protocol;

namespace UBot.Core.Network.Handler.Agent.Job;

public class JobCosStuckResponse : IPacketHandler
{
    public ushort Opcode => 0x30E7;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var reason = (TransportStuckReason)packet.ReadByte();
        ProtocolRuntime.GameState?.FireEvent("OnJobCosStuck", reason);
        ProtocolRuntime.GameState?.LogNotify("[Job] Your transport is stuck!");
    }
}
