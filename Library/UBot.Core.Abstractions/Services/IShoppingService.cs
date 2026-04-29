using System.Collections.Generic;

namespace UBot.Core.Abstractions.Services;

public interface IShoppingService
{
    bool Finished { get; }
    bool Enabled { get; set; }
    bool RepairGear { get; set; }
    List<string> SellFilter { get; set; }
    List<string> StoreFilter { get; set; }
    bool Running { get; set; }
    bool SellPetItems { get; set; }
    bool StorePetItems { get; set; }

    void Run(string npcCodeName);
    void SellItem(object item, object cos = null);
    void PurchaseItem(int tab, int slot, ushort amount);
    void PurchaseItem(object transport, int tab, int slot, ushort amount);
    void ReceiveSupplies(string npcCodeName);
    uint GetQuestId(string npcCodeName);
    void ReceiveQuestReward(string npcCodeName, uint questId, uint rewardId);
    void RepairItems(string npcCodeName);
    void StoreItems(string npcCodeName);
    void SortItems(string npcCodeName);
    void CloseShop();
    void CloseGuildShop();
    void CloseGuildStorage(uint uniqueId);
    void SelectNPC(string npcCodeName);
    void LoadFilters();
    void SaveFilters();
    void ChooseTalkOption(string npcCodeName, object option);
    void Stop();
}
