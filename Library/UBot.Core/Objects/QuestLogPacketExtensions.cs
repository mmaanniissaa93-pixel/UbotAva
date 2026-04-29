using System.Collections.Generic;
using UBot.Core.Network;
using UBot.Core.Objects.Quests;

namespace UBot.Core.Objects;

internal static class QuestLogPacketExtensions
{
    internal static QuestLog ReadQuestLog(this Packet packet)
    {
        var result = new QuestLog();
        result.ParseCompletedQuests(packet);
        result.ParseActiveQuests(packet);
        return result;
    }

    internal static void ParseCompletedQuests(this QuestLog questLog, Packet packet)
    {
        var count = packet.ReadUShort();
        questLog.CompletedQuests = new uint[count];

        for (var i = 0; i < count; i++)
            questLog.CompletedQuests[i] = packet.ReadUInt();
    }

    internal static void ParseActiveQuests(this QuestLog questLog, Packet packet)
    {
        var count = packet.ReadByte();
        questLog.ActiveQuests = new Dictionary<uint, ActiveQuest>(count);

        for (var i = 0; i < count; i++)
        {
            var questId = packet.ReadUInt();
            var activeQuest = packet.ReadActiveQuest(questId);
            questLog.ActiveQuests.TryAdd(activeQuest.Id, activeQuest);
        }
    }

    internal static void AbandonQuest(this QuestLog questLog, uint questId)
    {
        if (!questLog.ActiveQuests.ContainsKey(questId))
            return;

        var packet = new Packet(0x70D9);
        packet.WriteUInt(questId);

        var callback = new AwaitCallback(null, 0xB0D9);
        PacketManager.SendPacket(packet, PacketDestination.Server, callback);
        callback.AwaitResponse();
    }

    internal static ActiveQuest ReadActiveQuest(this Packet packet, uint questId)
    {
        var activeQuest = new ActiveQuest { Id = questId };

        if (Game.ClientType == GameClientType.Vietnam274)
        {
            activeQuest.AchievementAmount = packet.ReadUShort();
            activeQuest.RequiredShareParty = packet.ReadUShort();
        }
        else
        {
            activeQuest.AchievementAmount = packet.ReadByte();
            activeQuest.RequiredShareParty = packet.ReadByte();
        }

        if (Game.ClientType >= GameClientType.Chinese)
        {
            activeQuest.Unknown1 = packet.ReadByte();
            activeQuest.Unknown2 = packet.ReadByte();
        }

        activeQuest.Type = (QuestType)packet.ReadByte();

        if ((activeQuest.Type & QuestType.Time) == QuestType.Time)
            activeQuest.RemainingTime = packet.ReadInt();

        if ((activeQuest.Type & QuestType.Status) == QuestType.Status)
            activeQuest.Status = (QuestStatus)packet.ReadByte();

        if ((activeQuest.Type & QuestType.Objective) == QuestType.Objective)
        {
            var objectiveCount = packet.ReadByte();
            activeQuest.Objectives = new QuestObjective[objectiveCount];

            for (var i = 0; i < objectiveCount; i++)
            {
                var objective = new QuestObjective
                {
                    Id = packet.ReadByte(),
                    InProgress = packet.ReadBool(),
                    NameStrId = packet.ReadString(),
                    Tasks = packet.ReadUIntArray(packet.ReadByte()),
                };

                activeQuest.Objectives[i] = objective;
            }
        }

        if ((activeQuest.Type & QuestType.RefObjects) == QuestType.RefObjects)
            activeQuest.Npcs = packet.ReadUIntArray(packet.ReadByte());

        return activeQuest;
    }
}
