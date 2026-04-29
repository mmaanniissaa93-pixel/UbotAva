using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UBot.Core.Abstractions;
using UBot.Core.Abstractions.Services;
using UBot.Core.Objects;
using UBot.Core.Services;
using UBot.GameData.ReferenceObjects;

namespace UBot.Core.Components;

public static class ShoppingManager
{
    private static IShoppingService _service = new ShoppingService();

    public static Dictionary<RefShopGood, int> ShoppingList
    {
        get => ((ShoppingService)_service).ShoppingList;
        set => ((ShoppingService)_service).ShoppingList = value;
    }

    public static bool Finished => _service.Finished;

    public static bool Enabled
    {
        get => _service.Enabled;
        set => _service.Enabled = value;
    }

    public static bool RepairGear
    {
        get => _service.RepairGear;
        set => _service.RepairGear = value;
    }

    public static List<string> SellFilter
    {
        get => _service.SellFilter;
        set => _service.SellFilter = value;
    }

    public static List<string> StoreFilter
    {
        get => _service.StoreFilter;
        set => _service.StoreFilter = value;
    }

    public static bool Running
    {
        get => _service.Running;
        set => _service.Running = value;
    }

    public static bool SellPetItems
    {
        get => _service.SellPetItems;
        set => _service.SellPetItems = value;
    }

    public static bool StorePetItems
    {
        get => _service.StorePetItems;
        set => _service.StorePetItems = value;
    }

    public static Dictionary<byte, InventoryItem> BuybackList
    {
        get => ((ShoppingService)_service).BuybackList;
        set => ((ShoppingService)_service).BuybackList = value;
    }

    public static void Initialize()
    {
        Initialize(new ShoppingService());
    }

    public static void Initialize(IShoppingService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        ServiceRuntime.Shopping = _service;
        ServiceRuntime.Log?.Debug("Initialized [ShoppingManager]!");
    }

    public static void Run(string npcCodeName) => _service.Run(npcCodeName);
    public static void SellItem(InventoryItem item, object cos = null) => _service.SellItem(item, cos);
    public static void PurchaseItem(int tab, int slot, ushort amount) => _service.PurchaseItem(tab, slot, amount);
    public static void PurchaseItem(object transport, int tab, int slot, ushort amount) => _service.PurchaseItem(transport, tab, slot, amount);
    public static void ReceiveSupplies(string npcCodeName) => _service.ReceiveSupplies(npcCodeName);
    public static uint GetQuestId(string npcCodeName) => _service.GetQuestId(npcCodeName);
    public static void ReceiveQuestReward(string npcCodeName, uint questId, uint rewardId) => _service.ReceiveQuestReward(npcCodeName, questId, rewardId);
    public static void RepairItems(string npcCodeName) => _service.RepairItems(npcCodeName);
    public static void StoreItems(string npcCodeName) => _service.StoreItems(npcCodeName);
    public static void SortItems(string npcCodeName) => _service.SortItems(npcCodeName);
    public static void CloseShop() => _service.CloseShop();
    public static void CloseGuildShop() => _service.CloseGuildShop();
    public static void CloseGuildStorage(uint uniqueId) => _service.CloseGuildStorage(uniqueId);
    public static void SelectNPC(string npcCodeName) => _service.SelectNPC(npcCodeName);
    public static void LoadFilters() => _service.LoadFilters();
    public static void SaveFilters() => _service.SaveFilters();
    public static void ChooseTalkOption(string npcCodeName, TalkOption option) => _service.ChooseTalkOption(npcCodeName, option);
    public static void Stop() => _service.Stop();
}

public sealed class ShoppingService : IShoppingService
{
    public ShoppingService()
    {
        ShoppingList = new Dictionary<RefShopGood, int>();
        StoreFilter = new List<string>();
        SellFilter = new List<string>();
        BuybackList = new Dictionary<byte, InventoryItem>();
    }

    public Dictionary<RefShopGood, int> ShoppingList { get; set; }
    public Dictionary<byte, InventoryItem> BuybackList { get; set; }
    public bool Finished { get; private set; }
    public bool Enabled { get; set; }
    public bool RepairGear { get; set; }
    public List<string> SellFilter { get; set; }
    public List<string> StoreFilter { get; set; }
    public bool Running { get; set; }
    public bool SellPetItems { get; set; }
    public bool StorePetItems { get; set; }

