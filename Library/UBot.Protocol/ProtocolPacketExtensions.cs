using UBot.Core.Abstractions;
using UBot.Core.Network;
using UBot.Core.Objects.Item;
using UBot.Core.Objects.Quests;
using GameClientType = UBot.Core.GameClientType;
using UBot.Core;

namespace UBot.Protocol;

internal static class ProtocolPacketExtensions
{
    public static MagicOptionInfo ReadMagicOptionInfo(this Packet packet)
    {
        return new MagicOptionInfo { Id = packet.ReadUInt(), Value = packet.ReadUInt() };
    }

    public static ActiveQuest ReadActiveQuest(this Packet packet, uint questId)
    {
        var activeQuest = new ActiveQuest { Id = questId };
        var clientType = UBot.Protocol.ProtocolRuntime.GameState?.ClientType ?? GameClientType.Thailand;

        if (clientType == GameClientType.Vietnam274)
        {
            activeQuest.AchievementAmount = packet.ReadUShort();
            activeQuest.RequiredShareParty = packet.ReadUShort();
        }
        else
        {
            activeQuest.AchievementAmount = packet.ReadByte();
            activeQuest.RequiredShareParty = packet.ReadByte();
        }

        if (clientType >= GameClientType.Chinese)
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
                activeQuest.Objectives[i] = new QuestObjective
                {
                    Id = packet.ReadByte(),
                    InProgress = packet.ReadBool(),
                    NameStrId = packet.ReadString(),
                    Tasks = packet.ReadUIntArray(packet.ReadByte()),
                };
            }
        }

        if ((activeQuest.Type & QuestType.RefObjects) == QuestType.RefObjects)
            activeQuest.Npcs = packet.ReadUIntArray(packet.ReadByte());

        return activeQuest;
    }
}
