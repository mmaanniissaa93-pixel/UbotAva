using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Event;
using UBot.Core.Objects;
using UBot.Core.Objects.Party;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Party;

internal class PartyUpdateResponse 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x3864;

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
        PartyUpdateResponseCommon(packet);
    }

    public static void PartyUpdateResponseCommon(Packet packet)
    {
        var type = (PartyUpdateType)packet.ReadByte();

        switch (type)
        {
            case PartyUpdateType.Dismissed:
                CoreGame.Party.Clear();
                EventManager.FireEvent("OnPartyDismiss");
                break;

            case PartyUpdateType.Joined:
                var memberJoined = packet.ReadPartyMember();
                CoreGame.Party.Members?.Add(memberJoined);
                EventManager.FireEvent("OnPartyMemberJoin", memberJoined);
                break;

            case PartyUpdateType.Leave:
                var memberLeft = CoreGame.Party.GetMemberById(packet.ReadUInt());
                CoreGame.Party.Members.Remove(memberLeft);
                /*
                    0x03 => ????
                 */
                if (packet.ReadByte() == 0x04)
                {
                    EventManager.FireEvent("OnPartyMemberBanned", memberLeft);
                }
                else if (memberLeft.Name == CoreGame.Player.Name)
                {
                    EventManager.FireEvent("OnPartyDismiss");
                    CoreGame.Party.Clear();
                }
                else
                {
                    EventManager.FireEvent("OnPartyMemberLeave", memberLeft);
                }

                break;

            case PartyUpdateType.Member:
                var memberId = packet.ReadUInt();
                var member = CoreGame.Party.GetMemberById(memberId);
                var memberUpdateType = (PartyMemberUpdateType)packet.ReadByte();

                switch (memberUpdateType)
                {
                    case PartyMemberUpdateType.NameRefObjID:
                        member.Name = packet.ReadString();
                        member.ObjectId = packet.ReadUInt();
                        break;

                    case PartyMemberUpdateType.HPMP:
                        member.HealthMana = packet.ReadByte(); //0-A|0-A -> 0%-100%|0%-100%
                        break;

                    case PartyMemberUpdateType.Mastery:
                        member.MasteryId1 = packet.ReadUInt();
                        member.MasteryId2 = packet.ReadUInt();
                        break;

                    case PartyMemberUpdateType.Level:
                        member.Level = packet.ReadByte();
                        break;

                    case PartyMemberUpdateType.Position:

                        member.Position = packet.ReadPositionConditional();

                        break;

                    case PartyMemberUpdateType.Guild:
                        member.Guild = packet.ReadString();
                        break;
                }

                EventManager.FireEvent("OnPartyMemberUpdate", member);
                break;

            case PartyUpdateType.Leader:
                CoreGame.Party.Leader = CoreGame.Party.GetMemberById(packet.ReadUInt());
                EventManager.FireEvent("OnPartyLeaderChange");
                break;

            case PartyUpdateType.LeaderChange:
                CoreGame.Party.Leader = CoreGame.Party.GetMemberById(packet.ReadUInt());
                EventManager.FireEvent("OnPartyLeaderChange");
                break;

            default:
                Log.Debug($"Unknow party type:{type} opcode: {packet.Opcode}");
                break;
        }
    }
}





