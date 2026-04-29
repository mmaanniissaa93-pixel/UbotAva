using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Protocol;

namespace UBot.Protocol.Commands.Agent.Action;

public class ActionDeselectRequest : IPacketHandler
{
    public ushort Opcode => 0x704B;

    public PacketDestination Destination => PacketDestination.Server;

    public void Invoke(Packet packet)
    {
        var player = ProtocolRuntime.GameState?.Player as Player;
        if (player == null)
            return;

        var entityId = packet.ReadUInt();

        if (
            player.State.DialogState is { IsInDialog: true }
            && player.State.DialogState.Npc != null
            && player.State.DialogState.Npc.UniqueId == entityId
        )
            player.State.DialogState.RequestedCloseNpcId = entityId;
    }
}

