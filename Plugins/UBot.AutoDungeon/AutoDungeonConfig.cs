using System;
using System.Collections.Generic;
using System.Linq;
using UBot.Core;
using UBot.Core.Objects;

namespace UBot.AutoDungeon;

internal static class AutoDungeonConfig
{
    private const string Prefix = "UBot.AutoDungeon.";

    private const string IgnoreNamesKey = Prefix + "IgnoreNames";
    private const string IgnoreTypesKey = Prefix + "IgnoreTypes";
    private const string OnlyCountTypesKey = Prefix + "OnlyCountTypes";
    private const string AcceptForgottenWorldKey = Prefix + "AcceptForgottenWorld";

    public static HashSet<string> LoadIgnoreNames()
    {
        return PlayerConfig
            .GetArray<string>(IgnoreNamesKey)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    public static void SaveIgnoreNames(IEnumerable<string> names)
    {
        var values = names
            ?.Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? [];

        PlayerConfig.SetArray(IgnoreNamesKey, values);
    }

    public static HashSet<MonsterRarity> LoadIgnoreTypes()
    {
        return PlayerConfig
            .GetArray<byte>(IgnoreTypesKey)
            .Select(value => (MonsterRarity)value)
            .ToHashSet();
    }

    public static void SaveIgnoreTypes(IEnumerable<MonsterRarity> rarities)
    {
        var values = rarities?.Select(rarity => (byte)rarity).Distinct().ToArray() ?? [];
        PlayerConfig.SetArray(IgnoreTypesKey, values);
    }

    public static HashSet<MonsterRarity> LoadOnlyCountTypes()
    {
        return PlayerConfig
            .GetArray<byte>(OnlyCountTypesKey)
            .Select(value => (MonsterRarity)value)
            .ToHashSet();
    }

    public static void SaveOnlyCountTypes(IEnumerable<MonsterRarity> rarities)
    {
        var values = rarities?.Select(rarity => (byte)rarity).Distinct().ToArray() ?? [];
        PlayerConfig.SetArray(OnlyCountTypesKey, values);
    }

    public static bool LoadAcceptForgottenWorld()
    {
        return PlayerConfig.Get(AcceptForgottenWorldKey, false);
    }

    public static void SaveAcceptForgottenWorld(bool enabled)
    {
        PlayerConfig.Set(AcceptForgottenWorldKey, enabled);
    }
}
