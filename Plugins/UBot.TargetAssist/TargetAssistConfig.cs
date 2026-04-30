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
        UBot.Core.RuntimeAccess.Player.Set(EnabledKey, UBot.Core.RuntimeAccess.Player.Get(EnabledKey, false));
        UBot.Core.RuntimeAccess.Player.Set(MaxRangeKey, Math.Clamp(UBot.Core.RuntimeAccess.Player.Get(MaxRangeKey, 40f), 5f, 400f));
        UBot.Core.RuntimeAccess.Player.Set(IncludeDeadTargetsKey, UBot.Core.RuntimeAccess.Player.Get(IncludeDeadTargetsKey, false));
        UBot.Core.RuntimeAccess.Player.Set(IgnoreSnowShieldTargetsKey, UBot.Core.RuntimeAccess.Player.Get(IgnoreSnowShieldTargetsKey, true));
        UBot.Core.RuntimeAccess.Player.Set(IgnoreBloodyStormTargetsKey, UBot.Core.RuntimeAccess.Player.Get(IgnoreBloodyStormTargetsKey, false));
        UBot.Core.RuntimeAccess.Player.Set(OnlyCustomPlayersKey, UBot.Core.RuntimeAccess.Player.Get(OnlyCustomPlayersKey, false));

        var roleMode = UBot.Core.RuntimeAccess.Player.GetEnum(RoleModeKey, TargetAssistRoleMode.Civil);
        UBot.Core.RuntimeAccess.Player.Set(RoleModeKey, roleMode);

        var hotkey = NormalizeTargetCycleKey(UBot.Core.RuntimeAccess.Player.Get(TargetCycleKeyKey, "Oem3"));
        UBot.Core.RuntimeAccess.Player.Set(TargetCycleKeyKey, hotkey.ToString());

        var guilds = PlayerConfig
            .GetArray<string>(IgnoredGuildsKey, '|')
            .Where(guild => !string.IsNullOrWhiteSpace(guild))
            .Select(guild => guild.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        UBot.Core.RuntimeAccess.Player.SetArray(IgnoredGuildsKey, guilds, "|");

        var customPlayers = PlayerConfig
            .GetArray<string>(CustomPlayersKey, '|')
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        UBot.Core.RuntimeAccess.Player.SetArray(CustomPlayersKey, customPlayers, "|");
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
            UBot.Core.RuntimeAccess.Player.Get(EnabledKey, false),
            Math.Clamp(UBot.Core.RuntimeAccess.Player.Get(MaxRangeKey, 40f), 5f, 400f),
            UBot.Core.RuntimeAccess.Player.Get(IncludeDeadTargetsKey, false),
            UBot.Core.RuntimeAccess.Player.Get(IgnoreSnowShieldTargetsKey, true),
            UBot.Core.RuntimeAccess.Player.Get(IgnoreBloodyStormTargetsKey, false),
            guilds,
            customPlayers,
            UBot.Core.RuntimeAccess.Player.Get(OnlyCustomPlayersKey, false),
            UBot.Core.RuntimeAccess.Player.GetEnum(RoleModeKey, TargetAssistRoleMode.Civil),
            NormalizeTargetCycleKey(UBot.Core.RuntimeAccess.Player.Get(TargetCycleKeyKey, "Oem3"))
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
