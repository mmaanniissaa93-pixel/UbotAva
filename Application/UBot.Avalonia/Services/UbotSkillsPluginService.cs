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

internal sealed class UbotSkillsPluginService : UbotServiceBase
{
    private static Dictionary<string, object?> BuildSkillsPluginConfig()
    {
        var config = LoadPluginJsonConfig(SkillsPluginName);
        config["enableAttacks"] = PlayerConfig.Get("UBot.Desktop.Skills.EnableAttacks", true);
        config["enableBuffs"] = PlayerConfig.Get("UBot.Desktop.Skills.EnableBuffs", true);
        config["attackTypeIndex"] = Math.Clamp(
            PlayerConfig.Get("UBot.Desktop.Skills.AttackTypeIndex", 0),
            0,
            AttackRarityByIndex.Length - 1);
        config["noAttack"] = PlayerConfig.Get("UBot.Skills.checkBoxNoAttack", false);
        config["useSkillsInOrder"] = PlayerConfig.Get("UBot.Skills.checkUseSkillsInOrder", false);
        config["useDefaultAttack"] = PlayerConfig.Get("UBot.Skills.checkUseDefaultAttack", true);
        config["useTeleportSkill"] = PlayerConfig.Get("UBot.Skills.checkUseTeleportSkill", false);
        config["castBuffsInTowns"] = PlayerConfig.Get("UBot.Skills.checkCastBuffsInTowns", false);
        config["castBuffsDuringWalkBack"] = PlayerConfig.Get("UBot.Skills.checkCastBuffsDuringWalkBack", true);
        config["castBuffsBetweenAttacks"] = PlayerConfig.Get("UBot.Skills.checkCastBuffsBetweenAttacks", false);
        config["acceptResurrection"] = PlayerConfig.Get("UBot.Skills.checkAcceptResurrection", false);
        config["resurrectParty"] = PlayerConfig.Get("UBot.Skills.checkResurrectParty", false);
        config["resDelay"] = Math.Clamp(PlayerConfig.Get("UBot.Skills.numResDelay", 120), 1, 3600);
        config["resRadius"] = Math.Clamp(PlayerConfig.Get("UBot.Skills.numResRadius", 100), 1, 500);
        config["learnMastery"] = PlayerConfig.Get("UBot.Skills.checkLearnMastery", false);
        config["learnMasteryBotStopped"] = PlayerConfig.Get("UBot.Skills.checkLearnMasteryBotStopped", false);
        config["masteryGap"] = Math.Clamp(PlayerConfig.Get("UBot.Skills.numMasteryGap", 0), 0, 120);
        config["warlockMode"] = PlayerConfig.Get("UBot.Skills.checkWarlockMode", false);
        config["imbueSkillId"] = RedirectIdIfPossible(PlayerConfig.Get("UBot.Desktop.Skills.ImbueSkillId", 0U));
        config["resurrectionSkillId"] = RedirectIdIfPossible(PlayerConfig.Get("UBot.Skills.ResurrectionSkill", 0U));
        config["teleportSkillId"] = RedirectIdIfPossible(PlayerConfig.Get("UBot.Skills.TeleportSkill", 0U));
        config["selectedMasteryId"] = PlayerConfig.Get("UBot.Skills.selectedMastery", 0U);

        for (var i = 0; i < AttackRarityByIndex.Length; i++)
            config[$"attackSkills_{i}"] = PlayerConfig.GetArray<uint>($"UBot.Skills.Attacks_{i}")
                .Select(RedirectIdIfPossible)
                .Distinct().ToList();

        config["buffSkills"] = PlayerConfig.GetArray<uint>("UBot.Skills.Buffs")
            .Select(RedirectIdIfPossible)
            .Distinct().ToList();
        config["skillCatalog"] = BuildSkillCatalog();
        config["masteryCatalog"] = BuildMasteryCatalog();
        config["activeBuffs"] = BuildActiveBuffSnapshot();
        return config;
    }

    private sealed class ItemsShoppingTarget
    {
        public string ShopCodeName { get; set; } = string.Empty;
        public string ItemCodeName { get; set; } = string.Empty;
        public int Amount { get; set; }
    }

