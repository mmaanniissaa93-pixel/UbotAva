using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Objects.Spawn;

namespace UBot.Core.Network.Handler.Agent.Entity;

internal class EntityUpdateMoveSpeedResponse : IPacketHandler
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x30D0;

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
        var player = Game.Player;
        if (player == null)
            return;

        var uniqueId = packet.ReadUInt();
        var walkSpeed = packet.ReadFloat();
        var runSpeed = packet.ReadFloat();

        if (uniqueId == player.UniqueId || player.Vehicle?.UniqueId == uniqueId)
        {
            player.SetSpeed(walkSpeed, runSpeed);

            EventManager.FireEvent("OnUpdatePlayerSpeed");
        }
        else
        {
            if (!SpawnManager.TryGetEntity<SpawnedBionic>(uniqueId, out var bionic))
                return;

            bionic.SetSpeed(walkSpeed, runSpeed);

            EventManager.FireEvent("OnUpdateEntitySpeed", uniqueId);
        }
    }
}
