using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;
using UBot.Protocol.Extensions;
using UBot.Core.Network;
using UBot.Protocol.Legacy;

namespace UBot.Core.Objects.Spawn;

public class SpawnedPlayerStall
{
    /// <summary>
    ///     Gets or sets the name.
    /// </summary>
    /// <value>
    ///     The name.
    /// </value>
    public string Name { get; set; }

    /// <summary>
    ///     Gets or sets the decoration identifier.
    /// </summary>
    /// <value>
    ///     The decoration identifier.
    /// </value>
    public uint DecorationId { get; set; }

    /// <summary>
    ///     Gets the decoration.
    /// </summary>
    /// <value>
    ///     The decoration.
    /// </value>
    public RefObjItem Decoration => LegacyGame.ReferenceManager.GetRefItem(DecorationId);

    /// <summary>
    ///     Froms the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    /// <returns></returns>
    public static SpawnedPlayerStall FromPacket(Packet packet)
    {
        return new SpawnedPlayerStall { Name = packet.ReadConditonalString(), DecorationId = packet.ReadUInt() };
    }
}
