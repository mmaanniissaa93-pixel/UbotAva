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
        var player = UBot.Protocol.ProtocolRuntime.GameState?.Player as Player;
        if (player == null)
            return;

        packet.ReadByte();
        var recurring = packet.ReadByte();

        if (recurring == 0)
        {
            player.InAction = false;
            UBot.Protocol.ProtocolRuntime.Feedback?.Debug("Player has exited in action!");
            UBot.Protocol.ProtocolRuntime.EventBus?.Fire("OnPlayerExitAction");
        }
        else
        {
            player.InAction = true;
            UBot.Protocol.ProtocolRuntime.Feedback?.Debug("Player has entered in action!");
            UBot.Protocol.ProtocolRuntime.EventBus?.Fire("OnPlayerInAction");
        }
    }
}
