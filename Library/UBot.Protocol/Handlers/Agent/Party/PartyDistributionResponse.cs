using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using System.Collections.Generic;
using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;
using UBot.Core.Objects.Party;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Party
{
    public class PartyDistributionResponse : IPacketHandler 
    {
        public ushort Opcode => 0x3068;

        public PacketDestination Destination => PacketDestination.Client;

        public void Invoke(Packet packet)
        {
            uint partyMemberJID = packet.ReadUInt();

            var members = (List<PartyMember>)CoreGame.Party.Members;
            PartyMember member = members.Find(member => member.MemberId == partyMemberJID);

            if (member == null)
                return;

            uint itemId = packet.ReadUInt();
            RefObjItem item = CoreGame.ReferenceManager.GetRefItem(itemId);
            if (item.TypeID1 == 3)
            {
                //ITEM_
                if (item.TypeID2 == 1)
                {
                    //ITEM_CH_
                    //ITEM_EU_
                    //ITEM_AVATAR_
                    byte optLevel = packet.ReadByte();
                    Log.Notify($"Item [{item.GetRealName() ?? itemId.ToString()} (+{optLevel})] is distributed to [{member.Name}].");
                }
                else if (item.TypeID2 == 2)
                {
                    //ITEM_COS_
                    // No message triggered by server.
                }
                else if (item.TypeID2 == 3)
                {
                    //ITEM_ETC_
                    ushort quantity = packet.ReadUShort();
                    Log.Notify($"Item [{item.GetRealName() ?? itemId.ToString()} {quantity} pieces] is distributed to [{member.Name}].");
                }
            }
        }
    }
}





