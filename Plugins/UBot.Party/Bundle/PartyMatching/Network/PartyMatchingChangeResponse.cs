using UBot.Core;
using UBot.Core.Event;
using UBot.Core.Extensions;
using UBot.Core.Network;
using UBot.Core.Objects.Party;

namespace UBot.Party.Bundle.PartyMatching.Network;

internal class PartyMatchingChangeResponse : IPacketHandler
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>The opcode.</value>
    public ushort Opcode => 0xB06A;

    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>The destination.</value>
    public PacketDestination Destination => PacketDestination.Client;

    /// <summary>
    ///     Handles the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    public void Invoke(Packet packet)
    {
        if (packet.ReadByte() != 0x01)
            return;

        Container.PartyMatching.Id = packet.ReadUInt();
        packet.ReadUInt();

        UBot.Core.RuntimeAccess.Session.Party.Settings = PartySettings.FromType(packet.ReadByte());
        Container.PartyMatching.Config.Purpose = (PartyPurpose)packet.ReadByte();
        Container.PartyMatching.Config.LevelFrom = packet.ReadByte();
        Container.PartyMatching.Config.LevelTo = packet.ReadByte();
        Container.PartyMatching.Config.Title = packet.ReadConditonalString();

        UBot.Core.RuntimeAccess.Events.FireEvent("OnChangePartyEntry");
    }
}
