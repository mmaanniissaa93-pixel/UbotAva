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

internal sealed class UbotTargetAssistPluginService : UbotServiceBase
{
    private static Dictionary<string, object?> BuildTargetAssistPluginConfig()
    {
        var roleModeRaw = UBot.Core.RuntimeAccess.Player.Get("UBot.TargetAssist.RoleMode", "Civil");
        var roleMode = roleModeRaw.Equals("Thief", StringComparison.OrdinalIgnoreCase)
            ? "thief"
            : roleModeRaw.Equals("HunterTrader", StringComparison.OrdinalIgnoreCase)
                ? "hunterTrader"
                : "civil";

        return new Dictionary<string, object?>
        {
            ["enabled"] = UBot.Core.RuntimeAccess.Player.Get("UBot.TargetAssist.Enabled", false),
            ["maxRange"] = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.TargetAssist.MaxRange", 40f), 5f, 400f),
            ["includeDeadTargets"] = UBot.Core.RuntimeAccess.Player.Get("UBot.TargetAssist.IncludeDeadTargets", false),
            ["ignoreSnowShieldTargets"] = UBot.Core.RuntimeAccess.Player.Get("UBot.TargetAssist.IgnoreSnowShieldTargets", true),
            ["ignoreBloodyStormTargets"] = UBot.Core.RuntimeAccess.Player.Get("UBot.TargetAssist.IgnoreBloodyStormTargets", false),
            ["ignoredGuilds"] = UBot.Core.RuntimeAccess.Player.GetArray<string>("UBot.TargetAssist.IgnoredGuilds", '|').Cast<object?>().ToList(),
            ["customPlayers"] = UBot.Core.RuntimeAccess.Player.GetArray<string>("UBot.TargetAssist.CustomPlayers", '|').Cast<object?>().ToList(),
            ["onlyCustomPlayers"] = UBot.Core.RuntimeAccess.Player.Get("UBot.TargetAssist.OnlyCustomPlayers", false),
            ["roleMode"] = roleMode,
            ["targetCycleKey"] = UBot.Core.RuntimeAccess.Player.Get("UBot.TargetAssist.TargetCycleKey", "Oem3")
        };
    }

    private static bool ApplyTargetAssistPluginPatch(Dictionary<string, object?> patch)
    {
        var changed = false;
        changed |= SetPlayerBool("UBot.TargetAssist.Enabled", patch, "enabled");
        changed |= SetPlayerBool("UBot.TargetAssist.IncludeDeadTargets", patch, "includeDeadTargets");
        changed |= SetPlayerBool("UBot.TargetAssist.IgnoreSnowShieldTargets", patch, "ignoreSnowShieldTargets");
        changed |= SetPlayerBool("UBot.TargetAssist.IgnoreBloodyStormTargets", patch, "ignoreBloodyStormTargets");
        changed |= SetPlayerBool("UBot.TargetAssist.OnlyCustomPlayers", patch, "onlyCustomPlayers");
        changed |= SetPlayerString("UBot.TargetAssist.TargetCycleKey", patch, "targetCycleKey");

        if (TryGetDoubleValue(patch, "maxRange", out var maxRange))
        {
            UBot.Core.RuntimeAccess.Player.Set("UBot.TargetAssist.MaxRange", (float)Math.Clamp(maxRange, 5d, 400d));
            changed = true;
        }

        if (TryGetStringListValue(patch, "ignoredGuilds", out var ignoredGuilds))
        {
            UBot.Core.RuntimeAccess.Player.SetArray(
                "UBot.TargetAssist.IgnoredGuilds",
                ignoredGuilds.Select(value => value.Trim())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase),
                "|");
            changed = true;
        }

