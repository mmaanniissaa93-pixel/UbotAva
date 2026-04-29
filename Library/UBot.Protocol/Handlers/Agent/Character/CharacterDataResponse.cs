using UBot.Core.Network;
using UBot.Protocol;
namespace UBot.Protocol.Handlers.Agent.Character;
public class CharacterDataResponse : IPacketHandler
{
    public ushort Opcode => 0x3013;
    public PacketDestination Destination => PacketDestination.Client;
    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(CharacterDataResponse), packet);
    }
}
