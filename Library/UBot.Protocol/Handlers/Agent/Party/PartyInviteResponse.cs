using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Party;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Party;

public class PartyInviteResponse : IPacketHandler 
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
                    UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnPartyRequest");

                break;

            case InviteRequestType.Resurrection1:
            case InviteRequestType.Resurrection2:
                UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnResurrectionRequest");
                break;
        }
    }
}