    public void Run(string npcCodeName)
    {
        var shopping = ShoppingRuntime;
        var inventory = InventoryRuntime;
        if (!Enabled || shopping == null || inventory == null)
            return;

        Finished = false;
        Running = true;

        SelectNPC(npcCodeName);

        ServiceRuntime.Log?.Notify("Selling items");

        var tempItemSellList = inventory.GetPlayerNormalPartItems(item =>
            item is InventoryItem inventoryItem && SellFilter.Any(p => p == inventoryItem.Record.CodeName)
        ).OfType<InventoryItem>().ToList();
        foreach (var item in tempItemSellList)
            SellItem(item);

        if (inventory.PlayerHasActiveAbilityPet && SellPetItems)
        {
            tempItemSellList = inventory.GetAbilityPetItems(item =>
                item is InventoryItem inventoryItem && SellFilter.Any(p => p == inventoryItem.Record.CodeName)
            ).OfType<InventoryItem>().ToList();

            foreach (var item in tempItemSellList)
            {
                var playerSlot = inventory.MoveAbilityPetItemToPlayer(item.Slot);
                if (playerSlot != 0xFF)
                    SellItem(inventory.GetPlayerInventoryItemAt(playerSlot));
            }
        }

        var shopGroup = shopping.GetShopGroup(npcCodeName);
        if (shopGroup == null)
        {
            ServiceRuntime.Log?.Warn("Could not buy anything from this NPC - It's not a shop!");
            CloseShop();
            Finished = true;
            Running = false;
            return;
        }

        var shopGoods = shopping.GetShopGoods(shopGroup).OfType<RefShopGood>().ToList();

        foreach (var item in ShoppingList)
        {
            if (!Running)
                return;

            var actualItem = shopGoods.FirstOrDefault(x => x.RefPackageItemCodeName == item.Key.RefPackageItemCodeName);
            if (actualItem == null)
                continue;

            var tabIndex = shopping.GetShopGoodTabIndex(npcCodeName, actualItem);
            if (tabIndex == 0xFF)
                continue;

            var refPackageItem = shopping.GetPackageItem(item.Key.RefPackageItemCodeName) as RefPackageItem;
            if (refPackageItem == null)
                continue;

            var holdingAmount = inventory.GetPlayerInventorySumAmount(refPackageItem.RefItemCodeName);
            var totalAmountToBuy = item.Value - holdingAmount;

            var refItem = shopping.GetRefItem(refPackageItem.RefItemCodeName) as RefObjItem;
            if (refItem == null)
                continue;

            ServiceRuntime.Log?.Notify("Buying items");

            while (totalAmountToBuy > 0 && !inventory.PlayerInventoryFull)
            {
                var amountStep = totalAmountToBuy;
                if (totalAmountToBuy >= refItem.MaxStack)
                    amountStep = refItem.MaxStack;

                PurchaseItem(tabIndex, actualItem.SlotIndex, (ushort)amountStep);
                totalAmountToBuy -= amountStep;
                Thread.Sleep(500);
            }

            MergePlayerStacks(refPackageItem.RefItemCodeName, refItem.MaxStack);
        }

        CloseShop();

        Finished = true;
        Running = false;
    }

    public void SellItem(object item, object cos = null)
    {
        if (item is InventoryItem inventoryItem)
            ShoppingRuntime?.SellItem(inventoryItem, cos);
    }

    public void PurchaseItem(int tab, int slot, ushort amount) => ShoppingRuntime?.PurchaseItem(tab, slot, amount);

    public void PurchaseItem(object transport, int tab, int slot, ushort amount)
    {
        ShoppingRuntime?.PurchaseItem(transport, tab, slot, amount);
    }

    public void ReceiveSupplies(string npcCodeName)
    {
        var shopping = ShoppingRuntime;
        var inventory = InventoryRuntime;
        if (shopping == null || inventory == null)
            return;

        Finished = false;
        Running = true;

        var questId = GetQuestId(npcCodeName);
        CloseShop();

        var currentWeapon = inventory.CurrentWeapon as InventoryItem;
        var excludedItemCodeNames = new List<string>();

        if (currentWeapon?.Record?.TypeID4 == 6)
            excludedItemCodeNames.Add("ITEM_ETC_LEVEL_BOLT");
        else if (currentWeapon?.Record?.TypeID4 == 12)
            excludedItemCodeNames.Add("ITEM_ETC_LEVEL_ARROW");
        else
            excludedItemCodeNames.AddRange(["ITEM_ETC_LEVEL_ARROW", "ITEM_ETC_LEVEL_BOLT"]);

        var items = shopping.GetEventRewardItems(questId).OfType<RefEventRewardItems>()
            .Where(r =>
                inventory.PlayerLevel >= r.MinRequiredLevel
                && inventory.PlayerLevel <= r.MaxRequiredLevel
                && !excludedItemCodeNames.Contains(r.ItemCodeName)
            );

        foreach (var item in items)
        {
            if (item.Item != null)
                ReceiveQuestReward(npcCodeName, questId, item.Item.ID);
        }

        Finished = true;
        Running = false;
    }

