using System.Collections.Generic;
using UBot.Core;

namespace UBot.Core.Abstractions;

public interface IReferenceManager
{
    int LanguageTab { get; }
    GameClientType ClientType { get; }

    void EnsureTextDataLoaded();
    void EnsureCharacterDataLoaded();
    void EnsureItemDataLoaded();
    void EnsureSkillDataLoaded();
    void EnsureLevelDataLoaded();
    void EnsureQuestDataLoaded();
    void EnsureTeleportDataLoaded();
    void EnsureShopDataLoaded();
    void EnsureOptLevelDataLoaded();

    string GetTranslation(string code);

    object GetRefObjChar(uint id);
    object GetRefItem(string codeName);
    object GetQuestReward(uint id);
    IEnumerable<object> GetQuestRewardItems(uint id);
}

public interface IReference
{
    uint ID { get; }
    string CodeName { get; }
    string GetName();
    string GetRealName(bool displayRarity = false);
}

public static class ReferenceProvider
{
    private static IReferenceManager _instance;

    public static IReferenceManager Instance
    {
        get => _instance;
        set => _instance = value;
    }

    public static void Clear() => _instance = null;
}
