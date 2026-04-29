using System.Collections.Generic;

namespace UBot.Core.Objects.Quests;

public class QuestLog
{
    public Dictionary<uint, ActiveQuest> ActiveQuests;
    public uint[] CompletedQuests;
}
