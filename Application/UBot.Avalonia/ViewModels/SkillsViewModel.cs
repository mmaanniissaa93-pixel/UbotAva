using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using UBot.Avalonia.Services;

namespace UBot.Avalonia.ViewModels;

public sealed class SkillCatalogEntry
{
    public uint Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsPassive { get; set; }
    public bool IsAttack { get; set; }
    public bool IsBuff { get; set; }
    public bool IsImbue { get; set; }
    public bool IsLowLevel { get; set; }
    public int GroupId { get; set; }
    public string BasicGroup { get; set; } = string.Empty;
    public bool IsLearned { get; set; }
    public string Icon { get; set; } = string.Empty;
}



public sealed class MasteryCatalogEntry
{
    public uint Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
}

public sealed class ActiveBuffEntry
{
    public uint Id { get; set; }
    public uint Token { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RemainingMs { get; set; }
    public double RemainingPercent { get; set; }
}

public sealed class SkillsViewModel : PluginViewModelBase
{
    public SkillsViewModel(IUbotCoreService core, AppState state) : base(core, state)
    {
    }

    public Task<byte[]?> GetSkillIconAsync(string iconFile)
        => Core.GetSkillIconAsync(iconFile);

    protected override async void OnAttached()
    {
        await LoadConfigAsync();
    }

    public List<uint> GetAttackSkills(int attackTypeIndex)
        => UIntListCfg($"attackSkills_{attackTypeIndex}");

    public List<uint> GetBuffSkills()
        => UIntListCfg("buffSkills");

    public List<SkillCatalogEntry> GetSkillCatalog()
        => SkillCatalogCfg("skillCatalog");

    public List<MasteryCatalogEntry> GetMasteryCatalog()
        => MasteryCatalogCfg("masteryCatalog");

    public List<ActiveBuffEntry> GetActiveBuffs()
        => ActiveBuffCatalogCfg("activeBuffs");

    public Task PatchAttackSkillsAsync(int attackTypeIndex, IEnumerable<uint> skillIds)
        => PatchConfigAsync(new Dictionary<string, object?>
        {
            [$"attackSkills_{attackTypeIndex}"] = skillIds?.Distinct().ToList() ?? new List<uint>()
        });

    public Task PatchBuffSkillsAsync(IEnumerable<uint> skillIds)
        => PatchConfigAsync(new Dictionary<string, object?>
        {
            ["buffSkills"] = skillIds?.Distinct().ToList() ?? new List<uint>()
        });

