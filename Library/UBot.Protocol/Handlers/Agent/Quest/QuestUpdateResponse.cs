using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Quests;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Quest;

public class QuestUpdateResponse : IPacketHandler
{
    public ushort Opcode => 0x30D5;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var player = ProtocolRuntime.GameState?.Player as Player;
        if (player == null)
            return;

        var type = (QuestUpdateType)packet.ReadByte();
        var questId = packet.ReadUInt();
        dynamic quest = ProtocolRuntime.GameState?.GetReference("RefQuest", questId);

        if (quest == null)
        {
            ProtocolRuntime.Feedback?.Warn($"QuestLog with id {questId} not found!");
            return;
        }

        if (type == QuestUpdateType.Abandon)
        {
            ProtocolRuntime.Feedback?.Notify($"Abandon quest [{quest.GetTranslatedName()}]");

            if (player.QuestLog.ActiveQuests.TryGetValue(questId, out var playerQuest))
                playerQuest.Status = QuestStatus.Cancelled;
        }

        if (type == QuestUpdateType.Remove)
        {
            ProtocolRuntime.Feedback?.Notify($"Remove quest [{quest.GetTranslatedName()}");
            player.QuestLog.ActiveQuests.Remove(questId);
        }

        if (type == QuestUpdateType.Add)
        {
            var activeQuest = packet.ReadActiveQuest(questId);
            player.QuestLog.ActiveQuests.TryAdd(questId, activeQuest);
            ProtocolRuntime.Feedback?.Notify($"Added quest [{activeQuest.Quest.GetTranslatedName()}");
        }

        if (type == QuestUpdateType.Update)
        {
            var activeQuest = packet.ReadActiveQuest(questId);
            player.QuestLog.ActiveQuests[questId] = activeQuest;
            ProtocolRuntime.Feedback?.Debug($"Updated quest [{activeQuest.Quest.GetTranslatedName()}");
        }

        ProtocolRuntime.EventBus?.Fire("OnUpdateQuests");
    }
}
