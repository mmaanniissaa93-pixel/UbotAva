using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Protocol.Legacy;
namespace UBot.Protocol.Handlers.Agent.Character;

public class CharacterDataBeginResponse : IPacketHandler
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x34A5;

    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Client;

    /// <summary>
    ///     Handles the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    public void Invoke(Packet packet)
    {
        CoreGame.Player?.StopMoving();
        CoreGame.ChunkedPacket = new Packet(0);

        if (CoreGame.Clientless)
            CoreGame.Ready = false;
    }
}






