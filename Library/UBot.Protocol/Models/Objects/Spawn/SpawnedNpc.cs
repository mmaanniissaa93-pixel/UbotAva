using UBot.Core.Network;
using UBot.Protocol.Legacy;

namespace UBot.Core.Objects.Spawn;

public class SpawnedNpc : SpawnedBionic
{
    /// <summary>
    ///     <inheritdoc />
    /// </summary>
    /// <param name="objId">The ref obj id</param>
    public SpawnedNpc(uint objId)
        : base(objId) { }

    /// <summary>
    ///     Gets or sets the npc talk.
    /// </summary>
    public NpcTalk Talk { get; } = new();

    /// <summary>
    ///     Froms the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    /// <param name="bionic">The bionic.</param>
    /// <returns></returns>
    public virtual void Deserialize(Packet packet)
    {
        Talk.Deserialize(packet);
    }
}
