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
using UBot.GameData.ReferenceObjects;
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

internal sealed class UbotAlchemyBotbaseService : UbotServiceBase
{
    private static object BuildAlchemyState(IBotbase botbase)
    {
        var selectableItems = GetAlchemySelectableItems();
        var selectedItem = ResolveAlchemySelectedItem(selectableItems);
        var blues = BuildAlchemyBlueRows(selectedItem);
        var stats = BuildAlchemyStatRows(selectedItem);

        return new Dictionary<string, object?>
        {
            ["selected"] = Kernel.Bot?.Botbase?.Name == botbase?.Name,
            ["mode"] = NormalizeAlchemyMode(PlayerConfig.Get(AlchemyModeKey, "enhance")),
            ["hasItem"] = selectedItem != null,
            ["selectedItem"] = selectedItem == null
                ? null
                : new Dictionary<string, object?>
                {
                    ["codeName"] = selectedItem.Record?.CodeName ?? string.Empty,
                    ["name"] = selectedItem.Record?.GetRealName(true) ?? selectedItem.ItemId.ToString(CultureInfo.InvariantCulture),
                    ["degree"] = selectedItem.Record?.Degree ?? 0,
                    ["optLevel"] = selectedItem.OptLevel,
                    ["slot"] = selectedItem.Slot
                },
            ["luckyPowderCount"] = selectedItem != null ? GetAlchemyLuckyPowderCount(selectedItem) : 0,
            ["luckyStoneCount"] = selectedItem != null ? GetAlchemyStonesByGroup(selectedItem, RefMagicOpt.MaterialLuck).Sum(item => item.Amount) : 0,
            ["immortalStoneCount"] = selectedItem != null ? GetAlchemyStonesByGroup(selectedItem, RefMagicOpt.MaterialImmortal).Sum(item => item.Amount) : 0,
            ["astralStoneCount"] = selectedItem != null ? GetAlchemyStonesByGroup(selectedItem, RefMagicOpt.MaterialAstral).Sum(item => item.Amount) : 0,
            ["steadyStoneCount"] = selectedItem != null ? GetAlchemyStonesByGroup(selectedItem, RefMagicOpt.MaterialSteady).Sum(item => item.Amount) : 0,
            ["itemsCatalog"] = selectableItems
                .GroupBy(item => item.Record?.CodeName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(item => item.OptLevel).ThenBy(item => item.Slot).First())
                .OrderBy(item => item.Record?.GetRealName() ?? string.Empty)
                .Select(item => new Dictionary<string, object?>
                {
                    ["codeName"] = item.Record?.CodeName ?? string.Empty,
                    ["name"] = $"{item.Record?.GetRealName(true) ?? item.ItemId.ToString(CultureInfo.InvariantCulture)} (+{item.OptLevel})",
                    ["degree"] = item.Record?.Degree ?? 0,
                    ["optLevel"] = item.OptLevel,
                    ["slot"] = item.Slot
                })
                .Cast<object?>()
                .ToList(),
            ["alchemyBlues"] = blues.Cast<object?>().ToList(),
            ["alchemyStats"] = stats.Cast<object?>().ToList()
        };
    }

    private static object[] BuildAlchemyBlueRows(InventoryItem selectedItem)
    {
        if (selectedItem?.Record == null)
            return Array.Empty<object>();

        var degree = selectedItem.Record.Degree;
        var rows = AlchemyBlueOptions.Select(option =>
        {
            var currentValue = GetAlchemyMagicOptionValue(selectedItem, option.Group);
            var maxValue = GetAlchemyMagicOptionMaxValue(selectedItem, option.Group);
            var stones = GetAlchemyStonesByGroup(selectedItem, option.Group).Sum(item => item.Amount);

            return new Dictionary<string, object?>
            {
                ["key"] = option.Key,
                ["name"] = option.Label,
                ["value"] = currentValue.ToString(CultureInfo.InvariantCulture),
                ["current"] = (int)currentValue,
                ["max"] = (int)maxValue,
                ["stoneCount"] = stones,
                ["group"] = option.Group,
                ["degree"] = degree
            };
        }).Cast<object>().ToList();

