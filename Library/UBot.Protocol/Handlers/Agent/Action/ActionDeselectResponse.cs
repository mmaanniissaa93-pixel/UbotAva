using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Action;

public class ActionDeselectResponse : IPacketHandler
{
    public ushort Opcode => 0xB04B;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var player = UBot.Protocol.ProtocolRuntime.GameState?.Player as Player;
        if (player == null || packet.ReadByte() != 1)
            return;

        if (player.State.DialogState is { IsInDialog: true } && player.State.DialogState.RequestedCloseNpcId != 0)
            player.State.DialogState = null;

        UBot.Protocol.ProtocolRuntime.EventBus?.Fire("OnDeselectEntity");
    }
}
