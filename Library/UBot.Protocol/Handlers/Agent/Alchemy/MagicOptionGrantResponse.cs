using UBot.Core.Network;
using UBot.Protocol;
namespace UBot.Protocol.Handlers.Agent.Alchemy;
public class MagicOptionGrantResponse : IPacketHandler
{
    public ushort Opcode => 0x34A9;
    public PacketDestination Destination => PacketDestination.Client;
    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(MagicOptionGrantResponse), packet);
    }
}