        rows.Add(new Dictionary<string, object?>
        {
            ["key"] = "availableSlots",
            ["name"] = "Available slots",
            ["value"] = selectedItem.MagicOptions?.Count.ToString(CultureInfo.InvariantCulture) ?? "0",
            ["current"] = selectedItem.MagicOptions?.Count ?? 0,
            ["max"] = selectedItem.MagicOptions?.Count ?? 0,
            ["stoneCount"] = 0,
            ["group"] = string.Empty,
            ["degree"] = degree
        });

        return rows.ToArray();
    }

    private static object[] BuildAlchemyStatRows(InventoryItem selectedItem)
    {
        if (selectedItem?.Record == null)
            return Array.Empty<object>();

        var availableGroups = ItemAttributesInfo.GetAvailableAttributeGroupsForItem(selectedItem.Record)?.ToHashSet()
            ?? new HashSet<ItemAttributeGroup>();

        return AlchemyStatOptions
            .Select(option =>
            {
                var currentPercent = availableGroups.Contains(option.Group)
                    ? GetAlchemyAttributePercentage(selectedItem, option.Group)
                    : 0;

                return new Dictionary<string, object?>
                {
                    ["key"] = option.Key,
                    ["name"] = option.Label,
                    ["value"] = currentPercent > 0 ? $"+{currentPercent}%" : "0",
                    ["current"] = currentPercent
                };
            })
            .Cast<object>()
            .ToArray();
    }

    private static IReadOnlyList<InventoryItem> GetAlchemySelectableItems()
    {
        var inventory = Game.Player?.Inventory;
        if (inventory == null)
            return Array.Empty<InventoryItem>();

        return inventory
            .GetNormalPartItems(item =>
                item?.Record != null
                && item.Record.IsEquip
                && !item.Record.IsAvatar
                && (item.Record.IsWeapon || item.Record.IsShield || item.Record.IsArmor || item.Record.IsAccessory))
            .OrderBy(item => item.Slot)
            .ToArray();
    }

    private static InventoryItem? ResolveAlchemySelectedItem(IEnumerable<InventoryItem>? candidates = null)
    {
        var source = candidates?.ToList() ?? GetAlchemySelectableItems().ToList();
        if (source.Count == 0)
            return null;

        var codeName = (PlayerConfig.Get(AlchemyItemCodeKey, string.Empty) ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(codeName))
        {
            var matched = source.FirstOrDefault(item =>
                item.Record?.CodeName != null
                && item.Record.CodeName.Equals(codeName, StringComparison.OrdinalIgnoreCase));
            if (matched != null)
                return matched;
        }

        return source.FirstOrDefault();
    }

    private static IReadOnlyList<InventoryItem> GetAlchemyStonesByGroup(InventoryItem targetItem, string group)
    {
        if (targetItem?.Record == null || string.IsNullOrWhiteSpace(group))
            return Array.Empty<InventoryItem>();

        var inventory = Game.Player?.Inventory;
        if (inventory == null)
            return Array.Empty<InventoryItem>();

        return inventory
            .GetNormalPartItems(item =>
                item?.Record != null
                && item.Record.Desc1 == group
                && item.Record.ItemClass == targetItem.Record.Degree)
            .ToArray();
    }

    private static IReadOnlyList<InventoryItem> ResolveAlchemyElixirs(InventoryItem targetItem, string elixirType)
    {
        var inventory = Game.Player?.Inventory;
        if (inventory == null || targetItem?.Record == null)
            return Array.Empty<InventoryItem>();

        const int protectorParam = 16909056;
        const int weaponParam = 100663296;
        const int accessoryParam = 83886080;
        const int shieldParam = 67108864;

        var normalizedType = NormalizeAlchemyElixirType(elixirType);
        var paramValue = normalizedType switch
        {
            "shield" => shieldParam,
            "protector" => protectorParam,
            "accessory" => accessoryParam,
            _ => weaponParam
        };

        var degree = targetItem.Record.Degree;
        Func<InventoryItem, bool> predicate;
        if (Game.ClientType >= GameClientType.Chinese && degree >= 12)
            predicate = item => item.Record.Param1 == degree && item.Record.Param3 == paramValue;
        else
            predicate = item => item.Record.Param1 == paramValue;

        return inventory.GetNormalPartItems(item => item?.Record != null && predicate(item)).ToArray();
    }

