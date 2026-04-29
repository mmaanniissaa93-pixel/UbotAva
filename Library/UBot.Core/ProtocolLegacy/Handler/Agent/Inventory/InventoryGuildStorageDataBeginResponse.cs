using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Objects.Inventory;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Inventory;

internal class InventoryGuildStorageDataBeginResponse 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x3253;

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
        CoreGame.ChunkedPacket = new Packet(0);
        CoreGame.Player.GuildStorage = new Storage();
        CoreGame.Player.GuildStorage.Gold = packet.ReadULong();
    }
}





