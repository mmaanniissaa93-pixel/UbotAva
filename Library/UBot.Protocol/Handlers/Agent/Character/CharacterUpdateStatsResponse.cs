using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Character;

public class CharacterUpdateStatsResponse : IPacketHandler
{
    public ushort Opcode => 0x303D;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        dynamic player = UBot.Protocol.ProtocolRuntime.GameState?.Player;
        if (player == null)
            return;

        player.PhysicalAttackMin = packet.ReadUInt();
        player.PhysicalAttackMax = packet.ReadUInt();
        player.MagicalAttackMin = packet.ReadUInt();
        player.MagicalAttackMax = packet.ReadUInt();

        player.PhysicalDefence = packet.ReadUShort();
        player.MagicalDefence = packet.ReadUShort();

        player.HitRate = packet.ReadUShort();
        player.ParryRate = packet.ReadUShort();

        player.MaximumHealth = packet.ReadInt();
        player.MaximumMana = packet.ReadInt();

        player.Strength = packet.ReadUShort();
        player.Intelligence = packet.ReadUShort();

        UBot.Protocol.ProtocolRuntime.GameState?.FireEvent("OnLoadCharacterStats");
    }
}

