using CoreKernel = UBot.Protocol.Legacy.LegacyKernel;
using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using Game = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Protocol.Legacy;
namespace UBot.Protocol.Hooks.Agent.Cos;

public class CosActionRequestHook : IPacketHook 
{
    /// <summary>
    ///     Gets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x705C;

    /// <summary>
    ///     Gets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Server;

    /// <summary>
    ///     Replaces the packet and returns a new packet.
    /// </summary>
    /// <param name="packet"></param>
    /// <returns></returns>
    public Packet ReplacePacket(Packet packet)
    {
        var uniqueId = packet.ReadUInt();
        var type = packet.ReadByte();

        if (
            CoreKernel.Bot.Running
            && CoreGame.Player.HasActiveAbilityPet
            && CoreGame.Player.AbilityPet.UniqueId == uniqueId
            && type == 0x08
        )
            return null;

        return packet;
    }
}