    private static bool ApplySkillsPluginPatch(Dictionary<string, object?> patch)
    {
        var changed = false;
        changed |= SetPlayerBool("UBot.Desktop.Skills.EnableAttacks", patch, "enableAttacks");
        changed |= SetPlayerBool("UBot.Desktop.Skills.EnableBuffs", patch, "enableBuffs");
        changed |= SetPlayerInt("UBot.Desktop.Skills.AttackTypeIndex", patch, "attackTypeIndex", 0, AttackRarityByIndex.Length - 1);
        changed |= SetPlayerBool("UBot.Skills.checkBoxNoAttack", patch, "noAttack");
        changed |= SetPlayerBool("UBot.Skills.checkUseSkillsInOrder", patch, "useSkillsInOrder");
        changed |= SetPlayerBool("UBot.Skills.checkUseDefaultAttack", patch, "useDefaultAttack");
        changed |= SetPlayerBool("UBot.Skills.checkUseTeleportSkill", patch, "useTeleportSkill");
        changed |= SetPlayerBool("UBot.Skills.checkCastBuffsInTowns", patch, "castBuffsInTowns");
        changed |= SetPlayerBool("UBot.Skills.checkCastBuffsDuringWalkBack", patch, "castBuffsDuringWalkBack");
        changed |= SetPlayerBool("UBot.Skills.checkCastBuffsBetweenAttacks", patch, "castBuffsBetweenAttacks");
        changed |= SetPlayerBool("UBot.Skills.checkAcceptResurrection", patch, "acceptResurrection");
        changed |= SetPlayerBool("UBot.Skills.checkResurrectParty", patch, "resurrectParty");
        changed |= SetPlayerInt("UBot.Skills.numResDelay", patch, "resDelay", 1, 3600);
        changed |= SetPlayerInt("UBot.Skills.numResRadius", patch, "resRadius", 1, 500);
        changed |= SetPlayerBool("UBot.Skills.checkLearnMastery", patch, "learnMastery");
        changed |= SetPlayerBool("UBot.Skills.checkLearnMasteryBotStopped", patch, "learnMasteryBotStopped");
        changed |= SetPlayerInt("UBot.Skills.numMasteryGap", patch, "masteryGap", 0, 120);
        changed |= SetPlayerBool("UBot.Skills.checkWarlockMode", patch, "warlockMode");

        if (TryGetUIntValue(patch, "imbueSkillId", out var imbueSkillId))
        {
            PlayerConfig.Set("UBot.Desktop.Skills.ImbueSkillId", imbueSkillId);
            changed = true;
        }

        if (TryGetUIntValue(patch, "resurrectionSkillId", out var resurrectionSkillId))
        {
            PlayerConfig.Set("UBot.Skills.ResurrectionSkill", resurrectionSkillId);
            changed = true;
        }

        if (TryGetUIntValue(patch, "teleportSkillId", out var teleportSkillId))
        {
            PlayerConfig.Set("UBot.Skills.TeleportSkill", teleportSkillId);
            changed = true;
        }

        if (TryGetUIntValue(patch, "selectedMasteryId", out var masteryId))
        {
            PlayerConfig.Set("UBot.Skills.selectedMastery", masteryId);
            changed = true;
        }

        if (TryGetUIntListValue(patch, "buffSkills", out var buffs))
        {
            PlayerConfig.SetArray("UBot.Skills.Buffs", buffs);
            changed = true;
        }

        for (var i = 0; i < AttackRarityByIndex.Length; i++)
        {
            if (!TryGetUIntListValue(patch, $"attackSkills_{i}", out var attackSkills))
                continue;

            PlayerConfig.SetArray($"UBot.Skills.Attacks_{i}", attackSkills);
            changed = true;
        }

        if (changed)
            RefreshLiveSkillsFromConfig();

        return changed;
    }

