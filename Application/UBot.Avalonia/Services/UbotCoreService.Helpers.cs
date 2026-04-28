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

namespace UBot.Avalonia.Services;

public abstract class UbotServiceBase
{
    protected const string ConnectionModeKey = "UBot.Desktop.ConnectionMode";
    protected const string MapPluginName = "UBot.Map";
    protected const string QuestPluginAlias = "UBot.Quest";
    protected const string QuestRuntimePlugin = "UBot.QuestLog";
    protected const string SkillsPluginName = "UBot.Skills";
    protected const string ItemsPluginName = "UBot.Items";
    protected const string PartyPluginName = "UBot.Party";
    protected const string TargetAssistPluginName = "UBot.TargetAssist";
    protected const string CommandCenterPluginName = "UBot.CommandCenter";

    protected const string AlchemyModeKey = "UBot.Desktop.Alchemy.Mode";
    protected const string AlchemyItemCodeKey = "UBot.Desktop.Alchemy.ItemCode";
    protected const string AlchemyMaxEnhancementKey = "UBot.Desktop.Alchemy.MaxEnhancement";
    protected const string AlchemyElixirTypeKey = "UBot.Desktop.Alchemy.ElixirType";
    protected const string AlchemyStopAtNoPowderKey = "UBot.Desktop.Alchemy.StopAtNoPowder";
    protected const string AlchemyUseLuckyStoneKey = "UBot.Desktop.Alchemy.UseLuckyStone";
    protected const string AlchemyUseImmortalStoneKey = "UBot.Desktop.Alchemy.UseImmortalStone";
    protected const string AlchemyUseAstralStoneKey = "UBot.Desktop.Alchemy.UseAstralStone";
    protected const string AlchemyUseSteadyStoneKey = "UBot.Desktop.Alchemy.UseSteadyStone";
    protected const double MapClickMaxStepDistance = 145.0;
    protected const int MapSectorGridSize = 3;
    protected const float MapSectorSpan = 1920f;
    protected const int MinimapSectorPixels = 256;
    protected const int NavMeshSectorPixels = 512;

    protected static readonly MonsterRarity[] AttackRarityByIndex =
    {
        MonsterRarity.General,
        MonsterRarity.Champion,
        MonsterRarity.Giant,
        MonsterRarity.GeneralParty,
        MonsterRarity.ChampionParty,
        MonsterRarity.GiantParty,
        MonsterRarity.Elite,
        MonsterRarity.EliteStrong,
        MonsterRarity.Unique
    };

    protected static readonly TypeIdFilter ReverseReturnScrollFilter = new(3, 3, 3, 3);

    protected static readonly (string Key, string Group, string Label)[] AlchemyBlueOptions =
    {
        ("str", RefMagicOpt.MaterialStr, "Str"),
        ("int", RefMagicOpt.MaterialInt, "Int"),
        ("immortal", RefMagicOpt.MaterialImmortal, "Immortal"),
        ("steady", RefMagicOpt.MaterialSteady, "Steady"),
        ("lucky", RefMagicOpt.MaterialLuck, "Lucky"),
        ("durability", RefMagicOpt.MaterialDurability, "Durability"),
        ("attackRate", RefMagicOpt.MaterialAttackRate, "Attack rate"),
        ("blockRate", RefMagicOpt.MaterialBlockRate, "Blocking rate")
    };

    protected static readonly (string Key, ItemAttributeGroup Group, string Label)[] AlchemyStatOptions =
    {
        ("critical", ItemAttributeGroup.Critical, "Critical"),
        ("magAtk", ItemAttributeGroup.MagicalDamage, "Mag. atk. pwr."),
        ("phyAtk", ItemAttributeGroup.PhysicalDamage, "Phy. atk. pwr."),
        ("attackRate", ItemAttributeGroup.HitRatio, "Attack rate"),
        ("magReinforce", ItemAttributeGroup.MagicalSpecialize, "Mag. reinforce"),
        ("phyReinforce", ItemAttributeGroup.PhysicalSpecialize, "Phy. reinforce"),
        ("durability", ItemAttributeGroup.Durability, "Durability")
    };

