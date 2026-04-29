using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
namespace UBot.Core.ProtocolLegacy.Handler.Agent.Inventory;

internal class InventoryGuildStorageDataResponse 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x3255;

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
        CoreGame.ChunkedPacket.WriteBytes(packet.GetBytes());
    }
}





