using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Action;

public class ActionItemPerkAddResponse : IPacketHandler
{
    public ushort Opcode => 0x325F;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(ActionItemPerkAddResponse), packet);
    }
}
