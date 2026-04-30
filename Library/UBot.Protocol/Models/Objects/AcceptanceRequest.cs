using UBot.Core.Network;
using UBot.Core.Objects.Party;
using UBot.Core.Objects.Spawn;
using UBot.Protocol.Legacy;

namespace UBot.Core.Objects;

public class AcceptanceRequest
{
    public uint PlayerUniqueId;
    public PartySettings Settings;
    public InviteRequestType Type;
    public SpawnedPlayer Player => SpawnManager.GetEntity<SpawnedPlayer>(PlayerUniqueId);

    public void Accept()
    {
        var packet = new Packet(0x3080);
        packet.WriteByte(1);
        packet.WriteByte(1);

        PacketManager.SendPacket(packet, PacketDestination.Server);
    }

    public void Refuse()
    {
        var packet = new Packet(0x3080);

        switch (Type)
        {
            case InviteRequestType.Party1:
            case InviteRequestType.Party2:
                packet.WriteByte(2);
                packet.WriteUShort(11276);
                break;

            case InviteRequestType.Resurrection1:
            case InviteRequestType.Resurrection2:
                packet.WriteByte(1);
                packet.WriteByte(2);
                break;
        }

        PacketManager.SendPacket(packet, PacketDestination.Server);
    }

    public static AcceptanceRequest FromPacket(Packet packet)
    {
        return new AcceptanceRequest
        {
            Type = (InviteRequestType)packet.ReadByte(),
            PlayerUniqueId = packet.ReadUInt(),
        };
    }
}
