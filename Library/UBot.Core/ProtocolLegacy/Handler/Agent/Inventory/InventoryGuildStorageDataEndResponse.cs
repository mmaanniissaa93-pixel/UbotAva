using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Inventory;

internal class InventoryGuildStorageDataEndResponse 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x3254;

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
        if (CoreGame.ChunkedPacket == null)
            return;

        packet = CoreGame.ChunkedPacket;
        packet.Lock();

        var storage = CoreGame.Player.GuildStorage;
        storage.Deserialize(packet);

        EventManager.FireEvent("OnGuildStorageData");

        Log.Notify($"Found {storage.Count} item(s) in guild storage.");

        CoreGame.ChunkedPacket = null;
    }
}