    protected static bool TryResolvePlugin(string pluginId, out IPlugin plugin)
    {
        plugin = ExtensionManager.Plugins.FirstOrDefault(item =>
            PluginIdEquals(item.Name, pluginId)
            || (PluginIdEquals(pluginId, QuestPluginAlias) && PluginIdEquals(item.Name, QuestRuntimePlugin)));
        return plugin != null;
    }

    protected static bool TryResolveBotbase(string pluginId, out IBotbase botbase)
    {
        botbase = ExtensionManager.Bots.FirstOrDefault(item => PluginIdEquals(item.Name, pluginId));
        return botbase != null;
    }

    protected static bool PluginIdEquals(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    protected static bool IsTrainingBotbase(IBotbase botbase) => ResolveModuleKey(botbase.Name) == "training";
    protected static bool IsAlchemyBotbase(IBotbase botbase) => ResolveModuleKey(botbase.Name) == "alchemy";
    protected static bool IsTradeBotbase(IBotbase botbase) => ResolveModuleKey(botbase.Name) == "trade";
    protected static bool IsLureBotbase(IBotbase botbase) => ResolveModuleKey(botbase.Name) == "lure";
    protected static bool IsGeneralPlugin(IPlugin plugin) => ResolveModuleKey(plugin.Name) == "general";
    protected static bool IsProtectionPlugin(IPlugin plugin) => ResolveModuleKey(plugin.Name) == "protection";
    protected static bool IsMapPlugin(IPlugin plugin) => ResolveModuleKey(plugin.Name) == "map";
    protected static bool IsInventoryPlugin(IPlugin plugin) => plugin?.Name == "UBot.Inventory";
    protected static bool IsPartyPlugin(IPlugin plugin) => ResolveModuleKey(plugin.Name) == "party";
    protected static bool IsStatisticsPlugin(IPlugin plugin) => ResolveModuleKey(plugin.Name) == "stats";
    protected static bool IsQuestPlugin(IPlugin plugin) => ResolveModuleKey(plugin.Name) == "quest";
    protected static bool IsTargetAssistPlugin(IPlugin plugin) => plugin?.Name == TargetAssistPluginName || ResolveModuleKey(plugin.Name) == "targetassist";
    protected static bool IsCommandCenterPlugin(IPlugin plugin) => plugin?.Name == CommandCenterPluginName || ResolveModuleKey(plugin.Name) == "commandcenter";


    protected static bool IsSkillsPlugin(IPlugin plugin) => ResolveModuleKey(plugin.Name) == "skills";
    protected static bool IsItemsPlugin(IPlugin plugin) => ResolveModuleKey(plugin.Name) == "items";

    protected static string ResolveModuleKey(string id)
    {
        var value = (id ?? string.Empty).ToLowerInvariant();
        if (value.Contains("general")) return "general";
        if (value.Contains("training")) return "training";
        if (value.Contains("skills")) return "skills";
        if (value.Contains("protection")) return "protection";
        if (value.Contains("inventory")) return "inventory";
        if (value.Contains("items")) return "items";
        if (value.Contains("map")) return "map";
        if (value.Contains("party")) return "party";
        if (value.Contains("statistics")) return "stats";
        if (value.Contains("quest")) return "quest";
        if (value.Contains("chat")) return "chat";
        if (value.Contains("log")) return "log";
        if (value.Contains("serverinfo")) return "server";
        if (value.Contains("autodungeon")) return "autodungeon";
        if (value.Contains("targetassist")) return "targetassist";
        if (value.Contains("alchemy")) return "alchemy";
        if (value.Contains("trade")) return "trade";
        if (value.Contains("lure")) return "lure";
        return value.Replace("ubot.", "");
    }


    protected static bool TryGetStringValue(IDictionary<string, object?> payload, string key, out string value)
    {
        value = string.Empty;
        if (!payload.TryGetValue(key, out var raw) || raw == null)
            return false;

        value = raw.ToString() ?? string.Empty;
        return true;
    }

    protected static bool TryGetBoolValue(IDictionary<string, object?> payload, string key, out bool value)
    {
        value = false;
        return payload.TryGetValue(key, out var raw) && TryConvertBool(raw, out value);
    }

    protected static bool TryGetDoubleValue(IDictionary<string, object?> payload, string key, out double value)
    {
        value = 0;
        return payload.TryGetValue(key, out var raw) && TryConvertDouble(raw, out value);
    }

    protected static bool TryGetUIntValue(IDictionary<string, object?> payload, string key, out uint value)
    {
        value = 0;
        if (!payload.TryGetValue(key, out var raw) || raw == null)
            return false;

        return TryConvertUIntLoose(raw, out value);
    }

    protected static bool TryConvertUIntLoose(object? raw, out uint value)
    {
        value = 0;
        if (raw == null)
            return false;

        switch (raw)
        {
            case uint uintValue:
                value = uintValue;
                return true;
            case int intValue when intValue >= 0:
                value = (uint)intValue;
                return true;
            case long longValue when longValue >= 0 && longValue <= uint.MaxValue:
                value = (uint)longValue;
                return true;
            case short shortValue when shortValue >= 0:
                value = (uint)shortValue;
                return true;
            case ushort ushortValue:
                value = ushortValue;
                return true;
            case byte byteValue:
                value = byteValue;
                return true;
            case double doubleValue when doubleValue >= 0 && doubleValue <= uint.MaxValue:
                value = (uint)Math.Round(doubleValue);
                return true;
            case float floatValue when floatValue >= 0 && floatValue <= uint.MaxValue:
                value = (uint)Math.Round(floatValue);
                return true;
        }

        return uint.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    protected static bool TryGetUIntListValue(IDictionary<string, object?> payload, string key, out List<uint> values)
    {
        values = new List<uint>();
        if (!payload.TryGetValue(key, out var raw) || raw == null)
            return false;

        if (raw is string text)
        {
            foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                if (uint.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    values.Add(parsed);
            }

            values = values.Distinct().ToList();
            return true;
        }

        if (raw is IEnumerable<uint> uintValues)
        {
            values = uintValues.Distinct().ToList();
            return true;
        }

        if (raw is IEnumerable<int> intValues)
        {
            values = intValues.Where(item => item >= 0).Select(item => (uint)item).Distinct().ToList();
            return true;
        }

        if (raw is IEnumerable<long> longValues)
        {
            values = longValues.Where(item => item >= 0 && item <= uint.MaxValue).Select(item => (uint)item).Distinct().ToList();
            return true;
        }

        if (raw is IEnumerable<double> doubleValues)
        {
            values = doubleValues
                .Where(item => item >= 0 && item <= uint.MaxValue)
                .Select(item => (uint)Math.Round(item))
                .Distinct()
                .ToList();
            return true;
        }

        if (raw is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;

                if (item is uint uintItem)
                {
                    values.Add(uintItem);
                    continue;
                }

                if (item is int intItem && intItem >= 0)
                {
                    values.Add((uint)intItem);
                    continue;
                }

                if (item is long longItem && longItem >= 0 && longItem <= uint.MaxValue)
                {
                    values.Add((uint)longItem);
                    continue;
                }

                if (uint.TryParse(item.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    values.Add(parsed);
            }

            values = values.Distinct().ToList();
            return true;
        }

        return false;
    }

    protected static bool TryGetStringListValue(IDictionary<string, object?> payload, string key, out List<string> values)
    {
        values = new List<string>();
        if (!payload.TryGetValue(key, out var raw) || raw == null)
            return false;

        if (raw is string single)
        {
            if (!string.IsNullOrWhiteSpace(single))
                values.Add(single.Trim());
            return true;
        }

        if (raw is IEnumerable<string> stringEnum)
        {
            values = stringEnum.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return true;
        }

        if (raw is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                var text = item?.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                    values.Add(text.Trim());
            }
            values = values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return true;
        }

        return false;
    }

    protected static bool TryGetIntValue(IDictionary<string, object?> payload, string key, out int value)
    {
        value = 0;
        return payload.TryGetValue(key, out var raw) && TryConvertInt(raw, out value);
    }


    protected static bool TryConvertObjectToDictionary(object? value, out Dictionary<string, object?> result)
    {
        if (value is Dictionary<string, object?> dict)
        {
            result = new Dictionary<string, object?>(dict, StringComparer.OrdinalIgnoreCase);
            return true;
        }

        if (value is IDictionary<string, object?> typedDict)
        {
            result = new Dictionary<string, object?>(typedDict, StringComparer.OrdinalIgnoreCase);
            return true;
        }

        if (value is IDictionary untypedDict)
        {
            result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (DictionaryEntry entry in untypedDict)
            {
                if (entry.Key == null)
                    continue;
                var key = entry.Key.ToString();
                if (string.IsNullOrWhiteSpace(key))
                    continue;
                result[key] = entry.Value;
            }

            return result.Count > 0;
        }

        if (value is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            result = JsonObjectToDictionary(element);
            return true;
        }

        result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        return false;
    }

    protected static bool TryConvertBool(object? value, out bool parsed)
    {
        parsed = false;
        if (value == null)
            return false;
        if (value is bool boolValue)
        {
            parsed = boolValue;
            return true;
        }
        if (value is string text && bool.TryParse(text, out var boolParsed))
        {
            parsed = boolParsed;
            return true;
        }
        return false;
    }

    protected static bool TryConvertInt(object? value, out int parsed)
    {
        parsed = 0;
        if (value == null)
            return false;
        if (value is int intValue)
        {
            parsed = intValue;
            return true;
        }
        if (value is long longValue && longValue >= int.MinValue && longValue <= int.MaxValue)
        {
            parsed = (int)longValue;
            return true;
        }
        if (value is double doubleValue)
        {
            parsed = (int)Math.Round(doubleValue);
            return true;
        }
        if (int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
        {
            parsed = parsedInt;
            return true;
        }
        return false;
    }

    protected static bool TryConvertDouble(object? value, out double parsed)
    {
        parsed = 0;
        if (value == null)
            return false;
        if (value is double doubleValue)
        {
            parsed = doubleValue;
            return true;
        }
        if (value is float floatValue)
        {
            parsed = floatValue;
            return true;
        }
        if (value is int intValue)
        {
            parsed = intValue;
            return true;
        }
        if (value is long longValue)
        {
            parsed = longValue;
            return true;
        }
        if (double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedDouble))
        {
            parsed = parsedDouble;
            return true;
        }
        return false;
    }

    protected static string NormalizeMapShowFilterValue(string? value)
    {
        var normalized = (value ?? "all").Trim().ToLowerInvariant();
        return normalized switch
        {
            "all" => "all",
            "monster" => "monster",
            "mob" => "monster",
            "party" => "party",
            "npc" => "npc",
            "character" => "character",
            "player" => "character",
            "pet" => "pet",
            "other" => "other",
            _ => "all"
        };
    }

    protected static string GetPluginConfigKey(string pluginName) => $"UBot.Desktop.PluginConfig.{pluginName}";

    protected static Dictionary<string, object?> LoadPluginJsonConfig(string pluginName)
    {
        var key = GetPluginConfigKey(pluginName);
        var raw = GlobalConfig.Get(key, "{}");
        try
        {
            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
                return JsonObjectToDictionary(document.RootElement);
        }
        catch
        {
            // ignored
        }

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    protected static void SavePluginJsonConfig(string pluginName, Dictionary<string, object?> config)
    {
        GlobalConfig.Set(GetPluginConfigKey(pluginName), JsonSerializer.Serialize(config));
    }

    protected static Dictionary<string, object?> JsonObjectToDictionary(JsonElement element)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in element.EnumerateObject())
            result[property.Name] = JsonElementToObject(property.Value);
        return result;
    }

    protected static object? JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => JsonObjectToDictionary(element),
            JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var value) ? value : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.ToString()
        };
    }

    protected static JsonElement ToJsonElement(object value)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value));
        return document.RootElement.Clone();
    }
}

