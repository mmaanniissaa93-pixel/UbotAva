using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UBot.FileSystem;
using UBot.NavMeshApi;
using UBot.NavMeshApi.Dungeon;
using UBot.NavMeshApi.Edges;
using UBot.NavMeshApi.Extensions;
using UBot.NavMeshApi.Terrain;
using UBot.Core.Client.ReferenceObjects;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Extensions;
using UBot.Core.Network;
using UBot.Core.Network.Protocol;
using UBot.Core.Objects;
using UBot.Core.Objects.Party;
using UBot.Core.Objects.Spawn;
using UBot.Core.Objects.Skill;
using UBot.Core.Plugins;
using Forms = System.Windows.Forms;
using CoreRegion = UBot.Core.Objects.Region;
using static UBot.Avalonia.Services.UbotPluginConfigHelpers;


namespace UBot.Avalonia.Services;

internal sealed class UbotItemsPluginService : UbotServiceBase
{
    private sealed class ItemsShoppingTarget
    {
        public string ShopCodeName { get; set; } = string.Empty;
        public string ItemCodeName { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    private static Dictionary<string, object?> BuildItemsPluginConfig()
    {
        var config = LoadPluginJsonConfig(ItemsPluginName);

        var shoppingEnabled = PlayerConfig.Get("UBot.Shopping.Enabled", true);
        var repairGear = PlayerConfig.Get("UBot.Shopping.RepairGear", true);
        var sellPetItems = PlayerConfig.Get("UBot.Shopping.SellPet", true);
        var storePetItems = PlayerConfig.Get("UBot.Shopping.StorePet", true);

        ShoppingManager.Enabled = shoppingEnabled;
        ShoppingManager.RepairGear = repairGear;
        ShoppingManager.SellPetItems = sellPetItems;
        ShoppingManager.StorePetItems = storePetItems;
        ShoppingManager.SellFilter ??= new List<string>();
        ShoppingManager.StoreFilter ??= new List<string>();
        ShoppingManager.ShoppingList ??= new Dictionary<RefShopGood, int>();
        ShoppingManager.LoadFilters();
        PickupManager.LoadFilter();

        config["shoppingEnabled"] = shoppingEnabled;
        config["repairGear"] = repairGear;
        config["sellPetItems"] = sellPetItems;
        config["storePetItems"] = storePetItems;
        config["pickupUseAbilityPet"] = PlayerConfig.Get("UBot.Items.Pickup.EnableAbilityPet", true);
        config["pickupJustMyItems"] = PlayerConfig.Get("UBot.Items.Pickup.JustPickMyItems", false);
        config["pickupDontInBerzerk"] = PlayerConfig.Get("UBot.Items.Pickup.DontPickupInBerzerk", true);
        config["pickupDontWhileBotting"] = PlayerConfig.Get("UBot.Items.Pickup.DontPickupWhileBotting", false);
        config["pickupGold"] = PlayerConfig.Get("UBot.Items.Pickup.Gold", true);
        config["pickupBlueItems"] = PlayerConfig.Get("UBot.Items.Pickup.Blue", true);
        config["pickupQuestItems"] = PlayerConfig.Get("UBot.Items.Pickup.Quest", true);
        config["pickupRareItems"] = PlayerConfig.Get("UBot.Items.Pickup.Rare", true);
        config["pickupAnyEquips"] = PlayerConfig.Get("UBot.Items.Pickup.AnyEquips", true);
        config["pickupEverything"] = PlayerConfig.Get("UBot.Items.Pickup.Everything", true);
        config["sellFilter"] = ShoppingManager.SellFilter.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        config["storeFilter"] = ShoppingManager.StoreFilter.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        config["pickupFilter"] = BuildPickupFilterSnapshot();

        if (!config.TryGetValue("shoppingShopCodeName", out _))
            config["shoppingShopCodeName"] = string.Empty;

        var showEquipmentOnShopping = false;
        if (config.TryGetValue("showEquipmentOnShopping", out var showEquipmentRaw)
            && TryConvertBool(showEquipmentRaw, out var parsedShowEquipment))
            showEquipmentOnShopping = parsedShowEquipment;
        config["showEquipmentOnShopping"] = showEquipmentOnShopping;

        var shoppingTargets = ParseShoppingTargets(config.TryGetValue("shoppingTargets", out var shoppingTargetsRaw)
            ? shoppingTargetsRaw
            : null);
        config["shoppingTargets"] = shoppingTargets
            .Select(ToShoppingTargetDictionary)
            .Cast<object?>()
            .ToList();

        SyncShoppingTargetsRuntime(shoppingTargets);

        config["shopCatalog"] = BuildItemsShopCatalog();
        config["itemCatalog"] = BuildItemsItemCatalog();

        return config;
    }

    private static List<Dictionary<string, object?>> BuildPickupFilterSnapshot()
    {
        return PickupManager.PickupFilter
            .Where(item => !string.IsNullOrWhiteSpace(item.CodeName))
            .GroupBy(item => item.CodeName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .OrderBy(item => item.CodeName, StringComparer.OrdinalIgnoreCase)
            .Select(item => new Dictionary<string, object?>
            {
                ["codeName"] = item.CodeName,
                ["pickOnlyChar"] = item.PickOnlyChar
            })
            .ToList();
    }

    private static List<Dictionary<string, object?>> BuildItemsShopCatalog()
    {
        var result = new List<Dictionary<string, object?>>();
        if (Game.ReferenceManager?.ShopGroups == null)
            return result;

        Game.ReferenceManager.EnsureShopDataLoaded();

        foreach (var shopGroup in Game.ReferenceManager.ShopGroups.Values)
        {
            if (shopGroup == null || string.IsNullOrWhiteSpace(shopGroup.RefNpcCodeName))
                continue;

            var items = new List<Dictionary<string, object?>>();
            var itemCodeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var good in Game.ReferenceManager.GetRefShopGoods(shopGroup))
            {
                var package = Game.ReferenceManager.GetRefPackageItem(good.RefPackageItemCodeName);
                var itemCodeName = package?.RefItemCodeName;
                if (string.IsNullOrWhiteSpace(itemCodeName) || !itemCodeNames.Add(itemCodeName))
                    continue;

                var refItem = Game.ReferenceManager.GetRefItem(itemCodeName);
                if (refItem == null)
                    continue;

                items.Add(new Dictionary<string, object?>
                {
                    ["codeName"] = refItem.CodeName,
                    ["name"] = ResolveItemDisplayName(refItem),
                    ["isEquip"] = refItem.IsEquip,
                    ["level"] = refItem.ReqLevel1,
                    ["country"] = (int)refItem.Country
                });
            }

            items = items
                .OrderBy(row => row.TryGetValue("name", out var value) ? value?.ToString() : string.Empty, StringComparer.OrdinalIgnoreCase)
                .ToList();

            result.Add(new Dictionary<string, object?>
            {
                ["codeName"] = shopGroup.RefNpcCodeName,
                ["name"] = ResolveShopDisplayName(shopGroup),
                ["items"] = items
            });
        }

        return result
            .GroupBy(row => row.TryGetValue("codeName", out var value) ? value?.ToString() : string.Empty, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(row => row.TryGetValue("name", out var value) ? value?.ToString() : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<Dictionary<string, object?>> BuildItemsItemCatalog()
    {
        var result = new List<Dictionary<string, object?>>();
        if (Game.ReferenceManager?.ItemData == null)
            return result;

        Game.ReferenceManager.EnsureItemDataLoaded();

        foreach (var refItem in Game.ReferenceManager.ItemData.Values)
        {
            if (refItem == null || refItem.TypeID1 != 3 || refItem.IsGold)
                continue;

            result.Add(new Dictionary<string, object?>
            {
                ["codeName"] = refItem.CodeName,
                ["name"] = ResolveItemDisplayName(refItem),
                ["level"] = (int)refItem.ReqLevel1,
                ["degree"] = refItem.Degree,
                ["gender"] = (int)refItem.ReqGender,
                ["country"] = (int)refItem.Country,
                ["rarity"] = (int)(byte)refItem.Rarity,
                ["isEquip"] = refItem.IsEquip,
                ["isQuest"] = refItem.IsQuest,
                ["isAmmunition"] = refItem.IsAmmunition,
                ["typeId2"] = (int)refItem.TypeID2,
                ["typeId3"] = (int)refItem.TypeID3,
                ["typeId4"] = (int)refItem.TypeID4
            });
        }

        return result
            .OrderBy(row => row.TryGetValue("name", out var value) ? value?.ToString() : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string ResolveShopDisplayName(RefShopGroup shopGroup)
    {
        var npc = Game.ReferenceManager?.GetRefObjChar(shopGroup.RefNpcCodeName);
        var translated = npc?.GetRealName();
        if (!string.IsNullOrWhiteSpace(translated))
            return translated;

        return FormatCodeName(shopGroup.RefNpcCodeName);
    }

    private static string ResolveItemDisplayName(RefObjItem item)
    {
        var translated = item.GetRealName();
        if (!string.IsNullOrWhiteSpace(translated))
            return translated;

        return FormatCodeName(item.CodeName);
    }

    private static string FormatCodeName(string? codeName)
    {
        if (string.IsNullOrWhiteSpace(codeName))
            return string.Empty;

        var normalized = codeName.Replace('_', ' ').Trim();
        normalized = normalized.ToLowerInvariant();
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(normalized);
    }

    private static Dictionary<string, object?> ToShoppingTargetDictionary(ItemsShoppingTarget target)
    {
        return new Dictionary<string, object?>
        {
            ["shopCodeName"] = target.ShopCodeName,
            ["itemCodeName"] = target.ItemCodeName,
            ["amount"] = Math.Max(target.Amount, 1)
        };
    }

    private static List<ItemsShoppingTarget> ParseShoppingTargets(object? rawTargets)
    {
        var result = new List<ItemsShoppingTarget>();
        if (rawTargets == null || rawTargets is string || rawTargets is not IEnumerable enumerable)
            return result;

        foreach (var rawEntry in enumerable)
        {
            if (!TryConvertObjectToDictionary(rawEntry, out var entry))
                continue;

            if (!TryGetStringValue(entry, "shopCodeName", out var shopCodeName))
                continue;
            if (!TryGetStringValue(entry, "itemCodeName", out var itemCodeName))
                continue;

            if (!TryGetIntValue(entry, "amount", out var amount))
                amount = 1;

            if (string.IsNullOrWhiteSpace(shopCodeName) || string.IsNullOrWhiteSpace(itemCodeName))
                continue;

            result.Add(new ItemsShoppingTarget
            {
                ShopCodeName = shopCodeName.Trim(),
                ItemCodeName = itemCodeName.Trim(),
                Amount = Math.Clamp(amount, 1, 50000)
            });
        }

        return result
            .GroupBy(item => $"{item.ShopCodeName}|{item.ItemCodeName}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToList();
    }

    private static void SyncShoppingTargetsRuntime(List<ItemsShoppingTarget> targets)
    {
        ShoppingManager.ShoppingList ??= new Dictionary<RefShopGood, int>();
        ShoppingManager.ShoppingList.Clear();

        if (Game.ReferenceManager == null || targets.Count == 0)
            return;

        foreach (var target in targets)
        {
            var shopGroup = Game.ReferenceManager.GetRefShopGroup(target.ShopCodeName);
            if (shopGroup == null)
                continue;

            RefShopGood? matchedGood = null;
            foreach (var good in Game.ReferenceManager.GetRefShopGoods(shopGroup))
            {
                var package = Game.ReferenceManager.GetRefPackageItem(good.RefPackageItemCodeName);
                if (package == null || string.IsNullOrWhiteSpace(package.RefItemCodeName))
                    continue;

                if (string.Equals(package.RefItemCodeName, target.ItemCodeName, StringComparison.OrdinalIgnoreCase))
                {
                    matchedGood = good;
                    break;
                }
            }

            if (matchedGood == null)
                continue;

            ShoppingManager.ShoppingList[matchedGood] = Math.Clamp(target.Amount, 1, 50000);
        }
    }

    private static bool ApplyItemsPluginPatch(Dictionary<string, object?> patch)
    {
        var changed = false;
        ShoppingManager.SellFilter ??= new List<string>();
        ShoppingManager.StoreFilter ??= new List<string>();
        ShoppingManager.ShoppingList ??= new Dictionary<RefShopGood, int>();

        if (TryGetBoolValue(patch, "shoppingEnabled", out var shoppingEnabled))
        {
            PlayerConfig.Set("UBot.Shopping.Enabled", shoppingEnabled);
            ShoppingManager.Enabled = shoppingEnabled;
            changed = true;
        }

        if (TryGetBoolValue(patch, "repairGear", out var repairGear))
        {
            PlayerConfig.Set("UBot.Shopping.RepairGear", repairGear);
            ShoppingManager.RepairGear = repairGear;
            changed = true;
        }

        if (TryGetBoolValue(patch, "sellPetItems", out var sellPetItems))
        {
            PlayerConfig.Set("UBot.Shopping.SellPet", sellPetItems);
            ShoppingManager.SellPetItems = sellPetItems;
            changed = true;
        }

        if (TryGetBoolValue(patch, "storePetItems", out var storePetItems))
        {
            PlayerConfig.Set("UBot.Shopping.StorePet", storePetItems);
            ShoppingManager.StorePetItems = storePetItems;
            changed = true;
        }

        changed |= SetPlayerBool("UBot.Items.Pickup.EnableAbilityPet", patch, "pickupUseAbilityPet");
        changed |= SetPlayerBool("UBot.Items.Pickup.JustPickMyItems", patch, "pickupJustMyItems");
        changed |= SetPlayerBool("UBot.Items.Pickup.DontPickupInBerzerk", patch, "pickupDontInBerzerk");
        changed |= SetPlayerBool("UBot.Items.Pickup.DontPickupWhileBotting", patch, "pickupDontWhileBotting");
        changed |= SetPlayerBool("UBot.Items.Pickup.Gold", patch, "pickupGold");
        changed |= SetPlayerBool("UBot.Items.Pickup.Blue", patch, "pickupBlueItems");
        changed |= SetPlayerBool("UBot.Items.Pickup.Quest", patch, "pickupQuestItems");
        changed |= SetPlayerBool("UBot.Items.Pickup.Rare", patch, "pickupRareItems");
        changed |= SetPlayerBool("UBot.Items.Pickup.AnyEquips", patch, "pickupAnyEquips");
        changed |= SetPlayerBool("UBot.Items.Pickup.Everything", patch, "pickupEverything");

        if (TryGetStringListValue(patch, "sellFilter", out var sellFilter))
        {
            ShoppingManager.SellFilter.Clear();
            ShoppingManager.SellFilter.AddRange(sellFilter);
            ShoppingManager.SaveFilters();
            changed = true;
        }

        if (TryGetStringListValue(patch, "storeFilter", out var storeFilter))
        {
            ShoppingManager.StoreFilter.Clear();
            ShoppingManager.StoreFilter.AddRange(storeFilter);
            ShoppingManager.SaveFilters();
            changed = true;
        }

        if (TryGetPickupFilterValue(patch, "pickupFilter", out var pickupFilter))
        {
            PickupManager.PickupFilter.Clear();
            foreach (var item in pickupFilter)
                PickupManager.PickupFilter.Add(item);
            PickupManager.SaveFilter();
            changed = true;
        }

        var pluginConfig = LoadPluginJsonConfig(ItemsPluginName);
        var pluginConfigChanged = false;

        if (TryGetShoppingTargetsValue(patch, "shoppingTargets", out var shoppingTargets))
        {
            pluginConfig["shoppingTargets"] = shoppingTargets
                .Select(ToShoppingTargetDictionary)
                .Cast<object?>()
                .ToList();
            SyncShoppingTargetsRuntime(shoppingTargets);
            pluginConfigChanged = true;
            changed = true;
        }

        if (TryGetStringValue(patch, "shoppingShopCodeName", out var shoppingShopCodeName))
        {
            pluginConfig["shoppingShopCodeName"] = shoppingShopCodeName?.Trim() ?? string.Empty;
            pluginConfigChanged = true;
            changed = true;
        }

        if (TryGetBoolValue(patch, "showEquipmentOnShopping", out var showEquipmentOnShopping))
        {
            pluginConfig["showEquipmentOnShopping"] = showEquipmentOnShopping;
            pluginConfigChanged = true;
            changed = true;
        }

        if (pluginConfigChanged)
            SavePluginJsonConfig(ItemsPluginName, pluginConfig);

        return changed;
    }

    private static bool TryGetPickupFilterValue(
        IDictionary<string, object?> payload,
        string key,
        out List<(string CodeName, bool PickOnlyChar)> values)
    {
        values = new List<(string CodeName, bool PickOnlyChar)>();
        if (!payload.TryGetValue(key, out var raw))
            return false;

        if (raw == null || raw is string || raw is not IEnumerable enumerable)
            return false;

        foreach (var entryRaw in enumerable)
        {
            if (!TryConvertObjectToDictionary(entryRaw, out var entry))
                continue;
            if (!TryGetStringValue(entry, "codeName", out var codeName))
                continue;

            var pickOnlyChar = false;
            if (entry.TryGetValue("pickOnlyChar", out var pickOnlyCharRaw))
                _ = TryConvertBool(pickOnlyCharRaw, out pickOnlyChar);

            if (string.IsNullOrWhiteSpace(codeName))
                continue;

            values.Add((codeName.Trim(), pickOnlyChar));
        }

        values = values
            .GroupBy(item => item.CodeName, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToList();
        return true;
    }

    private static bool TryGetShoppingTargetsValue(
        IDictionary<string, object?> payload,
        string key,
        out List<ItemsShoppingTarget> values)
    {
        values = new List<ItemsShoppingTarget>();
        if (!payload.TryGetValue(key, out var raw))
            return false;

        values = ParseShoppingTargets(raw);
        return true;
    }
    private static object BuildInventoryPluginState()
    {
        var player = Game.Player;
        if (player == null) return new { selectedTab = "Inventory", items = new List<object>(), freeSlots = 0, totalSlots = 0 };

        var type = PlayerConfig.Get("UBot.Desktop.Inventory.SelectedTab", "Inventory");
        var items = new List<InventoryItemDto>();
        var freeSlots = 0;
        var totalSlots = 0;

        try
        {
            switch (type)
            {
                case "Inventory":
                    if (player.Inventory != null)
                    {
                        // Filter out equipment slots (0-12) for the main inventory tab
                        items = player.Inventory.Where(x => x?.Record != null && x.Slot >= 13).Select(ToInventoryItemDto).ToList();
                        freeSlots = player.Inventory.FreeSlots;
                        totalSlots = player.Inventory.Capacity;
                    }
                    break;
                case "Equipment":
                    if (player.Inventory != null)
                        items = player.Inventory.Where(x => x?.Record != null && x.Slot < 13).Select(ToInventoryItemDto).ToList();
                    break;
                case "Avatars":
                    if (player.Avatars != null)
                        items = player.Avatars.Where(x => x?.Record != null).Select(ToInventoryItemDto).ToList();
                    break;
                case "Storage":
                    if (player.Storage != null)
                    {
                        items = player.Storage.Where(x => x?.Record != null).Select(ToInventoryItemDto).ToList();
                        freeSlots = player.Storage.FreeSlots;
                        totalSlots = player.Storage.Capacity;
                    }
                    break;
                case "Guild Storage":
                    if (player.GuildStorage != null)
                    {
                        items = player.GuildStorage.Where(x => x?.Record != null).Select(ToInventoryItemDto).ToList();
                        freeSlots = player.GuildStorage.FreeSlots;
                        totalSlots = player.GuildStorage.Capacity;
                    }
                    break;
                case "Grab Pet":
                    if (player.AbilityPet?.Inventory != null)
                    {
                        items = player.AbilityPet.Inventory.Where(x => x?.Record != null).Select(ToInventoryItemDto).ToList();
                        freeSlots = player.AbilityPet.Inventory.FreeSlots;
                        totalSlots = player.AbilityPet.Inventory.Capacity;
                    }
                    break;
                case "Job Transport":
                    if (player.JobTransport?.Inventory != null)
                    {
                        items = player.JobTransport.Inventory.Where(x => x?.Record != null).Select(ToInventoryItemDto).ToList();
                        freeSlots = player.JobTransport.Inventory.FreeSlots;
                        totalSlots = player.JobTransport.Inventory.Capacity;
                    }
                    break;
                case "Specialty":
                    if (player.Job2SpecialtyBag != null)
                    {
                        items = player.Job2SpecialtyBag.Where(x => x?.Record != null).Select(ToInventoryItemDto).ToList();
                        freeSlots = player.Job2SpecialtyBag.FreeSlots;
                        totalSlots = player.Job2SpecialtyBag.Capacity;
                    }
                    break;
                case "Job Equipment":
                    if (player.Job2 != null)
                    {
                        items = player.Job2.Where(x => x?.Record != null).Select(ToInventoryItemDto).ToList();
                        freeSlots = player.Job2.FreeSlots;
                        totalSlots = player.Job2.Capacity;
                    }
                    break;
                case "Fellow Pet":
                    if (player.Fellow?.Inventory != null)
                    {
                        items = player.Fellow.Inventory.Where(x => x?.Record != null).Select(ToInventoryItemDto).ToList();
                        freeSlots = player.Fellow.Inventory.FreeSlots;
                        totalSlots = player.Fellow.Inventory.Capacity;
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error building inventory state: {ex.Message}");
        }

        return new
        {
            selectedTab = type,
            items = items,
            freeSlots = freeSlots,
            totalSlots = totalSlots,
            autoSort = PlayerConfig.Get("UBot.Inventory.AutoSort", false)
        };
    }

    private static InventoryItemDto ToInventoryItemDto(InventoryItem item)
    {
        return new InventoryItemDto
        {
            Slot = item.Slot,
            Name = item.Record?.GetRealName() ?? "Unknown",
            Amount = item.Amount,
            Opt = item.OptLevel,
            Icon = item.Record?.AssocFileIcon ?? "",
            CanUse = item.Record != null && (item.Record.CanUse & ObjectUseType.Yes) != 0,
            CanDrop = item.Record != null && item.Record.CanDrop != ObjectDropType.No,
            IsReverseReturnScroll = item.Equals(ReverseReturnScrollFilter),
            Code = item.Record?.CodeName ?? ""
        };
    }


    internal Dictionary<string, object?> BuildConfig() => BuildItemsPluginConfig();
    internal bool ApplyPatch(Dictionary<string, object?> patch) => ApplyItemsPluginPatch(patch);
    internal object BuildInventoryState() => BuildInventoryPluginState();
}

