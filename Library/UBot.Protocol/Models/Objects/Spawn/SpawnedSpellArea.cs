using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;
using UBot.Core.Network;
using UBot.Protocol.Legacy;
using UBot.Core;

namespace UBot.Core.Objects.Spawn;

public class SpawnedSpellArea : SpawnedEntity
{
    /// <summary>
    ///     Gets or sets the skill identifier.
    /// </summary>
    public uint SkillId;

    /// <summary>
    ///     Gets or sets the record.
    /// </summary>
    /// <value>
    ///     The record.
    /// </value>
    public new RefSkill Record => LegacyGame.ReferenceManager.GetRefSkill(SkillId);

    /// <summary>
    ///     Froms the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    /// <returns></returns>
    public static SpawnedSpellArea FromPacket(Packet packet)
    {
        //UNK0
        if (LegacyGame.ClientType >= GameClientType.Chinese)
            packet.ReadUInt();
        else
            packet.ReadUShort();

        var spellArea = new SpawnedSpellArea { SkillId = packet.ReadUInt(), UniqueId = packet.ReadUInt() };

        spellArea.Movement.Source = packet.ReadPosition();

        return spellArea;
    }
}
