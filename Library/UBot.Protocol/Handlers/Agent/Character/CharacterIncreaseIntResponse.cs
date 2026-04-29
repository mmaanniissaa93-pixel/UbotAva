using UBot.Protocol;

namespace UBot.Core.Network.Handler.Agent.Character;

public class CharacterIncreaseIntResponse : IPacketHandler
{
    public ushort Opcode => 0xB051;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        if (packet.ReadByte() != 1)
            return;

        dynamic player = ProtocolRuntime.GameState?.Player;
        if (player == null)
            return;

        player.StatPoints--;
        ProtocolRuntime.GameState?.FireEvent("OnIncreaseIntelligence");
    }
}
