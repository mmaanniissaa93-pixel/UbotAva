using System.Collections.Generic;

namespace UBot.Core.Abstractions.Services;

public interface IShoppingRuntime
{
    bool Clientless { get; }
    GameClientType ClientType { get; }
    object SelectedEntity { get; }

    string[] LoadSellFilter();
    string[] LoadStoreFilter();
    void SaveSellFilter(IEnumerable<string> values);
    void SaveStoreFilter(IEnumerable<string> values);

    object GetShopGroup(string npcCodeName);
    IEnumerable<object> GetShopGoods(object shopGroup);
    byte GetShopGoodTabIndex(string npcCodeName, object shopGood);
    object GetPackageItem(string packageItemCodeName);
    object GetRefItem(string itemCodeName);
    IEnumerable<object> GetEventRewardItems(uint questId);

    void SelectNPC(string npcCodeName);
    void SellItem(object item, object cos = null);
    void PurchaseItem(int tab, int slot, ushort amount);
    void PurchaseItem(object transport, int tab, int slot, ushort amount);
    uint GetQuestId(string npcCodeName);
    void ReceiveQuestReward(string npcCodeName, uint questId, uint rewardId);
    void RepairItems(string npcCodeName, bool repairGear);
    void StoreItem(object item, object npc);
    void OpenStorage(uint uniqueId);
    void OpenGuildStorage(uint uniqueId);
    void CloseShop();
    void CloseGuildShop();
    void CloseGuildStorage(uint uniqueId);
    void ChooseTalkOption(string npcCodeName, object option);

    object GetSelectedNpc();
    uint GetNpcUniqueId(object npc);
    string GetNpcCodeName(object npc);
    bool IsWarehouseNpc(object npc);
    bool HasPlayerStorage(bool guildStorage);
}
