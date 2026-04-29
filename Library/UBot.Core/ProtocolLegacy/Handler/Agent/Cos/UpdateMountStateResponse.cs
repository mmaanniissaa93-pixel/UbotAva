using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Objects.Spawn;

namespace UBot.Core.ProtocolLegacy.Handler.Agent;

internal class UpdateMountStateResponse
{
    #region Methods

    public void Invoke(Packet packet)
    {
        if (packet.ReadByte() != 0x01)
            return;

        var ownerUniqueId = packet.ReadUInt();
        var isMounted = packet.ReadBool();
        var cosUniqueId = packet.ReadUInt();

        if (ownerUniqueId == CoreGame.Player.UniqueId)
        {
            if (!isMounted)
            {
                CoreGame.Player.Vehicle = null;
                CoreGame.Player.Transport = null;

                return;
            }

            if (cosUniqueId == CoreGame.Player.Transport?.UniqueId)
                CoreGame.Player.Vehicle = CoreGame.Player.Transport;

            if (cosUniqueId == CoreGame.Player.JobTransport?.UniqueId)
                CoreGame.Player.Vehicle = CoreGame.Player.JobTransport;

            // Vsro private servers uses the attack pet like pet2
            if (cosUniqueId == CoreGame.Player.Growth?.UniqueId)
                CoreGame.Player.Vehicle = CoreGame.Player.Growth;

            if (cosUniqueId == CoreGame.Player.Fellow?.UniqueId)
                CoreGame.Player.Vehicle = CoreGame.Player.Fellow;

            var bionicPosition = CoreGame.Player.Vehicle.Bionic.Position;
            CoreGame.Player.Vehicle.StopMoving(bionicPosition);
            CoreGame.Player.StopMoving(bionicPosition);
        }

        //Assertion: only player's are supported to have active vehicles. Think it's the same in the client.
        if (!SpawnManager.TryGetEntity<SpawnedPlayer>(ownerUniqueId, out var owner))
            return;

        if (!SpawnManager.TryGetEntity<SpawnedCos>(cosUniqueId, out var cos))
            return;

        owner.OnTransport = isMounted;
        owner.TransportUniqueId = cosUniqueId;

        EventManager.FireEvent("OnEntityMountTransport", owner, cos, isMounted);
    }

    #endregion Methods

    #region Properties

    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0xB0CB;

    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Client;

    #endregion Properties
}






