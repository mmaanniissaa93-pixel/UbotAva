using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Spawn;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Entity;

public class EntityUpdatePositionResponse : IPacketHandler 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0xB023;

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
        var uniqueId = packet.ReadUInt();
        var position = packet.ReadPosition();
        if (uniqueId == CoreGame.Player.UniqueId)
        {
            CoreGame.Player.StopMoving(position);

            return;
        }

        if (!SpawnManager.TryGetEntity<SpawnedEntity>(uniqueId, out var entity))
            return;

        entity.StopMoving(position);
    }
}





