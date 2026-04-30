using System.Collections.Generic;
using System.Linq;
using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;
using UBot.Core.Network;
using UBot.Protocol.Legacy;
using UBot.Core;

namespace UBot.Core.Objects.Spawn;

public sealed class SpawnedPortal : SpawnedBionic
{
    /// <summary>
    ///     Gets the cached teleport links
    /// </summary>
    public List<RefTeleportLink> Links;

    /// <summary>
    ///     Gets or sets the name of the owner.
    /// </summary>
    /// <value>
    ///     The name of the owner.
    /// </value>
    public string OwnerName;

    /// <summary>
    ///     Gets or sets the owner unique identifier.
    /// </summary>
    /// <value>
    ///     The owner unique identifier.
    /// </value>
    public uint OwnerUniqueId;

    /// <summary>
    ///     <inheritdoc />
    /// </summary>
    /// <param name="objId">The ref obj id</param>
    public SpawnedPortal(uint objId)
        : base(objId) { }

    /// <summary>
    ///     Froms the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    /// <param name="characterId">The character identifier.</param>
    /// <returns></returns>
    public static SpawnedPortal FromPacket(Packet packet, uint characterId)
    {
        var result = new SpawnedPortal(characterId);
        result.UniqueId = packet.ReadUInt();
        result.Movement.Source = packet.ReadPosition();

        var teleportObj = LegacyGame.ReferenceManager.GetRefObjChar(result.Id);
        if (teleportObj != null)
        {
            LegacyGame.ReferenceManager.EnsureTeleportDataLoaded();
            var teleportData = (IEnumerable<RefTeleport>)LegacyGame.ReferenceManager.TeleportData;
            result.Links = teleportData.FirstOrDefault(t => t.AssocRefObjId == teleportObj.ID)?.GetLinks();
        }

        if (LegacyGame.ClientType < GameClientType.Vietnam)
            return result;

        packet.ReadByte(); //unkByte0
        var unkByte1 = packet.ReadByte();
        packet.ReadByte(); //unkByte2
        var unkByte3 = packet.ReadByte();

        if (unkByte3 == 1)
        {
            //Regular portal
            packet.ReadUInt(); //unkUINT0
            packet.ReadUInt(); //unkUINT1
        }
        else if (unkByte3 == 6)
        {
            //Dimension hole
            result.OwnerName = packet.ReadString();
            result.OwnerUniqueId = packet.ReadUInt();
        }

        if (unkByte1 == 1)
        {
            packet.ReadUInt(); //unkUInt3
            packet.ReadByte(); //unkByte5
        }

        return result;
    }
}