    private static int GetAlchemyLuckyPowderCount(InventoryItem targetItem)
    {
        if (targetItem?.Record == null)
            return 0;

        var inventory = Game.Player?.Inventory;
        if (inventory == null)
            return 0;

        var powders = inventory
            .GetNormalPartItems(item =>
                item?.Record != null
                && item.Record.TypeID2 == 3
                && item.Record.TypeID3 == 10
                && item.Record.TypeID4 == 2
                && item.Record.ItemClass == targetItem.Record.Degree)
            .Sum(item => item.Amount);

        if (Game.ClientType >= GameClientType.Chinese && targetItem.Record.Degree >= 12)
        {
            powders += inventory
                .GetNormalPartItems(item =>
                    item?.Record != null
                    && item.Record.TypeID2 == 3
                    && item.Record.TypeID3 == 10
                    && item.Record.TypeID4 == 8
                    && item.Record.Param1 == targetItem.Record.ItemClass)
                .Sum(item => item.Amount);
        }

        return powders;
    }

    private static uint GetAlchemyMagicOptionValue(InventoryItem targetItem, string group)
    {
        if (targetItem?.Record == null || string.IsNullOrWhiteSpace(group))
            return 0;

        var option = targetItem.MagicOptions?.FirstOrDefault(m =>
        {
            var record = m?.Record ?? Game.ReferenceManager.GetMagicOption(m?.Id ?? 0);
            return record?.Group == group;
        });

        return option?.Value ?? 0;
    }

    private static ushort GetAlchemyMagicOptionMaxValue(InventoryItem targetItem, string group)
    {
        if (targetItem?.Record == null || string.IsNullOrWhiteSpace(group))
            return 0;

        var current = targetItem.MagicOptions?.FirstOrDefault(m =>
        {
            var record = m?.Record ?? Game.ReferenceManager.GetMagicOption(m?.Id ?? 0);
            return record?.Group == group;
        });

        if (current?.Record != null)
            return current.Record.GetMaxValue();

        var byDegree = Game.ReferenceManager.GetMagicOption(group, (byte)targetItem.Record.Degree);
        return byDegree?.GetMaxValue() ?? 0;
    }

    private static int GetAlchemyAttributePercentage(InventoryItem targetItem, ItemAttributeGroup group)
    {
        if (targetItem?.Record == null)
            return 0;

        var available = ItemAttributesInfo.GetAvailableAttributeGroupsForItem(targetItem.Record);
        if (available == null || !available.Contains(group))
            return 0;

        var slot = ItemAttributesInfo.GetAttributeSlotForItem(group, targetItem.Record);
        return targetItem.Attributes.GetPercentage(slot);
    }

    private static string NormalizeAlchemyMode(string mode)
    {
        var normalized = (mode ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "blues" => "blues",
            "stats" => "stats",
            _ => "enhance"
        };
    }

    private static string NormalizeAlchemyElixirType(string type)
    {
        var normalized = (type ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "shield" => "shield",
            "protector" => "protector",
            "accessory" => "accessory",
            _ => "weapon"
        };
    }

    private static string NormalizeAlchemyStatTarget(string target)
    {
        var normalized = (target ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "low" => "low",
            "medium" => "medium",
            "high" => "high",
            "max" => "max",
            _ => "off"
        };
    }

    private static int MapAlchemyStatTargetToPercent(string target)
    {
        return NormalizeAlchemyStatTarget(target) switch
        {
            "low" => 25,
            "medium" => 50,
            "high" => 75,
            "max" => 100,
            _ => 0
        };
    }

    private static string GetAlchemyBlueEnabledConfigKey(string key) => $"UBot.Desktop.Alchemy.Blue.{key}.Enabled";
    private static string GetAlchemyBlueMaxConfigKey(string key) => $"UBot.Desktop.Alchemy.Blue.{key}.Max";
    private static string GetAlchemyStatEnabledConfigKey(string key) => $"UBot.Desktop.Alchemy.Stat.{key}.Enabled";
    private static string GetAlchemyStatTargetConfigKey(string key) => $"UBot.Desktop.Alchemy.Stat.{key}.Target";

    private static string InferAlchemyElixirType(InventoryItem selectedItem)
    {
        var record = selectedItem?.Record;
        if (record == null)
            return "weapon";

        if (record.IsShield)
            return "shield";
        if (record.IsAccessory)
            return "accessory";
        if (record.IsArmor)
            return "protector";
        return "weapon";
    }

