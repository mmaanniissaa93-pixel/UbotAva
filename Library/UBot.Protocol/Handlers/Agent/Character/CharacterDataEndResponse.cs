using UBot.Core.Network;
using UBot.Protocol;
namespace UBot.Protocol.Handlers.Agent.Character;
public class CharacterDataEndResponse : IPacketHandler
{
    public ushort Opcode => 0x34A6;
    public PacketDestination Destination => PacketDestination.Client;
    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(CharacterDataEndResponse), packet);
    }
}