    private static void RefreshLiveSkillsFromConfig()
    {
        if (Game.Player?.Skills == null || SkillManager.Skills == null || SkillManager.Buffs == null)
            return;

        Game.Player.TryGetAbilitySkills(out var abilitySkills);

        SkillManager.Buffs.Clear();
        foreach (var rarity in SkillManager.Skills.Keys.ToArray())
            SkillManager.Skills[rarity].Clear();

        var imbueSkillId = PlayerConfig.Get("UBot.Desktop.Skills.ImbueSkillId", 0U);
        SkillManager.ImbueSkill = ResolveSkillInfoById(imbueSkillId, abilitySkills);

        var resurrectionSkillId = PlayerConfig.Get("UBot.Skills.ResurrectionSkill", 0U);
        SkillManager.ResurrectionSkill = ResolveSkillInfoById(resurrectionSkillId, abilitySkills);

        var teleportSkillId = PlayerConfig.Get("UBot.Skills.TeleportSkill", 0U);
        SkillManager.TeleportSkill = ResolveSkillInfoById(teleportSkillId, abilitySkills);

        foreach (var buffId in PlayerConfig.GetArray<uint>("UBot.Skills.Buffs").Distinct())
        {
            var skill = ResolveSkillInfoById(buffId, abilitySkills);
            if (skill != null)
                SkillManager.Buffs.Add(skill);
        }

        for (var i = 0; i < AttackRarityByIndex.Length; i++)
        {
            var rarity = AttackRarityByIndex[i];
            foreach (var attackSkillId in PlayerConfig.GetArray<uint>($"UBot.Skills.Attacks_{i}").Distinct())
            {
                var skill = ResolveSkillInfoById(attackSkillId, abilitySkills);
                if (skill != null)
                    SkillManager.Skills[rarity].Add(skill);
            }
        }
    }

    private static SkillInfo? ResolveSkillInfoById(uint skillId, List<SkillInfo> abilitySkills)
    {
        if (skillId == 0)
            return null;

        var knownSkill = Game.Player?.Skills?.GetSkillInfoById(skillId);
        if (knownSkill != null)
            return knownSkill;

        return abilitySkills?.FirstOrDefault(skill => skill.Id == skillId);
    }

    private static Dictionary<string, object?> BuildSkillsPluginState()
    {
        var state = new Dictionary<string, object?>
        {
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["playerReady"] = Game.Player != null,
            ["skillCatalog"] = BuildSkillCatalog(),
            ["masteryCatalog"] = BuildMasteryCatalog(),
            ["activeBuffs"] = BuildActiveBuffSnapshot()
        };

        if (Game.Player != null)
        {
            state["imbueSkillId"] = RedirectIdIfPossible(PlayerConfig.Get("UBot.Desktop.Skills.ImbueSkillId", 0U));
            state["resurrectionSkillId"] = RedirectIdIfPossible(PlayerConfig.Get("UBot.Skills.ResurrectionSkill", 0U));
            state["teleportSkillId"] = RedirectIdIfPossible(PlayerConfig.Get("UBot.Skills.TeleportSkill", 0U));

            for (var i = 0; i < AttackRarityByIndex.Length; i++)
                state[$"attackSkills_{i}"] = PlayerConfig.GetArray<uint>($"UBot.Skills.Attacks_{i}")
                    .Select(RedirectIdIfPossible)
                    .Distinct().ToList();

            state["buffSkills"] = PlayerConfig.GetArray<uint>("UBot.Skills.Buffs")
                .Select(RedirectIdIfPossible)
                .Distinct().ToList();
        }

        return state;
    }

    private static List<Dictionary<string, object?>> BuildSkillCatalog()
    {
        var entries = new List<Dictionary<string, object?>>();
        var seenIds = new HashSet<uint>();

        // 1. Current known/ability skills
        foreach (var skill in CollectKnownAndAbilitySkills())
        {
            if (seenIds.Add(skill.Id))
                entries.Add(MapSkillToEntry(skill.Id, skill.Record, skill, true));
        }

        // 2. Referenced skills from config (to keep names valid during upgrade/sync)
        foreach (var id in GetReferencedSkillIds())
        {
            if (id != 0 && seenIds.Add(id))
            {
                var record = Game.ReferenceManager?.GetRefSkill(id);
                if (record != null)
                    entries.Add(MapSkillToEntry(id, record, null, false));
            }
        }

        return entries
            .GroupBy(e => new
            {
                Name = e["name"]?.ToString(),
                GroupId = GetGroupIdFromEntry(e)
            })
            .Select(g => g.OrderByDescending(e => GetIsLearnedFromEntry(e)).First())
            .OrderBy(row => row.TryGetValue("name", out var n) ? n?.ToString() : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.TryGetValue("id", out var id) && id is uint u ? u : 0)
            .ToList();
    }

    private static HashSet<uint> GetReferencedSkillIds()
    {
        var ids = new HashSet<uint>();
        ids.Add(PlayerConfig.Get("UBot.Desktop.Skills.ImbueSkillId", 0U));
        ids.Add(PlayerConfig.Get("UBot.Skills.ResurrectionSkill", 0U));
        ids.Add(PlayerConfig.Get("UBot.Skills.TeleportSkill", 0U));

        for (var i = 0; i < AttackRarityByIndex.Length; i++)
        {
            foreach (var id in PlayerConfig.GetArray<uint>($"UBot.Skills.Attacks_{i}"))
                ids.Add(id);
        }

        foreach (var id in PlayerConfig.GetArray<uint>("UBot.Skills.Buffs"))
            ids.Add(id);

        return ids;
    }

