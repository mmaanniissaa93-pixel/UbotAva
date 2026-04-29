using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Action;

public class ActionCommandStateResponse : IPacketHandler
{
    public ushort Opcode => 0xB074;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var player = ProtocolRuntime.GameState?.Player as Player;
        if (player == null)
            return;

        packet.ReadByte();
        var recurring = packet.ReadByte();

        if (recurring == 0)
        {
            player.InAction = false;
            ProtocolRuntime.Feedback?.Debug("Player has exited in action!");
            ProtocolRuntime.EventBus?.Fire("OnPlayerExitAction");
        }
        else
        {
            player.InAction = true;
            ProtocolRuntime.Feedback?.Debug("Player has entered in action!");
            ProtocolRuntime.EventBus?.Fire("OnPlayerInAction");
        }
    }
}
