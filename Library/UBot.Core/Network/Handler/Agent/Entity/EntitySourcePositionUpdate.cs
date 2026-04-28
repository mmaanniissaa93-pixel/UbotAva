using UBot.Core.Components;
using UBot.Core.Objects;
using UBot.Core.Objects.Spawn;

namespace UBot.Core.Network.Handler.Agent.Entity;

internal class EntitySourcePositionUpdate : IPacketHandler
{
    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Client;

    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x3028;

    /// <summary>
    ///     Invoke the packet handler
    /// </summary>
    /// <param name="packet"></param>
    public void Invoke(Packet packet)
    {
        var player = Game.Player;
        if (player == null)
            return;

        var position = packet.ReadPosition();
        var uniqueId = packet.ReadUInt();

        if (uniqueId == player.UniqueId)
        {
            player.SetSource(position);
            return;
        }

        if (!SpawnManager.TryGetEntity<SpawnedEntity>(uniqueId, out var entity))
            return;

        entity.SetSource(position);
    }
}