    private static Dictionary<string, object?> MapSkillToEntry(uint id, RefSkill record, SkillInfo? skill, bool isLearned)
    {
        var name = record.GetRealName();
        if (string.IsNullOrWhiteSpace(name))
            name = record.Basic_Code;
        if (string.IsNullOrWhiteSpace(name))
            name = $"Skill {id}";

        var isPassive = record.Basic_Activity == 0;
        var isAttack = record.Params.Contains(6386804);
        var isImbue = record.Basic_Activity == 1 && isAttack;

        bool isLowLevel = false;
        if (skill != null)
        {
            try { isLowLevel = skill.IsLowLevel(); } catch { }
        }

        return new Dictionary<string, object?>
        {
            ["id"] = id,
            ["name"] = name,
            ["isPassive"] = isPassive,
            ["isAttack"] = isAttack,
            ["isBuff"] = !isPassive && !isAttack,
            ["isImbue"] = isImbue,
            ["isLowLevel"] = isLowLevel,
            ["groupId"] = record.GroupID,
            ["basicGroup"] = record.Basic_Group,
            ["isLearned"] = isLearned,
            ["icon"] = record.UI_IconFile
        };
    }

    private static List<Dictionary<string, object?>> BuildMasteryCatalog()
    {
        var result = new List<Dictionary<string, object?>>();
        var masteries = Game.Player?.Skills?.Masteries;
        if (masteries == null)
            return result;

        foreach (var mastery in masteries)
        {
            var record = mastery.Record;
            if (record == null)
                continue;

            var name = record.Name;
            if (string.IsNullOrWhiteSpace(name))
                name = $"Mastery {mastery.Id}";

            result.Add(new Dictionary<string, object?>
            {
                ["id"] = mastery.Id,
                ["name"] = name,
                ["level"] = mastery.Level
            });
        }

        return result
            .OrderBy(row => row.TryGetValue("name", out var n) ? n?.ToString() : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<Dictionary<string, object?>> BuildActiveBuffSnapshot()
    {
        var result = new List<Dictionary<string, object?>>();
        var buffs = Game.Player?.State?.ActiveBuffs;
        if (buffs == null)
            return result;

        foreach (var buff in buffs)
        {
            var record = buff.Record;
            if (record == null)
                continue;

            var name = record.GetRealName();
            if (string.IsNullOrWhiteSpace(name))
                name = record.Basic_Code;
            if (string.IsNullOrWhiteSpace(name))
                name = $"Buff {buff.Id}";

            result.Add(new Dictionary<string, object?>
            {
                ["id"] = buff.Id,
                ["token"] = buff.Token,
                ["name"] = name,
                ["remainingMs"] = buff.RemainingMilliseconds,
                ["remainingPercent"] = Math.Round(buff.RemainingPercent * 100d, 2)
            });
        }

        return result
            .OrderBy(row => row.TryGetValue("name", out var n) ? n?.ToString() : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();
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
    internal Dictionary<string, object?> BuildConfig() => BuildSkillsPluginConfig();
    internal bool ApplyPatch(Dictionary<string, object?> patch) => ApplySkillsPluginPatch(patch);
    internal Dictionary<string, object?> BuildState() => BuildSkillsPluginState();

    private static uint RedirectIdIfPossible(uint skillId)
    {
        if (skillId == 0) return 0;
        var info = Game.Player?.Skills?.GetSkillInfoById(skillId);
        return info?.Id ?? skillId;
    }

    private static int GetGroupIdFromEntry(Dictionary<string, object?> entry)
    {
        if (!entry.TryGetValue("groupId", out var value) || value == null)
            return 0;

        return value switch
        {
            int i => i,
            long l => (int)l,
            short s => s,
            byte b => b,
            _ => 0
        };
    }

    private static bool GetIsLearnedFromEntry(Dictionary<string, object?> entry)
    {
        if (!entry.TryGetValue("isLearned", out var value) || value == null)
            return false;

        return value switch
        {
            bool b => b,
            int i => i != 0,
            long l => l != 0,
            string s => s.Equals("true", StringComparison.OrdinalIgnoreCase) || s == "1",
            _ => false
        };
    }
}


