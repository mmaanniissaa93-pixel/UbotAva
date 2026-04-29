using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Event;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Party;

internal class PartyAutoRefuseResponse 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0xB067;

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
        if (CoreGame.Party.HasPendingRequest)
            CoreGame.AcceptanceRequest = null;

        EventManager.FireEvent("OnPartyRequestRefused");
    }
}





