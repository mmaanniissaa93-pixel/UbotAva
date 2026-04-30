using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Entity;

public class PlayerUpdatePointsResponse : IPacketHandler
{
    public ushort Opcode => 0x304E;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var player = UBot.Protocol.ProtocolRuntime.GameState?.Player as Player;
        if (player == null)
            return;

        var type = packet.ReadByte();
        switch (type)
        {
            case 1:
                player.Gold = packet.ReadULong();
                UBot.Protocol.ProtocolRuntime.EventBus?.Fire("OnUpdateGold");
                break;

            case 2:
                player.SkillPoints = packet.ReadUInt();
                UBot.Protocol.ProtocolRuntime.EventBus?.Fire("OnUpdateSP");
                break;

            case 4:
                player.BerzerkPoints = packet.ReadByte();
                UBot.Protocol.ProtocolRuntime.EventBus?.Fire("OnUpdateBerzerkerPoints");
                break;
        }
    }
}
