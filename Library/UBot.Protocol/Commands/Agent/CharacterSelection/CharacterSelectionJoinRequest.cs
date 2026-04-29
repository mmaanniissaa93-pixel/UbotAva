using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Commands.Agent.CharacterSelection;

public class CharacterSelectionJoinRequest : IPacketHandler
{
    public ushort Opcode => 0x7001;

    public PacketDestination Destination => PacketDestination.Server;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(CharacterSelectionJoinRequest), packet);
    }
}