    public uint GetQuestId(string npcCodeName) => ShoppingRuntime?.GetQuestId(npcCodeName) ?? 0;

    public void ReceiveQuestReward(string npcCodeName, uint questId, uint rewardId)
    {
        ShoppingRuntime?.ReceiveQuestReward(npcCodeName, questId, rewardId);
    }

    public void RepairItems(string npcCodeName) => ShoppingRuntime?.RepairItems(npcCodeName, RepairGear);

    public void StoreItems(string npcCodeName)
    {
        var shopping = ShoppingRuntime;
        var inventory = InventoryRuntime;
        if (shopping == null || inventory == null)
            return;

        var firstSlot = GetInventoryFirstStoreSlot(shopping.ClientType);
        var tempInventory = inventory.GetPlayerInventoryItems(item =>
            item is InventoryItem inventoryItem
            && inventoryItem.Slot >= firstSlot
            && StoreFilter.Any(p => p == inventoryItem.Record.CodeName)
        ).OfType<InventoryItem>().ToList();

        SelectNPC(npcCodeName);
        var npc = shopping.GetSelectedNpc();
        if (npc == null)
        {
            ServiceRuntime.Log?.Debug("Cannot store items because there is no storage NPC selected!");
            return;
        }

        var guildStorage = !shopping.IsWarehouseNpc(npc);
        if (!guildStorage)
        {
            shopping.OpenStorage(shopping.GetNpcUniqueId(npc));
            if (!shopping.HasPlayerStorage(false))
                return;
        }
        else
        {
            shopping.OpenGuildStorage(shopping.GetNpcUniqueId(npc));
            if (!shopping.HasPlayerStorage(true))
                return;
        }

        ServiceRuntime.Log?.Notify("Storing items");
        foreach (var item in tempInventory)
            shopping.StoreItem(item, npc);

        if (inventory.PlayerHasActiveAbilityPet && StorePetItems)
        {
            var petItemStoreList = inventory.GetAbilityPetItems(item =>
                item is InventoryItem inventoryItem && StoreFilter.Any(p => p == inventoryItem.Record.CodeName)
            ).OfType<InventoryItem>().ToList();
            foreach (var item in petItemStoreList)
            {
                var playerSlot = inventory.MoveAbilityPetItemToPlayer(item.Slot);
                if (playerSlot != 0xFF)
                {
                    var movedItem = inventory.GetPlayerInventoryItemAt(playerSlot);
                    shopping.StoreItem(movedItem, npc);
                }
            }
        }

        if (guildStorage)
            CloseGuildStorage(shopping.GetNpcUniqueId(npc));

        if (shopping.Clientless || !guildStorage)
            CloseShop();
        else
            CloseGuildShop();
    }

