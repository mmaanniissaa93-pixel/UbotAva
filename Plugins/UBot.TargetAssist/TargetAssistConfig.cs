using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using UBot.Core;

namespace UBot.TargetAssist;

internal enum TargetAssistRoleMode
{
    Civil = 0,
    Thief = 1,
    HunterTrader = 2
}

internal readonly struct TargetAssistSettings
{
    public TargetAssistSettings(
        bool enabled,
        float maxRange,
        bool includeDeadTargets,
        bool ignoreSnowShieldTargets,
        bool ignoreBloodyStormTargets,
        string[] ignoredGuilds,
        string[] customPlayers,
        bool onlyCustomPlayers,
        TargetAssistRoleMode roleMode,
        Keys targetCycleKey
    )
    {
        Enabled = enabled;
        MaxRange = maxRange;
        IncludeDeadTargets = includeDeadTargets;
        IgnoreSnowShieldTargets = ignoreSnowShieldTargets;
        IgnoreBloodyStormTargets = ignoreBloodyStormTargets;
        IgnoredGuilds = ignoredGuilds;
        CustomPlayers = customPlayers;
        OnlyCustomPlayers = onlyCustomPlayers;
        RoleMode = roleMode;
        TargetCycleKey = targetCycleKey;
        IgnoredGuildSet = new HashSet<string>(
            (IgnoredGuilds ?? Array.Empty<string>())
            .Where(guild => !string.IsNullOrWhiteSpace(guild))
            .Select(guild => guild.Trim()),
            StringComparer.OrdinalIgnoreCase
        );
        CustomPlayerSet = new HashSet<string>(
            (CustomPlayers ?? Array.Empty<string>())
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim()),
            StringComparer.OrdinalIgnoreCase
        );
    }

    public bool Enabled { get; }
    public float MaxRange { get; }
    public bool IncludeDeadTargets { get; }
    public bool IgnoreSnowShieldTargets { get; }
    public bool IgnoreBloodyStormTargets { get; }
    public string[] IgnoredGuilds { get; }
    public string[] CustomPlayers { get; }
    public bool OnlyCustomPlayers { get; }
    public TargetAssistRoleMode RoleMode { get; }
    public Keys TargetCycleKey { get; }
    public HashSet<string> IgnoredGuildSet { get; }
    public HashSet<string> CustomPlayerSet { get; }
}

internal static class TargetAssistConfig
{
    private const string Prefix = "UBot.TargetAssist.";
    private const string EnabledKey = Prefix + "Enabled";
    private const string MaxRangeKey = Prefix + "MaxRange";
    private const string IncludeDeadTargetsKey = Prefix + "IncludeDeadTargets";
    private const string IgnoreSnowShieldTargetsKey = Prefix + "IgnoreSnowShieldTargets";
    private const string IgnoreBloodyStormTargetsKey = Prefix + "IgnoreBloodyStormTargets";
    private const string IgnoredGuildsKey = Prefix + "IgnoredGuilds";
    private const string CustomPlayersKey = Prefix + "CustomPlayers";
    private const string OnlyCustomPlayersKey = Prefix + "OnlyCustomPlayers";
    private const string RoleModeKey = Prefix + "RoleMode";
    private const string TargetCycleKeyKey = Prefix + "TargetCycleKey";

    public static void EnsureDefaults()
    {
        PlayerConfig.Set(EnabledKey, PlayerConfig.Get(EnabledKey, false));
        PlayerConfig.Set(MaxRangeKey, Math.Clamp(PlayerConfig.Get(MaxRangeKey, 40f), 5f, 400f));
        PlayerConfig.Set(IncludeDeadTargetsKey, PlayerConfig.Get(IncludeDeadTargetsKey, false));
        PlayerConfig.Set(IgnoreSnowShieldTargetsKey, PlayerConfig.Get(IgnoreSnowShieldTargetsKey, true));
        PlayerConfig.Set(IgnoreBloodyStormTargetsKey, PlayerConfig.Get(IgnoreBloodyStormTargetsKey, false));
        PlayerConfig.Set(OnlyCustomPlayersKey, PlayerConfig.Get(OnlyCustomPlayersKey, false));

        var roleMode = PlayerConfig.GetEnum(RoleModeKey, TargetAssistRoleMode.Civil);
        PlayerConfig.Set(RoleModeKey, roleMode);

        var hotkey = NormalizeTargetCycleKey(PlayerConfig.Get(TargetCycleKeyKey, "Oem3"));
        PlayerConfig.Set(TargetCycleKeyKey, hotkey.ToString());

        var guilds = PlayerConfig
            .GetArray<string>(IgnoredGuildsKey, '|')
            .Where(guild => !string.IsNullOrWhiteSpace(guild))
            .Select(guild => guild.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        PlayerConfig.SetArray(IgnoredGuildsKey, guilds, "|");

        var customPlayers = PlayerConfig
            .GetArray<string>(CustomPlayersKey, '|')
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        PlayerConfig.SetArray(CustomPlayersKey, customPlayers, "|");
    }

    public static TargetAssistSettings Load()
    {
        var guilds = PlayerConfig
            .GetArray<string>(IgnoredGuildsKey, '|')
            .Where(guild => !string.IsNullOrWhiteSpace(guild))
            .Select(guild => guild.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var customPlayers = PlayerConfig
            .GetArray<string>(CustomPlayersKey, '|')
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new TargetAssistSettings(
            PlayerConfig.Get(EnabledKey, false),
            Math.Clamp(PlayerConfig.Get(MaxRangeKey, 40f), 5f, 400f),
            PlayerConfig.Get(IncludeDeadTargetsKey, false),
            PlayerConfig.Get(IgnoreSnowShieldTargetsKey, true),
            PlayerConfig.Get(IgnoreBloodyStormTargetsKey, false),
            guilds,
            customPlayers,
            PlayerConfig.Get(OnlyCustomPlayersKey, false),
            PlayerConfig.GetEnum(RoleModeKey, TargetAssistRoleMode.Civil),
            NormalizeTargetCycleKey(PlayerConfig.Get(TargetCycleKeyKey, "Oem3"))
        );
    }

    public static Keys NormalizeTargetCycleKey(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Keys.Oem3;

        var normalized = input.Trim();
        if (Enum.TryParse(normalized, true, out Keys parsed) && parsed != Keys.None)
            return parsed;

        return Keys.Oem3;
    }
}
