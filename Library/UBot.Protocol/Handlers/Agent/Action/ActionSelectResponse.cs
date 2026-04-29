using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Action;

public class ActionSelectResponse : IPacketHandler
{
    public ushort Opcode => 0xB045;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(ActionSelectResponse), packet);
    }
}