    public List<uint> UIntListCfg(string key)
    {
        var result = new List<uint>();
        if (!Config.TryGetValue(key, out var raw) || raw == null)
            return result;

        if (raw is string csv)
        {
            foreach (var part in csv.Split(','))
                if (uint.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    result.Add(parsed);

            return result.Distinct().ToList();
        }

        if (raw is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                if (item == null)
                    continue;

                if (item is uint uintItem)
                {
                    result.Add(uintItem);
                    continue;
                }

                if (item is int intItem && intItem >= 0)
                {
                    result.Add((uint)intItem);
                    continue;
                }

                if (item is long longItem && longItem >= 0 && longItem <= uint.MaxValue)
                {
                    result.Add((uint)longItem);
                    continue;
                }

                if (item is double doubleItem && doubleItem >= 0 && doubleItem <= uint.MaxValue)
                {
                    result.Add((uint)doubleItem);
                    continue;
                }

                if (uint.TryParse(item.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
                    result.Add(parsed);
            }
        }

        return result.Distinct().ToList();
    }

    private List<SkillCatalogEntry> SkillCatalogCfg(string key)
    {
        var result = new List<SkillCatalogEntry>();
        if (!Config.TryGetValue(key, out var raw) || raw == null || raw is not IEnumerable enumerable)
            return result;

        foreach (var item in enumerable)
        {
            if (!TryReadUInt(item, "id", out var id))
                continue;

            var name = TryReadString(item, "name", out var parsedName) ? parsedName : $"Skill {id}";
            result.Add(new SkillCatalogEntry
            {
                Id = id,
                Name = name,
                IsPassive = TryReadBool(item, "isPassive", out var isPassive) && isPassive,
                IsAttack = TryReadBool(item, "isAttack", out var isAttack) && isAttack,
                IsBuff = TryReadBool(item, "isBuff", out var isBuff) && isBuff,
                IsImbue = TryReadBool(item, "isImbue", out var isImbue) && isImbue,
                IsLowLevel = TryReadBool(item, "isLowLevel", out var isLowLevel) && isLowLevel,
                GroupId = TryReadInt(item, "groupId", out var groupId) ? groupId : 0,
                BasicGroup = TryReadString(item, "basicGroup", out var basicGroup) ? basicGroup : string.Empty,
                IsLearned = TryReadBool(item, "isLearned", out var isLearned) && isLearned,
                Icon = TryReadString(item, "icon", out var icon) ? icon : string.Empty


            });
        }

        return result
            .OrderBy(entry => entry.Name, System.StringComparer.OrdinalIgnoreCase)
            .ThenBy(entry => entry.Id)
            .ToList();
    }

    private List<MasteryCatalogEntry> MasteryCatalogCfg(string key)
    {
        var result = new List<MasteryCatalogEntry>();
        if (!Config.TryGetValue(key, out var raw) || raw == null || raw is not IEnumerable enumerable)
            return result;

        foreach (var item in enumerable)
        {
            if (!TryReadUInt(item, "id", out var id))
                continue;

            var name = TryReadString(item, "name", out var parsedName) ? parsedName : $"Mastery {id}";
            TryReadInt(item, "level", out var level);
            result.Add(new MasteryCatalogEntry
            {
                Id = id,
                Name = name,
                Level = level
            });
        }

        return result
            .OrderBy(entry => entry.Name, System.StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private List<ActiveBuffEntry> ActiveBuffCatalogCfg(string key)
    {
        var result = new List<ActiveBuffEntry>();
        if (!Config.TryGetValue(key, out var raw) || raw == null || raw is not IEnumerable enumerable)
            return result;

        foreach (var item in enumerable)
        {
            if (!TryReadUInt(item, "id", out var id))
                continue;

            TryReadUInt(item, "token", out var token);
            TryReadInt(item, "remainingMs", out var remainingMs);
            TryReadDouble(item, "remainingPercent", out var remainingPercent);
            var name = TryReadString(item, "name", out var parsedName) ? parsedName : $"Buff {id}";

            result.Add(new ActiveBuffEntry
            {
                Id = id,
                Token = token,
                Name = name,
                RemainingMs = remainingMs,
                RemainingPercent = remainingPercent
            });
        }

        return result
            .OrderBy(entry => entry.Name, System.StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool TryReadValue(object? source, string key, out object? value)
    {
        value = null;
        if (source == null)
            return false;

        if (source is IDictionary<string, object?> typedDictionary)
        {
            if (typedDictionary.TryGetValue(key, out value))
                return true;

            foreach (var kv in typedDictionary)
            {
                if (string.Equals(kv.Key, key, System.StringComparison.OrdinalIgnoreCase))
                {
                    value = kv.Value;
                    return true;
                }
            }

            return false;
        }

        if (source is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (!string.Equals(entry.Key?.ToString(), key, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                value = entry.Value;
                return true;
            }
        }

        return false;
    }

    private static bool TryReadString(object? source, string key, out string value)
    {
        value = string.Empty;
        if (!TryReadValue(source, key, out var raw) || raw == null)
            return false;

        value = raw.ToString() ?? string.Empty;
        return true;
    }

    private static bool TryReadBool(object? source, string key, out bool value)
    {
        value = false;
        if (!TryReadValue(source, key, out var raw) || raw == null)
            return false;

        if (raw is bool boolValue)
        {
            value = boolValue;
            return true;
        }

        return bool.TryParse(raw.ToString(), out value);
    }

    private static bool TryReadUInt(object? source, string key, out uint value)
    {
        value = 0;
        if (!TryReadValue(source, key, out var raw) || raw == null)
            return false;

        if (raw is uint uintValue)
        {
            value = uintValue;
            return true;
        }

        if (raw is int intValue && intValue >= 0)
        {
            value = (uint)intValue;
            return true;
        }

        if (raw is long longValue && longValue >= 0 && longValue <= uint.MaxValue)
        {
            value = (uint)longValue;
            return true;
        }

        if (raw is double doubleValue && doubleValue >= 0 && doubleValue <= uint.MaxValue)
        {
            value = (uint)doubleValue;
            return true;
        }

        return uint.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryReadInt(object? source, string key, out int value)
    {
        value = 0;
        if (!TryReadValue(source, key, out var raw) || raw == null)
            return false;

        if (raw is int intValue)
        {
            value = intValue;
            return true;
        }

        if (raw is long longValue && longValue >= int.MinValue && longValue <= int.MaxValue)
        {
            value = (int)longValue;
            return true;
        }

        if (raw is double doubleValue)
        {
            value = (int)doubleValue;
            return true;
        }

        return int.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryReadDouble(object? source, string key, out double value)
    {
        value = 0;
        if (!TryReadValue(source, key, out var raw) || raw == null)
            return false;

        if (raw is double doubleValue)
        {
            value = doubleValue;
            return true;
        }

        if (raw is float floatValue)
        {
            value = floatValue;
            return true;
        }

        if (raw is int intValue)
        {
            value = intValue;
            return true;
        }

        if (raw is long longValue)
        {
            value = longValue;
            return true;
        }

        return double.TryParse(raw.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }
}
