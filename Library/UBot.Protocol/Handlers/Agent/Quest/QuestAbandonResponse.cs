using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Quest;

public class QuestAbandonResponse : IPacketHandler
{
    public ushort Opcode => 0xB0D9;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var player = ProtocolRuntime.GameState?.Player as Player;

        if (player != null && packet.ReadByte() == 0x01)
        {
            var questId = packet.ReadUInt();

            if (player.QuestLog.ActiveQuests.TryGetValue(questId, out var playerQuest))
            {
                player.QuestLog.ActiveQuests.Remove(questId);
                ProtocolRuntime.Feedback?.Notify($"Abandoned quest [{playerQuest.Quest.GetTranslatedName()}]");
            }
        }

        ProtocolRuntime.EventBus?.Fire("OnUpdateQuests");
    }
}
