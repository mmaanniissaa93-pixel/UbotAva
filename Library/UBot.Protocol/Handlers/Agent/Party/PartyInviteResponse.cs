using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Party;

public class PartyInviteResponse : IPacketHandler
{
    public ushort Opcode => 0x3080;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(PartyInviteResponse), packet);
    }
}
