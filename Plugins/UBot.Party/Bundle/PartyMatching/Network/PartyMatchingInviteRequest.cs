using System.Threading.Tasks;
using UBot.Core;
using UBot.Core.Network;

namespace UBot.Party.Bundle.PartyMatching.Network;

internal class PartyMatchingInviteRequest : IPacketHandler
{
    public ushort Opcode => 0x706D;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        if (Container.PartyMatching.Config.AutoAccept)
        {
            var requestID = packet.ReadUInt();
            var requestType = packet.ReadUInt();
            //var partyMatchingID = packet.ReadUInt();
            //var memberPrimaryMastery = packet.ReadUInt();
            //var memberSecondaryMastery = packet.ReadUInt();
            //var unkByte0 = packet.ReadByte();
            //var member = packet.ReadPartyMember();

            ushort opcode = 0x306E;
            if (UBot.Core.RuntimeAccess.Session.ClientType >= GameClientType.Chinese)
                opcode = 0x308D;

            var requestPacket = new Packet(opcode);
            requestPacket.WriteUInt(requestID);
            requestPacket.WriteUInt(requestType);
            requestPacket.WriteByte(1); //1 - accept, 2 - decline
            UBot.Core.RuntimeAccess.Packets.SendPacket(requestPacket, PacketDestination.Server);
        }

        if (Container.PartyMatching.Config.AutoReform)
            if (UBot.Core.RuntimeAccess.Session.Party != null && UBot.Core.RuntimeAccess.Session.Party.Members?.Count + 1 >= UBot.Core.RuntimeAccess.Session.Party.Settings.MaxMember)
                _ = Task.Run(() => Container.PartyMatching.RequestPartyList());
    }
}
