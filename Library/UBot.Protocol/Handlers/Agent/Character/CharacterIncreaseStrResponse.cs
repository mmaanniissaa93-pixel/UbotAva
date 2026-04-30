using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Character;

public class CharacterIncreaseStrResponse : IPacketHandler
{
    public ushort Opcode => 0xB050;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        if (packet.ReadByte() != 1)
            return;

        dynamic player = UBot.Protocol.ProtocolRuntime.GameState?.Player;
        if (player == null)
            return;

        player.StatPoints--;
        UBot.Protocol.ProtocolRuntime.GameState?.FireEvent("OnIncreaseStrength");
    }
}

