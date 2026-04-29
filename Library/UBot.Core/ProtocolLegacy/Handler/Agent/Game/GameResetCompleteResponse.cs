using CoreGame = global::UBot.Core.Game;
using UBot.Protocol;
using UBot.Core.Network;
using UBot.Core.Event;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Game;

internal class GameResetRequest 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x34B5;

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
        CoreGame.Ready = false;
        Log.Debug("Game client is loading...");

        if (CoreGame.Clientless)
        {
            Packet gameResetResponse = new Packet(0x34B6);
            ProtocolRuntime.Dispatch(gameResetResponse, PacketDestination.Server);
        }

        if (CoreGame.Player.Teleportation == null)
            return;

        CoreGame.Player.Teleportation.IsTeleporting = true;
        CoreGame.Player.State.DialogState = null;

        EventManager.FireEvent("OnTeleportStart");
    }
}





