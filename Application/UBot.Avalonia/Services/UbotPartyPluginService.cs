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

internal sealed class UbotPartyPluginService : UbotServiceBase
{
    private static Dictionary<string, object?> BuildPartyPluginConfig()
    {
        var config = LoadPluginJsonConfig(PartyPluginName);

        config["expAutoShare"] = PlayerConfig.Get("UBot.Party.EXPAutoShare", true);
        config["itemAutoShare"] = PlayerConfig.Get("UBot.Party.ItemAutoShare", true);
        config["allowInvitations"] = PlayerConfig.Get("UBot.Party.AllowInvitations", true);

        config["acceptAllInvitations"] = PlayerConfig.Get("UBot.Party.AcceptAll", false);
        config["acceptInvitationsFromList"] = PlayerConfig.Get("UBot.Party.AcceptList", false);
        config["autoInviteAllPlayers"] = PlayerConfig.Get("UBot.Party.InviteAll", false);
        config["autoInviteAllPlayersFromList"] = PlayerConfig.Get("UBot.Party.InviteList", false);
        config["acceptInviteOnlyTrainingPlace"] = PlayerConfig.Get("UBot.Party.AtTrainingPlace", false);
        config["acceptIfBotStopped"] = PlayerConfig.Get("UBot.Party.AcceptIfBotStopped", false);
        config["leaveIfMasterNot"] = PlayerConfig.Get("UBot.Party.LeaveIfMasterNot", false);
        config["leaveIfMasterNotName"] = PlayerConfig.Get("UBot.Party.LeaveIfMasterNotName", string.Empty);
        config["alwaysFollowPartyMaster"] = PlayerConfig.Get("UBot.Party.AlwaysFollowPartyMaster", false);
        config["listenPartyMasterCommands"] = PlayerConfig.Get("UBot.Party.Commands.ListenFromMaster", false);
        config["listenCommandsInList"] = PlayerConfig.Get("UBot.Party.Commands.ListenOnlyList", false);

        config["autoPartyPlayers"] = PlayerConfig.GetArray<string>("UBot.Party.AutoPartyList")
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<object?>()
            .ToList();

        config["commandPlayers"] = PlayerConfig.GetArray<string>("UBot.Party.Commands.PlayersList")
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<object?>()
            .ToList();

        config["matchingPurpose"] = (int)PlayerConfig.Get<byte>("UBot.Party.Matching.Purpose", 0);
        config["matchingTitle"] = PlayerConfig.Get("UBot.Party.Matching.Title", "For opening hunting on the silkroad!");
        config["matchingAutoReform"] = PlayerConfig.Get("UBot.Party.Matching.AutoReform", false);
        config["matchingAutoAccept"] = PlayerConfig.Get("UBot.Party.Matching.AutoAccept", true);
        config["matchingLevelFrom"] = (int)PlayerConfig.Get<byte>("UBot.Party.Matching.LevelFrom", 1);
        config["matchingLevelTo"] = (int)PlayerConfig.Get<byte>("UBot.Party.Matching.LevelTo", 140);
        config["autoJoinByName"] = PlayerConfig.Get("UBot.Party.AutoJoin.ByName", false);
        config["autoJoinByTitle"] = PlayerConfig.Get("UBot.Party.AutoJoin.ByTitle", false);
        config["autoJoinByNameText"] = PlayerConfig.Get("UBot.Party.AutoJoin.Name", string.Empty);
        config["autoJoinByTitleText"] = PlayerConfig.Get("UBot.Party.AutoJoin.Title", string.Empty);

        config["buffHideLowLevelSkills"] = PlayerConfig.Get("UBot.Party.Buff.HideLowLevelSkills", false);
        config["buffGroups"] = PlayerConfig.GetArray<string>("UBot.Party.Buff.Groups")
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Cast<object?>()
            .ToList();
        config["buffSkillIds"] = PlayerConfig.GetArray<uint>("UBot.Party.Buff.SkillIds")
            .Distinct()
            .Cast<object?>()
            .ToList();
        config["buffAssignments"] = PlayerConfig.GetArray<string>("UBot.Party.Buff.Assignments")
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Cast<object?>()
            .ToList();

        if (!config.TryGetValue("matchingResults", out var matchingResults) || matchingResults is not IList)
            config["matchingResults"] = new List<object?>();

        if (!config.TryGetValue("matchingSelectedId", out _))
            config["matchingSelectedId"] = 0U;

        return config;
    }

