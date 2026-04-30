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
        var player = UBot.Protocol.ProtocolRuntime.GameState?.Player as Player;
        if (player == null)
            return;

        var type = (QuestUpdateType)packet.ReadByte();
        var questId = packet.ReadUInt();
        dynamic quest = UBot.Protocol.ProtocolRuntime.GameState?.GetReference("RefQuest", questId);

        if (quest == null)
        {
            UBot.Protocol.ProtocolRuntime.Feedback?.Warn($"QuestLog with id {questId} not found!");
            return;
        }

        if (type == QuestUpdateType.Abandon)
        {
            UBot.Protocol.ProtocolRuntime.Feedback?.Notify($"Abandon quest [{quest.GetTranslatedName()}]");

            if (player.QuestLog.ActiveQuests.TryGetValue(questId, out var playerQuest))
                playerQuest.Status = QuestStatus.Cancelled;
        }

        if (type == QuestUpdateType.Remove)
        {
            UBot.Protocol.ProtocolRuntime.Feedback?.Notify($"Remove quest [{quest.GetTranslatedName()}");
            player.QuestLog.ActiveQuests.Remove(questId);
        }

        if (type == QuestUpdateType.Add)
        {
            var activeQuest = packet.ReadActiveQuest(questId);
            player.QuestLog.ActiveQuests.TryAdd(questId, activeQuest);
            UBot.Protocol.ProtocolRuntime.Feedback?.Notify($"Added quest [{activeQuest.Quest.GetTranslatedName()}");
        }

        if (type == QuestUpdateType.Update)
        {
            var activeQuest = packet.ReadActiveQuest(questId);
            player.QuestLog.ActiveQuests[questId] = activeQuest;
            UBot.Protocol.ProtocolRuntime.Feedback?.Debug($"Updated quest [{activeQuest.Quest.GetTranslatedName()}");
        }

        UBot.Protocol.ProtocolRuntime.EventBus?.Fire("OnUpdateQuests");
    }
}
