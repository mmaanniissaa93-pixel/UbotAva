using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Party;

public class PartyUpdateResponseNew : IPacketHandler
{
    public ushort Opcode => 0x3E6E;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(PartyUpdateResponseNew), packet);
    }
}
