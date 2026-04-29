using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Event;
using UBot.Core.Objects;
using UBot.Core.Objects.Party;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Party;

internal class PartyInviteResponse 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x3080;

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
        CoreGame.AcceptanceRequest = AcceptanceRequest.FromPacket(packet);

        switch (CoreGame.AcceptanceRequest.Type)
        {
            case InviteRequestType.Party1:
            case InviteRequestType.Party2:

                CoreGame.AcceptanceRequest.Settings = PartySettings.FromType(packet.ReadByte());

                if (CoreGame.Party.HasPendingRequest)
                    EventManager.FireEvent("OnPartyRequest");

                break;

            case InviteRequestType.Resurrection1:
            case InviteRequestType.Resurrection2:
                EventManager.FireEvent("OnResurrectionRequest");
                break;
        }
    }
}





