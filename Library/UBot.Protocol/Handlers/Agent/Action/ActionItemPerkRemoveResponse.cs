using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Action;

public class ActionItemPerkRemoveResponse : IPacketHandler
{
    public ushort Opcode => 0x3261;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(ActionItemPerkRemoveResponse), packet);
    }
}
