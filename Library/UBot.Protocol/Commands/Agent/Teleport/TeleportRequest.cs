using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using System.Collections.Generic;
using System.Linq;
using UBot.GameData.ReferenceObjects;
using UBot.Core.Objects;
using UBot.Core.Objects.Spawn;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Commands.Agent.Teleport;

public class TeleportRequest : IPacketHandler
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x705A;

    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Server;

    /// <summary>
    ///     Handles the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    public void Invoke(Packet packet)
    {
        var teleporterUniqueId = packet.ReadUInt();
        var teleportType = (TeleportType)packet.ReadByte();
        if (teleportType == TeleportType.Guide || teleportType == TeleportType.RUNTIME_PORTAL)
        {
            var operation = packet.ReadByte();
            return;
        }

        var destination = packet.ReadUInt();

        if (!SpawnManager.TryGetEntity<SpawnedBionic>(teleporterUniqueId, out var portal))
            return;

        CoreGame.ReferenceManager.EnsureTeleportDataLoaded();
        var teleportData = (IEnumerable<RefTeleport>)CoreGame.ReferenceManager.TeleportData;
        CoreGame.Player.Teleportation = new Teleportation
        {
            Destination = teleportData.FirstOrDefault(t => t.ID == destination),
        };

        UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnRequestTeleport", destination, portal.Record.CodeName);
    }
}