    private static Dictionary<string, object?> BuildAlchemyBotbaseConfig()
    {
        var selectedItem = ResolveAlchemySelectedItem();
        var mode = NormalizeAlchemyMode(PlayerConfig.Get(AlchemyModeKey, "enhance"));
        var elixirType = NormalizeAlchemyElixirType(PlayerConfig.Get(AlchemyElixirTypeKey, InferAlchemyElixirType(selectedItem)));

        var config = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["alchemyMode"] = mode,
            ["alchemyItemCode"] = PlayerConfig.Get(AlchemyItemCodeKey, selectedItem?.Record?.CodeName ?? string.Empty),
            ["alchemyItemDegree"] = selectedItem?.Record?.Degree ?? 0,
            ["alchemyCurrentEnhancement"] = selectedItem?.OptLevel ?? 0,
            ["alchemyMaxEnhancement"] = Math.Clamp(PlayerConfig.Get(AlchemyMaxEnhancementKey, 0), 0, 15),
            ["alchemyElixirType"] = elixirType,
            ["stopAtNoPowder"] = PlayerConfig.Get(AlchemyStopAtNoPowderKey, true),
            ["useLuckyStone"] = PlayerConfig.Get(AlchemyUseLuckyStoneKey, false),
            ["useImmortalStone"] = PlayerConfig.Get(AlchemyUseImmortalStoneKey, false),
            ["useAstralStone"] = PlayerConfig.Get(AlchemyUseAstralStoneKey, false),
            ["useSteadyStone"] = PlayerConfig.Get(AlchemyUseSteadyStoneKey, false),
            ["luckyPowderCount"] = selectedItem != null ? GetAlchemyLuckyPowderCount(selectedItem) : 0,
            ["luckyStoneCount"] = selectedItem != null ? GetAlchemyStonesByGroup(selectedItem, RefMagicOpt.MaterialLuck).Sum(item => item.Amount) : 0,
            ["immortalStoneCount"] = selectedItem != null ? GetAlchemyStonesByGroup(selectedItem, RefMagicOpt.MaterialImmortal).Sum(item => item.Amount) : 0,
            ["astralStoneCount"] = selectedItem != null ? GetAlchemyStonesByGroup(selectedItem, RefMagicOpt.MaterialAstral).Sum(item => item.Amount) : 0,
            ["steadyStoneCount"] = selectedItem != null ? GetAlchemyStonesByGroup(selectedItem, RefMagicOpt.MaterialSteady).Sum(item => item.Amount) : 0
        };

        foreach (var option in AlchemyBlueOptions)
        {
            var enabledKey = GetAlchemyBlueEnabledConfigKey(option.Key);
            var maxKey = GetAlchemyBlueMaxConfigKey(option.Key);
            var currentValue = selectedItem != null ? (int)GetAlchemyMagicOptionValue(selectedItem, option.Group) : 0;
            var maxValue = selectedItem != null ? (int)GetAlchemyMagicOptionMaxValue(selectedItem, option.Group) : 0;
            var persistedMax = Math.Max(0, PlayerConfig.Get(maxKey, maxValue));
            var stoneCount = selectedItem != null ? GetAlchemyStonesByGroup(selectedItem, option.Group).Sum(item => item.Amount) : 0;

            config[$"alchemyBlueEnabled_{option.Key}"] = PlayerConfig.Get(enabledKey, false);
            config[$"alchemyBlueCurrent_{option.Key}"] = currentValue;
            config[$"alchemyBlueMax_{option.Key}"] = persistedMax;
            config[$"alchemyBlueStoneCount_{option.Key}"] = stoneCount;
        }

        var availableSlotsMax = Math.Max(0, PlayerConfig.Get(GetAlchemyBlueMaxConfigKey("availableSlots"), selectedItem?.MagicOptions?.Count ?? 0));
        config["alchemyBlueEnabled_availableSlots"] = PlayerConfig.Get(GetAlchemyBlueEnabledConfigKey("availableSlots"), false);
        config["alchemyBlueCurrent_availableSlots"] = selectedItem?.MagicOptions?.Count ?? 0;
        config["alchemyBlueMax_availableSlots"] = availableSlotsMax;
        config["alchemyBlueStoneCount_availableSlots"] = 0;

