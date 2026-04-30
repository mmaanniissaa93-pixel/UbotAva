using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Spawn;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Entity;

public class EntityUpdateMovementResponse : IPacketHandler 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0xB021;

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
        var player = CoreGame.Player;
        if (player == null)
            return;

        var uniqueId = packet.ReadUInt();

        var movement = packet.ReadMotionMovement();
        if (uniqueId == player.UniqueId)
        {
            // Set source from movement
            if (movement.HasSource)
                player.SetSource(movement.Source);

            if (movement.HasAngle)
            {
                // Movement through angle
                player.Move(movement.Angle);
                UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnPlayerMoveAngle");

                return;
            }

            // Movement through click
            player.Move(movement.Destination);
            UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnPlayerMove");

            return;
        }

        if (!SpawnManager.TryGetEntity<SpawnedEntity>(uniqueId, out var entity))
            return;

        // Set source from movement
        if (movement.HasSource)
            entity.SetSource(movement.Source);

        if (movement.HasAngle)
        {
            // Movement through angle
            entity.Move(movement.Angle);
            UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnEntityMoveAngle", uniqueId);

            return;
        }

        if (player.Vehicle?.UniqueId == uniqueId)
            UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnVehicleMove");
        else
            UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnEntityMove", uniqueId);

        // Movement through click
        entity.Move(movement.Destination);
    }
}





