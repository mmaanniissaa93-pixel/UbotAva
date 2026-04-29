using UBot.Core.Network;
using UBot.Protocol;
namespace UBot.Protocol.Handlers.Agent.Alchemy;
public class MagicOptionUpdateResponse : IPacketHandler
{
    public ushort Opcode => 0x34AA;
    public PacketDestination Destination => PacketDestination.Client;
    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(MagicOptionUpdateResponse), packet);
    }
}