        foreach (var stat in AlchemyStatOptions)
        {
            var enabledKey = GetAlchemyStatEnabledConfigKey(stat.Key);
            var targetKey = GetAlchemyStatTargetConfigKey(stat.Key);
            var currentValue = selectedItem != null ? GetAlchemyAttributePercentage(selectedItem, stat.Group) : 0;
            var target = NormalizeAlchemyStatTarget(PlayerConfig.Get(targetKey, "off"));

            config[$"alchemyStatEnabled_{stat.Key}"] = PlayerConfig.Get(enabledKey, false);
            config[$"alchemyStatTarget_{stat.Key}"] = target;
            config[$"alchemyStatCurrent_{stat.Key}"] = currentValue;
        }

        return config;
    }

    private static bool ApplyAlchemyBotbasePatch(IBotbase botbase, Dictionary<string, object?> patch)
    {
        var changed = false;

        if (TryGetStringValue(patch, "alchemyMode", out var mode))
        {
            PlayerConfig.Set(AlchemyModeKey, NormalizeAlchemyMode(mode));
            changed = true;
        }

        if (TryGetStringValue(patch, "alchemyItemCode", out var itemCode))
        {
            PlayerConfig.Set(AlchemyItemCodeKey, itemCode.Trim());
            changed = true;
        }

        if (TryGetIntValue(patch, "alchemyMaxEnhancement", out var maxEnhancement))
        {
            PlayerConfig.Set(AlchemyMaxEnhancementKey, Math.Clamp(maxEnhancement, 0, 15));
            changed = true;
        }

        if (TryGetStringValue(patch, "alchemyElixirType", out var elixirType))
        {
            PlayerConfig.Set(AlchemyElixirTypeKey, NormalizeAlchemyElixirType(elixirType));
            changed = true;
        }

        changed |= SetPlayerBool(AlchemyStopAtNoPowderKey, patch, "stopAtNoPowder");
        changed |= SetPlayerBool(AlchemyUseLuckyStoneKey, patch, "useLuckyStone");
        changed |= SetPlayerBool(AlchemyUseImmortalStoneKey, patch, "useImmortalStone");
        changed |= SetPlayerBool(AlchemyUseAstralStoneKey, patch, "useAstralStone");
        changed |= SetPlayerBool(AlchemyUseSteadyStoneKey, patch, "useSteadyStone");

        foreach (var entry in patch)
        {
            const string blueEnabledPrefix = "alchemyBlueEnabled_";
            const string blueMaxPrefix = "alchemyBlueMax_";
            const string statEnabledPrefix = "alchemyStatEnabled_";
            const string statTargetPrefix = "alchemyStatTarget_";

            if (entry.Key.StartsWith(blueEnabledPrefix, StringComparison.Ordinal))
            {
                if (TryConvertBool(entry.Value, out var enabled))
                {
                    var key = entry.Key.Substring(blueEnabledPrefix.Length);
                    PlayerConfig.Set(GetAlchemyBlueEnabledConfigKey(key), enabled);
                    changed = true;
                }
                continue;
            }

            if (entry.Key.StartsWith(blueMaxPrefix, StringComparison.Ordinal))
            {
                if (TryConvertInt(entry.Value, out var maxValue))
                {
                    var key = entry.Key.Substring(blueMaxPrefix.Length);
                    PlayerConfig.Set(GetAlchemyBlueMaxConfigKey(key), Math.Max(0, maxValue));
                    changed = true;
                }
                continue;
            }

            if (entry.Key.StartsWith(statEnabledPrefix, StringComparison.Ordinal))
            {
                if (TryConvertBool(entry.Value, out var enabled))
                {
                    var key = entry.Key.Substring(statEnabledPrefix.Length);
                    PlayerConfig.Set(GetAlchemyStatEnabledConfigKey(key), enabled);
                    changed = true;
                }
                continue;
            }

            if (entry.Key.StartsWith(statTargetPrefix, StringComparison.Ordinal))
            {
                var key = entry.Key.Substring(statTargetPrefix.Length);
                PlayerConfig.Set(GetAlchemyStatTargetConfigKey(key), NormalizeAlchemyStatTarget(entry.Value?.ToString() ?? string.Empty));
                changed = true;
            }
        }

        if (changed)
            EventManager.FireEvent("OnSavePlayerConfig");

        ApplyAlchemyRuntimeConfig(botbase);
        return changed;
    }

    private static bool ApplyAlchemyRuntimeConfig(IBotbase botbase = null)
    {
        var activeBotbase = botbase ?? Kernel.Bot?.Botbase;
        if (!IsAlchemyBotbase(activeBotbase))
            return false;

        var globalsType = Type.GetType("UBot.Alchemy.Globals, UBot.Alchemy", false);
        var globalsBotbaseProperty = globalsType?.GetProperty("Botbase", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
        var runtimeBotbase = globalsBotbaseProperty?.GetValue(null);
        if (runtimeBotbase == null)
            return false;

        var selectedItem = ResolveAlchemySelectedItem();
        var mode = NormalizeAlchemyMode(PlayerConfig.Get(AlchemyModeKey, "enhance"));

        var magicTargets = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        var magicStones = new Dictionary<InventoryItem, RefMagicOpt>();
        if (selectedItem?.Record != null)
        {
            foreach (var option in AlchemyBlueOptions)
            {
                if (!PlayerConfig.Get(GetAlchemyBlueEnabledConfigKey(option.Key), false))
                    continue;

                var referenceOption = Game.ReferenceManager.GetMagicOption(option.Group, (byte)selectedItem.Record.Degree);
                if (referenceOption == null)
                    continue;

                var targetValue = PlayerConfig.Get(GetAlchemyBlueMaxConfigKey(option.Key), 0);
                if (targetValue <= 0)
                    targetValue = referenceOption.GetMaxValue();

                targetValue = Math.Min(targetValue, referenceOption.GetMaxValue());
                if (targetValue <= 0)
                    continue;

                var stone = GetAlchemyStonesByGroup(selectedItem, option.Group).FirstOrDefault(item => item.Amount > 0);
                if (stone == null)
                    continue;

                magicStones[stone] = referenceOption;
                magicTargets[option.Group] = (uint)targetValue;
            }
        }

        var attributePlans = new List<(ItemAttributeGroup Group, InventoryItem Stone, int MaxValue)>();
        if (selectedItem?.Record != null)
        {
            var available = ItemAttributesInfo.GetAvailableAttributeGroupsForItem(selectedItem.Record)?.ToHashSet()
                ?? new HashSet<ItemAttributeGroup>();

            foreach (var option in AlchemyStatOptions)
            {
                if (!PlayerConfig.Get(GetAlchemyStatEnabledConfigKey(option.Key), false))
                    continue;

                var targetScale = NormalizeAlchemyStatTarget(PlayerConfig.Get(GetAlchemyStatTargetConfigKey(option.Key), "off"));
                var targetValue = MapAlchemyStatTargetToPercent(targetScale);
                if (targetValue <= 0 || !available.Contains(option.Group))
                    continue;

                var groupName = ItemAttributesInfo.GetActualAttributeGroupNameForItem(selectedItem.Record, option.Group);
                if (string.IsNullOrWhiteSpace(groupName))
                    continue;

                var stone = Game.Player?.Inventory?
                    .GetNormalPartItems(item =>
                        item?.Record != null
                        && item.Record.TypeID2 == 3
                        && item.Record.TypeID3 == 11
                        && item.Record.TypeID4 == 2
                        && item.Record.Desc1 == groupName)
                    .FirstOrDefault(item => item.Amount > 0);

                if (stone == null)
                    continue;

                attributePlans.Add((option.Group, stone, targetValue));
            }
        }

        var engineName = mode switch
        {
            "stats" => "Attribute",
            "blues" => "Magic",
            _ => "Enhance"
        };

        var runtimeType = runtimeBotbase.GetType();
        var engineProperty = runtimeType.GetProperty("AlchemyEngine", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
        if (engineProperty?.PropertyType?.IsEnum == true)
        {
            var enumValue = Enum.Parse(engineProperty.PropertyType, engineName, ignoreCase: true);
            engineProperty.SetValue(runtimeBotbase, enumValue);
        }

        var enhanceConfigType = Type.GetType("UBot.Alchemy.Bundle.Enhance.EnhanceBundleConfig, UBot.Alchemy", false);
        var magicConfigType = Type.GetType("UBot.Alchemy.Bundle.Magic.MagicBundleConfig, UBot.Alchemy", false);
        var attributeConfigType = Type.GetType("UBot.Alchemy.Bundle.Attribute.AttributeBundleConfig, UBot.Alchemy", false);
        var attributeItemType = Type.GetType("UBot.Alchemy.Bundle.Attribute.AttributeBundleConfig+AttributeBundleConfigItem, UBot.Alchemy", false);

        object enhanceConfig = null;
        object magicConfig = null;
        object attributeConfig = null;

        if (engineName == "Enhance" && enhanceConfigType != null)
        {
            var config = Activator.CreateInstance(enhanceConfigType);
            var maxOpt = Math.Clamp(PlayerConfig.Get(AlchemyMaxEnhancementKey, 0), 0, 15);
            var elixirType = NormalizeAlchemyElixirType(PlayerConfig.Get(AlchemyElixirTypeKey, InferAlchemyElixirType(selectedItem)));
            var elixirs = selectedItem != null ? ResolveAlchemyElixirs(selectedItem, elixirType).ToArray() : Array.Empty<InventoryItem>();

            enhanceConfigType.GetProperty("MaxOptLevel")?.SetValue(config, (byte)maxOpt);
            enhanceConfigType.GetProperty("Item")?.SetValue(config, selectedItem);
            enhanceConfigType.GetProperty("StopIfLuckyPowderEmpty")?.SetValue(config, PlayerConfig.Get(AlchemyStopAtNoPowderKey, true));
            enhanceConfigType.GetProperty("UseImmortalStones")?.SetValue(config, PlayerConfig.Get(AlchemyUseImmortalStoneKey, false));
            enhanceConfigType.GetProperty("UseAstralStones")?.SetValue(config, PlayerConfig.Get(AlchemyUseAstralStoneKey, false));
            enhanceConfigType.GetProperty("UseSteadyStones")?.SetValue(config, PlayerConfig.Get(AlchemyUseSteadyStoneKey, false));
            enhanceConfigType.GetProperty("UseLuckyStones")?.SetValue(config, PlayerConfig.Get(AlchemyUseLuckyStoneKey, false));
            enhanceConfigType.GetProperty("Elixirs")?.SetValue(config, elixirs);

            enhanceConfig = config;
        }

        if (engineName == "Magic" && magicConfigType != null)
        {
            var config = Activator.CreateInstance(magicConfigType);
            magicConfigType.GetProperty("Item")?.SetValue(config, selectedItem);
            magicConfigType.GetProperty("MagicStones")?.SetValue(config, magicStones);
            magicConfigType.GetProperty("TargetValues")?.SetValue(config, magicTargets);
            magicConfig = config;
        }

        if (engineName == "Attribute" && attributeConfigType != null && attributeItemType != null)
        {
            var config = Activator.CreateInstance(attributeConfigType);
            var attributeListType = typeof(List<>).MakeGenericType(attributeItemType);
            var attributeList = (IList)Activator.CreateInstance(attributeListType);

            foreach (var plan in attributePlans)
            {
                var item = Activator.CreateInstance(attributeItemType);
                attributeItemType.GetProperty("MaxValue")?.SetValue(item, plan.MaxValue);
                attributeItemType.GetProperty("Stone")?.SetValue(item, plan.Stone);
                attributeItemType.GetProperty("Group")?.SetValue(item, plan.Group);
                attributeList.Add(item);
            }

            attributeConfigType.GetProperty("Item")?.SetValue(config, selectedItem);
            attributeConfigType.GetProperty("Attributes")?.SetValue(config, attributeList);
            attributeConfig = config;
        }

        runtimeType.GetProperty("EnhanceBundleConfig", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)?.SetValue(runtimeBotbase, enhanceConfig);
        runtimeType.GetProperty("MagicBundleConfig", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)?.SetValue(runtimeBotbase, magicConfig);
        runtimeType.GetProperty("AttributeBundleConfig", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public)?.SetValue(runtimeBotbase, attributeConfig);
        return true;
    }

    internal Dictionary<string, object?> BuildConfig() => BuildAlchemyBotbaseConfig();
    internal bool ApplyPatch(IBotbase botbase, Dictionary<string, object?> patch) => ApplyAlchemyBotbasePatch(botbase, patch);
    internal object BuildState(IBotbase botbase) => BuildAlchemyState(botbase);
}