    public void SortItems(string npcCodeName)
    {
        var shopping = ShoppingRuntime;
        var inventory = InventoryRuntime;
        if (shopping == null || inventory == null)
            return;

        SelectNPC(npcCodeName);
        var npc = shopping.GetSelectedNpc();
        if (npc == null)
        {
            ServiceRuntime.Log?.Debug("Cannot sort items because there is no storage NPC selected!");
            return;
        }

        var guildStorage = !shopping.IsWarehouseNpc(npc);
        if (!guildStorage)
            shopping.OpenStorage(shopping.GetNpcUniqueId(npc));
        else
            shopping.OpenGuildStorage(shopping.GetNpcUniqueId(npc));

        var allStorageItems = inventory.GetStorageItems(guildStorage, item => true).OfType<InventoryItem>().ToList();
        if (allStorageItems == null || allStorageItems.Count == 0)
        {
            CloseStorageView(shopping, npc, guildStorage);
            return;
        }

        var minSlot = allStorageItems.Min(i => i.Slot);
        var maxSlot = allStorageItems.Max(i => i.Slot);

        for (var i = minSlot; i <= maxSlot; i++)
        {
            var targetSlot = (byte)i;
            var remaining = inventory
                .GetStorageItems(guildStorage, it => it is InventoryItem item && item.Slot >= targetSlot)
                .OfType<InventoryItem>()
                .OrderBy(it => it.ItemId)
                .ThenBy(it => it.Slot)
                .ToList();

            if (remaining.Count == 0)
                continue;

            var groupKey = remaining[0].Record.CodeName;
            var candidateSlots = remaining
                .Where(it => it.Record.CodeName == groupKey)
                .Select(it => it.Slot)
                .OrderBy(s => s)
                .ToList();

            if (candidateSlots.Count == 0)
                break;

            var fromSlot = candidateSlots[0];
            if (fromSlot == targetSlot)
                continue;

            ServiceRuntime.Log?.Debug($"[ShoppingManager] Reordering storage: moving slot {fromSlot} to slot {targetSlot}");

            var itemToMove = inventory.GetStorageItemAt(guildStorage, fromSlot) as InventoryItem;
            if (itemToMove != null)
            {
                inventory.MoveStorageItem(guildStorage, fromSlot, targetSlot, (ushort)itemToMove.Amount, npc);
                Thread.Sleep(500);
            }
        }

        CloseStorageView(shopping, npc, guildStorage);
    }

    public void CloseShop()
    {
        Running = false;
        ShoppingRuntime?.CloseShop();
    }

    public void CloseGuildShop()
    {
        Running = false;
        ShoppingRuntime?.CloseGuildShop();
    }

    public void CloseGuildStorage(uint uniqueId) => ShoppingRuntime?.CloseGuildStorage(uniqueId);

    public void SelectNPC(string npcCodeName) => ShoppingRuntime?.SelectNPC(npcCodeName);

    public void LoadFilters()
    {
        SellFilter.Clear();
        StoreFilter.Clear();

        foreach (var item in ShoppingRuntime?.LoadSellFilter().Where(i => !string.IsNullOrWhiteSpace(i)).Distinct() ?? [])
            SellFilter.Add(item);

        foreach (var item in ShoppingRuntime?.LoadStoreFilter().Where(i => !string.IsNullOrWhiteSpace(i)).Distinct() ?? [])
            StoreFilter.Add(item);
    }

    public void SaveFilters()
    {
        ShoppingRuntime?.SaveSellFilter(SellFilter);
        ShoppingRuntime?.SaveStoreFilter(StoreFilter);
    }

    public void ChooseTalkOption(string npcCodeName, object option) => ShoppingRuntime?.ChooseTalkOption(npcCodeName, option);

    public void Stop()
    {
        Running = false;
        Finished = true;
    }

    private static IShoppingRuntime ShoppingRuntime => ServiceRuntime.ShoppingRuntime;

    private static IInventoryRuntime InventoryRuntime => ServiceRuntime.InventoryRuntime;

    private static int GetInventoryFirstStoreSlot(GameClientType clientType)
    {
        return clientType is GameClientType.Global
            or GameClientType.Korean
            or GameClientType.VTC_Game
            or GameClientType.RuSro
            or GameClientType.Turkey
            or GameClientType.Taiwan
            or GameClientType.Japanese
            ? 17
            : 13;
    }

    private static void MergePlayerStacks(string refItemCodeName, int maxStack)
    {
        if (maxStack <= 1 || InventoryRuntime == null)
            return;

        IList<InventoryItem> GetItems()
        {
            return InventoryRuntime.GetPlayerInventoryItems(i =>
                i is InventoryItem item && item.Record.CodeName == refItemCodeName && item.Amount < maxStack
            ).OfType<InventoryItem>().ToList();
        }

        var nonFullStacks = GetItems();
        while (nonFullStacks.Count >= 2)
        {
            InventoryRuntime.MovePlayerInventoryItem(
                nonFullStacks[1].Slot,
                nonFullStacks[0].Slot,
                (ushort)Math.Min(maxStack - nonFullStacks[0].Amount, nonFullStacks[1].Amount)
            );
            nonFullStacks = GetItems();
            Thread.Sleep(500);
        }
    }

    private static void CloseStorageView(IShoppingRuntime shopping, object npc, bool guildStorage)
    {
        if (guildStorage)
            shopping.CloseGuildStorage(shopping.GetNpcUniqueId(npc));

        if (shopping.Clientless || !guildStorage)
            shopping.CloseShop();
        else
            shopping.CloseGuildShop();
    }
}
