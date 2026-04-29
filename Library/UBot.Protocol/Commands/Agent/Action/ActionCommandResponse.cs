using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Commands.Agent.Action;

public class ActionCommandResponse : IPacketHandler
{
    public ushort Opcode => 0xB071;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(ActionCommandResponse), packet);
    }
}
