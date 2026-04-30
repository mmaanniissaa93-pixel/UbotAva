using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Spawn;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Entity;

public class EntitySourcePositionUpdate : IPacketHandler 
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
        var player = CoreGame.Player;
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





