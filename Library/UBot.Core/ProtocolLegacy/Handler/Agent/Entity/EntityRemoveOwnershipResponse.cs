using UBot.Core.Network;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Objects.Spawn;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Entity;

internal class EntityRemoveOwnershipResponse 
{
    /// <summary>
    ///     Invokes the specified packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    public void Invoke(Packet packet)
    {
        var itemUniqueId = packet.ReadUInt();
        if (!SpawnManager.TryGetEntity<SpawnedItem>(itemUniqueId, out var entity))
            return;

        entity.HasOwner = false;
        //entity.OwnerJID = 0;

        EventManager.FireEvent("OnRemoveItemOwnership", itemUniqueId);
    }

    #region Properites

    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x304D;

    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Client;

    #endregion Properites
}

