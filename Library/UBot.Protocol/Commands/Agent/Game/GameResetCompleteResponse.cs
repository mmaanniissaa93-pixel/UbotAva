using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Commands.Agent.Game;

public class GameResetRequest : IPacketHandler
{
    public ushort Opcode => 0x34B5;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(GameResetRequest), packet);
    }
}