    private static List<Dictionary<string, object?>> BuildPartyBuffCatalog(bool hideLowLevelSkills)
    {
        var result = new List<Dictionary<string, object?>>();
        foreach (var skill in CollectKnownAndAbilitySkills())
        {
            var record = skill.Record;
            if (record == null)
                continue;
            if (skill.IsPassive || skill.IsAttack)
                continue;

            var isLowLevel = false;
            try
            {
                isLowLevel = skill.IsLowLevel();
            }
            catch
            {
                // ignored
            }

            if (hideLowLevelSkills && isLowLevel)
                continue;

            var name = record.GetRealName();
            if (string.IsNullOrWhiteSpace(name))
                name = record.Basic_Code;
            if (string.IsNullOrWhiteSpace(name))
                name = $"Skill {skill.Id}";

            result.Add(new Dictionary<string, object?>
            {
                ["id"] = skill.Id,
                ["name"] = name,
                ["icon"] = record.UI_IconFile,
                ["isLowLevel"] = isLowLevel
            });
        }

        return result
            .OrderBy(item => item.TryGetValue("name", out var value) ? value?.ToString() : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed class PartyBuffAssignment
    {
        public string Name { get; set; } = string.Empty;
        public string Group { get; set; } = string.Empty;
        public List<uint> Buffs { get; set; } = new();
    }

    private static List<PartyBuffAssignment> ParsePartyBuffAssignments(IEnumerable<string> serializedAssignments)
    {
        var result = new List<PartyBuffAssignment>();
        foreach (var raw in serializedAssignments ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var parts = raw.Split(':');
            if (parts.Length != 3)
                continue;

            var name = parts[0].Trim();
            var group = parts[1].Trim();
            if (string.IsNullOrWhiteSpace(name))
                continue;

            var buffs = parts[2]
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(token => uint.TryParse(token, out _))
                .Select(uint.Parse)
                .Distinct()
                .ToList();

            result.Add(new PartyBuffAssignment
            {
                Name = name,
                Group = group,
                Buffs = buffs
            });
        }

        return result
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToList();
    }

    private static List<string> SerializePartyBuffAssignments(IEnumerable<PartyBuffAssignment> assignments)
    {
        return (assignments ?? Array.Empty<PartyBuffAssignment>())
            .Where(item => !string.IsNullOrWhiteSpace(item.Name))
            .Select(item =>
            {
                var buffString = string.Join(",", (item.Buffs ?? new List<uint>()).Distinct());
                return $"{item.Name}:{item.Group}:{buffString}";
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static PartySettings GetPartySettingsSnapshot()
    {
        if (Game.Party?.Settings != null)
            return Game.Party.Settings;

        return new PartySettings
        {
            ExperienceAutoShare = PlayerConfig.Get("UBot.Party.EXPAutoShare", true),
            ItemAutoShare = PlayerConfig.Get("UBot.Party.ItemAutoShare", true),
            AllowInvitation = PlayerConfig.Get("UBot.Party.AllowInvitations", true)
        };
    }

    internal static void ApplyLivePartySettingsFromConfig()
    {
        if (Game.Party == null)
            return;

        var expAutoShare = PlayerConfig.Get("UBot.Party.EXPAutoShare", true);
        var itemAutoShare = PlayerConfig.Get("UBot.Party.ItemAutoShare", true);
        var allowInvitations = PlayerConfig.Get("UBot.Party.AllowInvitations", true);

        if (Game.Party.Settings == null)
            Game.Party.Settings = new PartySettings(expAutoShare, itemAutoShare, allowInvitations);
        else
        {
            Game.Party.Settings.ExperienceAutoShare = expAutoShare;
            Game.Party.Settings.ItemAutoShare = itemAutoShare;
            Game.Party.Settings.AllowInvitation = allowInvitations;
        }
    }

    internal static void RefreshPartyPluginRuntime()
    {
        try
        {
            var containerType = Type.GetType("UBot.Party.Bundle.Container, UBot.Party", false);
            var refreshMethod = containerType?.GetMethod("Refresh", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            refreshMethod?.Invoke(null, null);
        }
        catch
        {
            // ignored
        }
    }
    private static bool ApplyPartyPluginPatch(Dictionary<string, object?> patch)
    {
        var changed = false;

        changed |= SetPlayerBool("UBot.Party.EXPAutoShare", patch, "expAutoShare");
        changed |= SetPlayerBool("UBot.Party.ItemAutoShare", patch, "itemAutoShare");
        changed |= SetPlayerBool("UBot.Party.AllowInvitations", patch, "allowInvitations");

        changed |= SetPlayerBool("UBot.Party.AcceptAll", patch, "acceptAllInvitations");
        changed |= SetPlayerBool("UBot.Party.AcceptList", patch, "acceptInvitationsFromList");
        changed |= SetPlayerBool("UBot.Party.InviteAll", patch, "autoInviteAllPlayers");
        changed |= SetPlayerBool("UBot.Party.InviteList", patch, "autoInviteAllPlayersFromList");
        changed |= SetPlayerBool("UBot.Party.AtTrainingPlace", patch, "acceptInviteOnlyTrainingPlace");
        changed |= SetPlayerBool("UBot.Party.AcceptIfBotStopped", patch, "acceptIfBotStopped");
        changed |= SetPlayerBool("UBot.Party.LeaveIfMasterNot", patch, "leaveIfMasterNot");
        changed |= SetPlayerString("UBot.Party.LeaveIfMasterNotName", patch, "leaveIfMasterNotName");
        changed |= SetPlayerBool("UBot.Party.AlwaysFollowPartyMaster", patch, "alwaysFollowPartyMaster");
        changed |= SetPlayerBool("UBot.Party.Commands.ListenFromMaster", patch, "listenPartyMasterCommands");
        changed |= SetPlayerBool("UBot.Party.Commands.ListenOnlyList", patch, "listenCommandsInList");

        changed |= SetPlayerBool("UBot.Party.Matching.AutoReform", patch, "matchingAutoReform");
        changed |= SetPlayerBool("UBot.Party.Matching.AutoAccept", patch, "matchingAutoAccept");
        changed |= SetPlayerString("UBot.Party.Matching.Title", patch, "matchingTitle");
        changed |= SetPlayerBool("UBot.Party.AutoJoin.ByName", patch, "autoJoinByName");
        changed |= SetPlayerBool("UBot.Party.AutoJoin.ByTitle", patch, "autoJoinByTitle");
        changed |= SetPlayerString("UBot.Party.AutoJoin.Name", patch, "autoJoinByNameText");
        changed |= SetPlayerString("UBot.Party.AutoJoin.Title", patch, "autoJoinByTitleText");
        changed |= SetPlayerBool("UBot.Party.Buff.HideLowLevelSkills", patch, "buffHideLowLevelSkills");

        if (TryGetIntValue(patch, "matchingPurpose", out var matchingPurpose))
        {
            PlayerConfig.Set("UBot.Party.Matching.Purpose", (byte)Math.Clamp(matchingPurpose, 0, 3));
            changed = true;
        }

        if (TryGetIntValue(patch, "matchingLevelFrom", out var levelFrom))
        {
            PlayerConfig.Set("UBot.Party.Matching.LevelFrom", (byte)Math.Clamp(levelFrom, 1, 140));
            changed = true;
        }

        if (TryGetIntValue(patch, "matchingLevelTo", out var levelTo))
        {
            PlayerConfig.Set("UBot.Party.Matching.LevelTo", (byte)Math.Clamp(levelTo, 1, 140));
            changed = true;
        }

        if (TryGetStringListValue(patch, "autoPartyPlayers", out var autoPartyPlayers))
        {
            var normalized = autoPartyPlayers
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            PlayerConfig.SetArray("UBot.Party.AutoPartyList", normalized);
            changed = true;
        }

        if (TryGetStringListValue(patch, "commandPlayers", out var commandPlayers))
        {
            var normalized = commandPlayers
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            PlayerConfig.SetArray("UBot.Party.Commands.PlayersList", normalized);
            changed = true;
        }

        if (TryGetStringListValue(patch, "buffGroups", out var buffGroups))
        {
            var normalized = buffGroups
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            PlayerConfig.SetArray("UBot.Party.Buff.Groups", normalized);
            changed = true;
        }

        if (TryGetUIntListValue(patch, "buffSkillIds", out var buffSkillIds))
        {
            PlayerConfig.SetArray("UBot.Party.Buff.SkillIds", buffSkillIds.Distinct().ToArray());
            changed = true;
        }

        if (TryGetStringListValue(patch, "buffAssignments", out var buffAssignments))
        {
            var normalizedAssignments = ParsePartyBuffAssignments(buffAssignments);
            PlayerConfig.SetArray("UBot.Party.Buff.Assignments", SerializePartyBuffAssignments(normalizedAssignments).ToArray());
            changed = true;
        }

        var pluginConfig = LoadPluginJsonConfig(PartyPluginName);
        var pluginConfigChanged = false;

        if (patch.TryGetValue("matchingSelectedId", out var selectedIdRaw) && selectedIdRaw != null)
        {
            if (TryConvertInt(selectedIdRaw, out var selectedIdInt) && selectedIdInt >= 0)
            {
                pluginConfig["matchingSelectedId"] = (uint)selectedIdInt;
                pluginConfigChanged = true;
                changed = true;
            }
            else if (selectedIdRaw is uint selectedIdUInt)
            {
                pluginConfig["matchingSelectedId"] = selectedIdUInt;
                pluginConfigChanged = true;
                changed = true;
            }
        }

        if (pluginConfigChanged)
            SavePluginJsonConfig(PartyPluginName, pluginConfig);

        if (changed)
        {
            ApplyLivePartySettingsFromConfig();
            RefreshPartyPluginRuntime();
            EventManager.FireEvent("OnSavePlayerConfig");
        }

        return changed;
    }

    private static Dictionary<string, object?> BuildPartyPluginState()
    {
        var settings = GetPartySettingsSnapshot();
        var members = new List<Dictionary<string, object?>>();
        var party = Game.Party;

        if (party?.Members != null)
        {
            foreach (var member in party.Members.Where(member => member != null))
            {
                var hpPercent = ((member.HealthMana >> 4) & 0x0F) * 10;
                var mpPercent = (member.HealthMana & 0x0F) * 10;

                hpPercent = Math.Clamp(hpPercent, 0, 100);
                mpPercent = Math.Clamp(mpPercent, 0, 100);

                var positionText = $"{member.Position.X:0.0}, {member.Position.Y:0.0}";

                members.Add(new Dictionary<string, object?>
                {
                    ["memberId"] = member.MemberId,
                    ["name"] = member.Name ?? string.Empty,
                    ["level"] = member.Level,
                    ["guild"] = member.Guild ?? string.Empty,
                    ["hpPercent"] = hpPercent,
                    ["mpPercent"] = mpPercent,
                    ["hpMp"] = $"{hpPercent}/{mpPercent}",
                    ["position"] = positionText
                });
            }
        }

        var buffCatalog = BuildPartyBuffCatalog(PlayerConfig.Get("UBot.Party.Buff.HideLowLevelSkills", false));
        var assignments = ParsePartyBuffAssignments(PlayerConfig.GetArray<string>("UBot.Party.Buff.Assignments"));
        var memberBuffs = assignments
            .Select(assignment => new Dictionary<string, object?>
            {
                ["name"] = assignment.Name,
                ["group"] = assignment.Group,
                ["buffs"] = assignment.Buffs.Cast<object?>().ToList()
            })
            .Cast<object?>()
            .ToList();

        var pluginConfig = LoadPluginJsonConfig(PartyPluginName);
        pluginConfig.TryGetValue("matchingResults", out var matchingResultsRaw);
        var matchingResults = matchingResultsRaw as IList ?? new List<object?>();

        return new Dictionary<string, object?>
        {
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["isInParty"] = party?.IsInParty == true,
            ["isLeader"] = party?.IsLeader == true,
            ["leaderName"] = party?.Leader?.Name ?? "Not in a party",
            ["canInvite"] = party?.CanInvite == true,
            ["expAutoShare"] = settings.ExperienceAutoShare,
            ["itemAutoShare"] = settings.ItemAutoShare,
            ["allowInvitations"] = settings.AllowInvitation,
            ["members"] = members.Cast<object?>().ToList(),
            ["buffCatalog"] = buffCatalog.Cast<object?>().ToList(),
            ["memberBuffs"] = memberBuffs,
            ["matchingResults"] = matchingResults.Cast<object?>().ToList()
        };
    }

    private static List<SkillInfo> CollectKnownAndAbilitySkills()
    {
        var result = new Dictionary<uint, SkillInfo>();
        var knownSkills = Game.Player?.Skills?.KnownSkills;
        if (knownSkills != null)
        {
            foreach (var known in knownSkills)
            {
                if (known?.Record == null || result.ContainsKey(known.Id))
                    continue;

                result[known.Id] = known;
            }
        }

        if (Game.Player != null && Game.Player.TryGetAbilitySkills(out var abilitySkills))
        {
            foreach (var ability in abilitySkills)
            {
                if (ability?.Record == null || result.ContainsKey(ability.Id))
                    continue;

                result[ability.Id] = ability;
            }
        }

        return result.Values.ToList();
    }
    internal Dictionary<string, object?> BuildConfig() => BuildPartyPluginConfig();
    internal bool ApplyPatch(Dictionary<string, object?> patch) => ApplyPartyPluginPatch(patch);
    internal Dictionary<string, object?> BuildState() => BuildPartyPluginState();
}

