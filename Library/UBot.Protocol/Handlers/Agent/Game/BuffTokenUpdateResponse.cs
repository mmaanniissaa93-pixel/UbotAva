using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Game;

public class BuffTokenUpdateResponse : IPacketHandler
{
    public ushort Opcode => 0x3077;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(BuffTokenUpdateResponse), packet);
    }
}
