using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Core.Objects.Inventory;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Inventory;

public class InventoryStorageDataBeginResponse : IPacketHandler 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x3047;

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
        CoreGame.Player.Storage = CoreGame.Player.Storage ?? new Storage();
        CoreGame.Player.Storage.Gold = packet.ReadULong();
    }
}