        if (TryGetStringListValue(patch, "customPlayers", out var customPlayers))
        {
            UBot.Core.RuntimeAccess.Player.SetArray(
                "UBot.TargetAssist.CustomPlayers",
                customPlayers.Select(value => value.Trim())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase),
                "|");
            changed = true;
        }

        if (TryGetStringValue(patch, "roleMode", out var roleModeRaw))
        {
            var roleMode = roleModeRaw.Trim().ToLowerInvariant() switch
            {
                "thief" => "Thief",
                "huntertrader" => "HunterTrader",
                "hunter_trader" => "HunterTrader",
                "hunter-trader" => "HunterTrader",
                _ => "Civil"
            };
            UBot.Core.RuntimeAccess.Player.Set("UBot.TargetAssist.RoleMode", roleMode);
            changed = true;
        }

        if (changed)
            UBot.Core.RuntimeAccess.Events.FireEvent("OnSavePlayerConfig");

        return changed;
    }

    private static object BuildTargetAssistState()
    {
        const int effectTransferParam = 1701213281;
        var bloodyStormCodeTokens = new[] { "FANSTORM", "FAN_STORM" };

        var enabled = UBot.Core.RuntimeAccess.Player.Get("UBot.TargetAssist.Enabled", false);
        var maxRange = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.TargetAssist.MaxRange", 40f), 5f, 400f);
        var includeDeadTargets = UBot.Core.RuntimeAccess.Player.Get("UBot.TargetAssist.IncludeDeadTargets", false);
        var ignoreSnowShieldTargets = UBot.Core.RuntimeAccess.Player.Get("UBot.TargetAssist.IgnoreSnowShieldTargets", true);
        var ignoreBloodyStormTargets = UBot.Core.RuntimeAccess.Player.Get("UBot.TargetAssist.IgnoreBloodyStormTargets", false);
        var onlyCustomPlayers = UBot.Core.RuntimeAccess.Player.Get("UBot.TargetAssist.OnlyCustomPlayers", false);

        var roleModeRaw = UBot.Core.RuntimeAccess.Player.Get("UBot.TargetAssist.RoleMode", "Civil");
        var roleMode = roleModeRaw.Equals("Thief", StringComparison.OrdinalIgnoreCase)
            ? "thief"
            : roleModeRaw.Equals("HunterTrader", StringComparison.OrdinalIgnoreCase)
                ? "hunterTrader"
                : "civil";

        var ignoredGuilds = UBot.Core.RuntimeAccess.Player.GetArray<string>("UBot.TargetAssist.IgnoredGuilds", '|')
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var ignoredGuildSet = new HashSet<string>(ignoredGuilds, StringComparer.OrdinalIgnoreCase);

        var customPlayers = UBot.Core.RuntimeAccess.Player.GetArray<string>("UBot.TargetAssist.CustomPlayers", '|')
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var customPlayerSet = new HashSet<string>(customPlayers, StringComparer.OrdinalIgnoreCase);

        var candidateCount = 0;
        var nearestTargetName = string.Empty;
        var nearestTargetDistance = -1d;

        if (enabled
            && UBot.Core.RuntimeAccess.Session.Ready
            && UBot.Core.RuntimeAccess.Session.Player != null
            && UBot.Core.RuntimeAccess.Session.Player.State?.LifeState == LifeState.Alive
            && SpawnManager.TryGetEntities<SpawnedPlayer>(out var players))
        {
            var candidates = players
                .Where(player => player != null && player.UniqueId != UBot.Core.RuntimeAccess.Session.Player.UniqueId)
                .Where(player => !string.IsNullOrWhiteSpace(player.Name))
                .Where(player => includeDeadTargets || player.State?.LifeState == LifeState.Alive)
                .Where(player => player.DistanceToPlayer <= maxRange)
                .Where(player => !ignoreSnowShieldTargets || !HasSnowShieldBuff(player, effectTransferParam))
                .Where(player => !ignoreBloodyStormTargets || !HasAnyBuffCodeToken(player, bloodyStormCodeTokens))
                .Where(player => !IsIgnoredGuildName(player, ignoredGuildSet))
                .Where(player => !onlyCustomPlayers || customPlayerSet.Contains(player.Name.Trim()))
                .Where(player => MatchesTargetAssistRoleMode(player, roleMode))
                .OrderBy(player => player.DistanceToPlayer)
                .ToList();

            candidateCount = candidates.Count;
            if (candidateCount > 0)
            {
                nearestTargetName = candidates[0].Name;
                nearestTargetDistance = Math.Round(candidates[0].DistanceToPlayer, 1);
            }
        }

        return new Dictionary<string, object?>
        {
            ["enabled"] = enabled,
            ["maxRange"] = maxRange,
            ["includeDeadTargets"] = includeDeadTargets,
            ["ignoreSnowShieldTargets"] = ignoreSnowShieldTargets,
            ["ignoreBloodyStormTargets"] = ignoreBloodyStormTargets,
            ["onlyCustomPlayers"] = onlyCustomPlayers,
            ["roleMode"] = roleMode,
            ["targetCycleKey"] = UBot.Core.RuntimeAccess.Player.Get("UBot.TargetAssist.TargetCycleKey", "Oem3"),
            ["ignoredGuilds"] = ignoredGuilds.Cast<object?>().ToList(),
            ["customPlayers"] = customPlayers.Cast<object?>().ToList(),
            ["candidateCount"] = candidateCount,
            ["nearestTargetName"] = nearestTargetName,
            ["nearestTargetDistance"] = nearestTargetDistance
        };
    }

    private static bool HasSnowShieldBuff(SpawnedPlayer player, int effectTransferParam)
    {
        if (player?.State?.ActiveBuffs == null)
            return false;

        foreach (var buff in player.State.ActiveBuffs)
        {
            var record = buff?.Record;
            if (record == null)
                continue;

            var code = record.Basic_Code;
            if (!string.IsNullOrWhiteSpace(code) && code.IndexOf("COLD_SHIELD", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (record.Params != null && record.Params.Contains(effectTransferParam))
                return true;
        }

        return false;
    }

    private static bool HasAnyBuffCodeToken(SpawnedPlayer player, IEnumerable<string> tokens)
    {
        if (player?.State?.ActiveBuffs == null || tokens == null)
            return false;

        foreach (var buff in player.State.ActiveBuffs)
        {
            var code = buff?.Record?.Basic_Code;
            if (string.IsNullOrWhiteSpace(code))
                continue;

            foreach (var token in tokens)
            {
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                if (code.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
        }

        return false;
    }

    private static bool IsIgnoredGuildName(SpawnedPlayer player, HashSet<string> ignoredGuildSet)
    {
        var guildName = player.Guild?.Name;
        if (string.IsNullOrWhiteSpace(guildName))
            return false;

        return ignoredGuildSet.Contains(guildName.Trim());
    }

    private static bool MatchesTargetAssistRoleMode(SpawnedPlayer player, string roleMode)
    {
        if (roleMode.Equals("thief", StringComparison.OrdinalIgnoreCase))
            return player.WearsJobSuite && (player.Job == JobType.Hunter || player.Job == JobType.Trade);

        if (roleMode.Equals("hunterTrader", StringComparison.OrdinalIgnoreCase))
            return player.WearsJobSuite && player.Job == JobType.Thief;

        return true;
    }

    internal Dictionary<string, object?> BuildConfig() => BuildTargetAssistPluginConfig();
    internal bool ApplyPatch(Dictionary<string, object?> patch) => ApplyTargetAssistPluginPatch(patch);
    internal object BuildState() => BuildTargetAssistState();
}

