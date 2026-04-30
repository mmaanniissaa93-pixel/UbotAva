using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Abstractions;
using UBot.Core.Network;
using System.Collections.Generic;
using UBot.Core.Objects;
using UBot.Core.Objects.Party;
using UBot.Protocol.Legacy;
using UBot.Core;

namespace UBot.Protocol.Handlers.Agent.Party;

public class PartyCreateFromMatching : IPacketHandler 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x3065;

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
        CoreGame.Party = new UBot.Core.Objects.Party.Party { Members = new List<PartyMember>() };

        packet.ReadByte(); //FF

        if (CoreGame.ClientType > GameClientType.Thailand)
            packet.ReadUInt(); // partyId

        var leaderId = packet.ReadUInt();

        CoreGame.Party.Settings = PartySettings.FromType(packet.ReadByte());

        var memberAmount = packet.ReadByte();
        for (var iMember = 0; iMember < memberAmount; iMember++)
            CoreGame.Party.Members.Add(packet.ReadPartyMember());

        CoreGame.Party.Leader = CoreGame.Party.GetMemberById(leaderId);

        UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnPartyData");
    }
}





