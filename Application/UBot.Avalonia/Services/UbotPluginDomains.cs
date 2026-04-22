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

internal sealed class UbotPluginConfigHelpers : UbotServiceBase
{
    internal static Dictionary<string, object?> LoadRawConfig(string pluginId)
    {
        return LoadPluginJsonConfig(pluginId);
    }

    internal static bool ApplyGenericPatch(string pluginId, Dictionary<string, object?> patch)
    {
        var current = LoadPluginJsonConfig(pluginId);
        foreach (var kv in patch)
            current[kv.Key] = kv.Value;

        SavePluginJsonConfig(pluginId, current);
        return true;
    }

    internal static bool SetGlobalBool(string key, object? value)
    {
        if (!TryConvertBool(value, out var parsed))
            return false;
        GlobalConfig.Set(key, parsed);
        return true;
    }

    internal static bool SetGlobalInt(string key, object? value, int min, int max)
    {
        if (!TryConvertInt(value, out var parsed))
            return false;
        GlobalConfig.Set(key, Math.Clamp(parsed, min, max));
        return true;
    }

    internal static bool SetGlobalString(string key, object? value)
    {
        if (value == null)
            return false;
        GlobalConfig.Set(key, value.ToString() ?? string.Empty);
        return true;
    }

    internal static bool SetPlayerBool(string targetKey, IDictionary<string, object?> patch, string patchKey)
    {
        if (!patch.TryGetValue(patchKey, out var value) || !TryConvertBool(value, out var parsed))
            return false;
        PlayerConfig.Set(targetKey, parsed);
        return true;
    }

    internal static bool SetPlayerInt(string targetKey, IDictionary<string, object?> patch, string patchKey, int min, int max)
    {
        if (!patch.TryGetValue(patchKey, out var value) || !TryConvertInt(value, out var parsed))
            return false;
        PlayerConfig.Set(targetKey, Math.Clamp(parsed, min, max));
        return true;
    }

    internal static bool SetPlayerFloat(string targetKey, IDictionary<string, object?> patch, string patchKey)
    {
        if (!patch.TryGetValue(patchKey, out var value) || !TryConvertDouble(value, out var parsed))
            return false;
        PlayerConfig.Set(targetKey, (float)parsed);
        return true;
    }

    internal static bool SetPlayerString(string targetKey, IDictionary<string, object?> patch, string patchKey)
    {
        if (!patch.TryGetValue(patchKey, out var value))
            return false;
        PlayerConfig.Set(targetKey, value?.ToString() ?? string.Empty);
        return true;
    }
}

internal sealed class UbotGeneralPluginService : UbotServiceBase
{
    private readonly UbotAutoLoginService _autoLoginService;
    internal UbotGeneralPluginService(UbotAutoLoginService autoLoginService)
    {
        _autoLoginService = autoLoginService;
    }
    private Dictionary<string, object?> BuildGeneralPluginConfig()
    {
        var config = LoadPluginJsonConfig("UBot.General");
        var savedAccounts = _autoLoginService.GetAutoLoginAccountsAsync().GetAwaiter().GetResult();
        config["enableAutomatedLogin"] = GlobalConfig.Get("UBot.General.EnableAutomatedLogin", false);
        config["autoLoginAccount"] = GlobalConfig.Get("UBot.General.AutoLoginAccountUsername", string.Empty);
        config["selectedCharacter"] = GlobalConfig.Get("UBot.General.AutoLoginCharacter", string.Empty);
        config["autoCharSelect"] = GlobalConfig.Get("UBot.General.CharacterAutoSelect", false);
        config["enableLoginDelay"] = GlobalConfig.Get("UBot.General.EnableLoginDelay", false);
        config["loginDelay"] = GlobalConfig.Get("UBot.General.LoginDelay", 10);
        config["enableWaitAfterDc"] = GlobalConfig.Get("UBot.General.EnableWaitAfterDC", false);
        config["waitAfterDc"] = GlobalConfig.Get("UBot.General.WaitAfterDC", 3);
        config["enableStaticCaptcha"] = GlobalConfig.Get("UBot.General.EnableStaticCaptcha", false);
        config["staticCaptcha"] = GlobalConfig.Get("UBot.General.StaticCaptcha", string.Empty);
        config["autoStartBot"] = GlobalConfig.Get("UBot.General.StartBot", false);
        config["useReturnScroll"] = GlobalConfig.Get("UBot.General.UseReturnScroll", false);
        config["autoHideClient"] = GlobalConfig.Get("UBot.General.HideOnStartClient", false);
        config["characterAutoSelectFirst"] = GlobalConfig.Get("UBot.General.CharacterAutoSelectFirst", false);
        config["characterAutoSelectHigher"] = GlobalConfig.Get("UBot.General.CharacterAutoSelectHigher", false);
        config["stayConnectedAfterClientExit"] = GlobalConfig.Get("UBot.General.StayConnected", false);
        config["moveToTrayOnMinimize"] = GlobalConfig.Get("UBot.General.TrayWhenMinimize", false);
        config["autoHidePendingWindow"] = GlobalConfig.Get("UBot.General.AutoHidePendingWindow", false);
        config["enablePendingQueueLogs"] = GlobalConfig.Get("UBot.General.PendingEnableQueueLogs", false);
        config["enableQueueNotification"] = GlobalConfig.Get("UBot.General.EnableQueueNotification", false);
        config["queuePeopleLeft"] = GlobalConfig.Get("UBot.General.QueueLeft", 30);
        config["sroExecutable"] = Path.Combine(
            GlobalConfig.Get("UBot.SilkroadDirectory", string.Empty),
            GlobalConfig.Get("UBot.SilkroadExecutable", string.Empty));

        var accounts = savedAccounts.Select(x => x.Username).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var characterMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var account in savedAccounts)
        {
            if (string.IsNullOrWhiteSpace(account.Username))
                continue;

            characterMap[account.Username] = account.Characters
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var selectedAccount = GlobalConfig.Get("UBot.General.AutoLoginAccountUsername", string.Empty);
        characterMap.TryGetValue(selectedAccount, out var selectedCharacters);
        var selectedCharacter = GlobalConfig.Get("UBot.General.AutoLoginCharacter", string.Empty);

        if (string.IsNullOrWhiteSpace(selectedCharacter))
        {
            var preferredCharacter = savedAccounts
                .FirstOrDefault(x => string.Equals(x.Username, selectedAccount, StringComparison.OrdinalIgnoreCase))
                ?.SelectedCharacter;
            if (!string.IsNullOrWhiteSpace(preferredCharacter))
                selectedCharacter = preferredCharacter;
        }

        config["autoLoginAccounts"] = accounts;
        config["autoLoginCharacters"] = selectedCharacters ?? new List<string>();
        config["autoLoginCharacterMap"] = characterMap;
        config["selectedCharacter"] = selectedCharacter;
        return config;
    }

    private bool ApplyGeneralPluginPatch(Dictionary<string, object?> patch)
    {
        var changed = false;
        var selectedCharacterPatched = false;
        string? selectedCharacterValue = null;
        string? selectedAccountValue = null;
        foreach (var kv in patch)
        {
            switch (kv.Key)
            {
                case "sroExecutable":
                    if (kv.Value is string sroPath && !string.IsNullOrWhiteSpace(sroPath))
                    {
                        var path = sroPath.Trim().Trim('"');
                        if (File.Exists(path))
                        {
                            GlobalConfig.Set("UBot.SilkroadDirectory", Path.GetDirectoryName(path) ?? string.Empty);
                            GlobalConfig.Set("UBot.SilkroadExecutable", Path.GetFileName(path));
                            changed = true;
                        }
                    }
                    break;
                case "enableAutomatedLogin":
                    changed |= SetGlobalBool("UBot.General.EnableAutomatedLogin", kv.Value);
                    break;
                case "autoLoginAccount":
                    changed |= SetGlobalString("UBot.General.AutoLoginAccountUsername", kv.Value);
                    selectedAccountValue = kv.Value?.ToString()?.Trim();
                    break;
                case "selectedCharacter":
                    changed |= SetGlobalString("UBot.General.AutoLoginCharacter", kv.Value);
                    selectedCharacterPatched = true;
                    selectedCharacterValue = kv.Value?.ToString()?.Trim() ?? string.Empty;
                    break;
                case "autoCharSelect":
                    changed |= SetGlobalBool("UBot.General.CharacterAutoSelect", kv.Value);
                    break;
                case "enableLoginDelay":
                    changed |= SetGlobalBool("UBot.General.EnableLoginDelay", kv.Value);
                    break;
                case "loginDelay":
                    changed |= SetGlobalInt("UBot.General.LoginDelay", kv.Value, 0, 3600);
                    break;
                case "enableWaitAfterDc":
                    changed |= SetGlobalBool("UBot.General.EnableWaitAfterDC", kv.Value);
                    break;
                case "waitAfterDc":
                    changed |= SetGlobalInt("UBot.General.WaitAfterDC", kv.Value, 0, 3600);
                    break;
                case "enableStaticCaptcha":
                    changed |= SetGlobalBool("UBot.General.EnableStaticCaptcha", kv.Value);
                    break;
                case "staticCaptcha":
                    changed |= SetGlobalString("UBot.General.StaticCaptcha", kv.Value);
                    break;
                case "autoStartBot":
                    changed |= SetGlobalBool("UBot.General.StartBot", kv.Value);
                    break;
                case "useReturnScroll":
                    changed |= SetGlobalBool("UBot.General.UseReturnScroll", kv.Value);
                    break;
                case "autoHideClient":
                    changed |= SetGlobalBool("UBot.General.HideOnStartClient", kv.Value);
                    break;
                case "characterAutoSelectFirst":
                    changed |= SetGlobalBool("UBot.General.CharacterAutoSelectFirst", kv.Value);
                    break;
                case "characterAutoSelectHigher":
                    changed |= SetGlobalBool("UBot.General.CharacterAutoSelectHigher", kv.Value);
                    break;
                case "stayConnectedAfterClientExit":
                    changed |= SetGlobalBool("UBot.General.StayConnected", kv.Value);
                    break;
                case "moveToTrayOnMinimize":
                    changed |= SetGlobalBool("UBot.General.TrayWhenMinimize", kv.Value);
                    break;
                case "autoHidePendingWindow":
                    changed |= SetGlobalBool("UBot.General.AutoHidePendingWindow", kv.Value);
                    break;
                case "enablePendingQueueLogs":
                    changed |= SetGlobalBool("UBot.General.PendingEnableQueueLogs", kv.Value);
                    break;
                case "enableQueueNotification":
                    changed |= SetGlobalBool("UBot.General.EnableQueueNotification", kv.Value);
                    break;
                case "queuePeopleLeft":
                    changed |= SetGlobalInt("UBot.General.QueueLeft", kv.Value, 0, 999);
                    break;
                default:
                    break;
            }
        }

        if (selectedCharacterPatched)
        {
            if (string.IsNullOrWhiteSpace(selectedAccountValue))
                selectedAccountValue = GlobalConfig.Get("UBot.General.AutoLoginAccountUsername", string.Empty);

            changed |= _autoLoginService.UpdateSelectedCharacterForAccount(selectedAccountValue, selectedCharacterValue);
        }

        return changed;
    }

    internal Dictionary<string, object?> BuildConfig() => BuildGeneralPluginConfig();
    internal bool ApplyPatch(Dictionary<string, object?> patch) => ApplyGeneralPluginPatch(patch);
}

internal sealed class UbotTrainingBotbaseService : UbotServiceBase
{
    private static Dictionary<string, object?> BuildTrainingBotbaseConfig()
    {
        return new Dictionary<string, object?>
        {
            ["areaRegion"] = (int)PlayerConfig.Get<ushort>("UBot.Area.Region"),
            ["areaX"] = PlayerConfig.Get("UBot.Area.X", 0f),
            ["areaY"] = PlayerConfig.Get("UBot.Area.Y", 0f),
            ["areaZ"] = PlayerConfig.Get("UBot.Area.Z", 0f),
            ["areaRadius"] = Math.Clamp(PlayerConfig.Get("UBot.Area.Radius", 50), 5, 100),
            ["walkScript"] = PlayerConfig.Get("UBot.Walkback.File", string.Empty),
            ["useMount"] = PlayerConfig.Get("UBot.Training.checkUseMount", true),
            ["castBuffs"] = PlayerConfig.Get("UBot.Training.checkCastBuffs", true),
            ["useSpeedDrug"] = PlayerConfig.Get("UBot.Training.checkUseSpeedDrug", true),
            ["useReverse"] = PlayerConfig.Get("UBot.Training.checkBoxUseReverse", false),
            ["berserkWhenFull"] = PlayerConfig.Get("UBot.Training.checkBerzerkWhenFull", false),
            ["berserkByMonsterAmount"] = PlayerConfig.Get("UBot.Training.checkBerzerkMonsterAmount", false),
            ["berserkMonsterAmount"] = PlayerConfig.Get("UBot.Training.numBerzerkMonsterAmount", 5),
            ["berserkByAvoidance"] = PlayerConfig.Get("UBot.Training.checkBerzerkAvoidance", false),
            ["berserkByMonsterRarity"] = PlayerConfig.Get("UBot.Training.checkBerserkOnMonsterRarity", false),
            ["ignoreDimensionPillar"] = PlayerConfig.Get("UBot.Training.checkBoxDimensionPillar", false),
            ["attackWeakerFirst"] = PlayerConfig.Get("UBot.Training.checkAttackWeakerFirst", false),
            ["dontFollowMobs"] = PlayerConfig.Get("UBot.Training.checkBoxDontFollowMobs", false),
            ["avoidanceList"] = PlayerConfig.GetArray<string>("UBot.Avoidance.Avoid").ToList(),
            ["preferList"] = PlayerConfig.GetArray<string>("UBot.Avoidance.Prefer").ToList(),
            ["berserkList"] = PlayerConfig.GetArray<string>("UBot.Avoidance.Berserk").ToList()
        };
    }

    private static bool ApplyTrainingBotbasePatch(Dictionary<string, object?> patch)
    {
        var changed = false;
        changed |= SetPlayerInt("UBot.Area.Region", patch, "areaRegion", 0, ushort.MaxValue);
        changed |= SetPlayerFloat("UBot.Area.X", patch, "areaX");
        changed |= SetPlayerFloat("UBot.Area.Y", patch, "areaY");
        changed |= SetPlayerFloat("UBot.Area.Z", patch, "areaZ");
        changed |= SetPlayerInt("UBot.Area.Radius", patch, "areaRadius", 5, 100);
        changed |= SetPlayerString("UBot.Walkback.File", patch, "walkScript");
        changed |= SetPlayerBool("UBot.Training.checkUseMount", patch, "useMount");
        changed |= SetPlayerBool("UBot.Training.checkCastBuffs", patch, "castBuffs");
        changed |= SetPlayerBool("UBot.Training.checkUseSpeedDrug", patch, "useSpeedDrug");
        changed |= SetPlayerBool("UBot.Training.checkBoxUseReverse", patch, "useReverse");
        changed |= SetPlayerBool("UBot.Training.checkBerzerkWhenFull", patch, "berserkWhenFull");
        changed |= SetPlayerBool("UBot.Training.checkBerzerkMonsterAmount", patch, "berserkByMonsterAmount");
        changed |= SetPlayerInt("UBot.Training.numBerzerkMonsterAmount", patch, "berserkMonsterAmount", 1, 20);
        changed |= SetPlayerBool("UBot.Training.checkBerzerkAvoidance", patch, "berserkByAvoidance");
        changed |= SetPlayerBool("UBot.Training.checkBerserkOnMonsterRarity", patch, "berserkByMonsterRarity");
        changed |= SetPlayerBool("UBot.Training.checkBoxDimensionPillar", patch, "ignoreDimensionPillar");
        changed |= SetPlayerBool("UBot.Training.checkAttackWeakerFirst", patch, "attackWeakerFirst");
        changed |= SetPlayerBool("UBot.Training.checkBoxDontFollowMobs", patch, "dontFollowMobs");

        if (TryGetStringListValue(patch, "avoidanceList", out var avoidance))
        {
            PlayerConfig.SetArray("UBot.Avoidance.Avoid", avoidance.ToArray());
            changed = true;
        }

        if (TryGetStringListValue(patch, "preferList", out var prefer))
        {
            PlayerConfig.SetArray("UBot.Avoidance.Prefer", prefer.ToArray());
            changed = true;
        }

        if (TryGetStringListValue(patch, "berserkList", out var berserk))
        {
            PlayerConfig.SetArray("UBot.Avoidance.Berserk", berserk.ToArray());
            changed = true;
        }

        if (changed)
            EventManager.FireEvent("OnSavePlayerConfig");

        return changed;
    }

    internal Dictionary<string, object?> BuildConfig() => BuildTrainingBotbaseConfig();
    internal bool ApplyPatch(Dictionary<string, object?> patch) => ApplyTrainingBotbasePatch(patch);
}

internal sealed class UbotLureBotbaseService : UbotServiceBase
{
    private static Dictionary<string, object?> BuildLureBotbaseConfig()
    {
        var region = PlayerConfig.Get<ushort>("UBot.Lure.Area.Region");
        var x = PlayerConfig.Get("UBot.Lure.Area.X", 0f);
        var y = PlayerConfig.Get("UBot.Lure.Area.Y", 0f);
        var z = PlayerConfig.Get("UBot.Lure.Area.Z", 0f);
        var selectedMonsterType = PlayerConfig.GetEnum("UBot.Lure.SelectedMonsterType", MonsterRarity.General);
        var useScript = PlayerConfig.Get("UBot.Lure.UseScript", false);
        var stayAtCenter = PlayerConfig.Get("UBot.Lure.StayAtCenter", false);

        var lureMode = useScript
            ? "useScript"
            : stayAtCenter
                ? "stayAtCenter"
                : "walkRandomly";

        return new Dictionary<string, object?>
        {
            ["lureLocationScript"] = PlayerConfig.Get("UBot.Lure.Walkback.File", string.Empty),
            ["lureCenterRegion"] = (int)region,
            ["lureCenterXSector"] = (int)((CoreRegion)region).X,
            ["lureCenterYSector"] = (int)((CoreRegion)region).Y,
            ["lureCenterX"] = Math.Round(x, 2),
            ["lureCenterY"] = Math.Round(y, 2),
            ["lureCenterZ"] = Math.Round(z, 2),
            ["lureRadius"] = Math.Clamp(PlayerConfig.Get("UBot.Lure.Area.Radius", 20), 1, 200),
            ["lureMode"] = lureMode,
            ["lureScriptPath"] = PlayerConfig.Get("UBot.Lure.SelectedScriptPath", string.Empty),
            ["lureStayAtCenterForEnabled"] = PlayerConfig.Get("UBot.Lure.StayAtCenterFor", false),
            ["lureStayAtCenterSeconds"] = Math.Clamp(PlayerConfig.Get("UBot.Lure.StayAtCenterForSeconds", 10), 0, 3600),
            ["lureCastHowlingShout"] = PlayerConfig.Get("UBot.Lure.UseHowlingShout", false),
            ["lureDontCastNearCenter"] = PlayerConfig.Get("UBot.Lure.NoHowlingAtCenter", true),
            ["lureUseNormalAttackSwitch"] = PlayerConfig.Get("UBot.Lure.UseNormalAttack", false),
            ["lureUseAttackSkillSwitch"] = PlayerConfig.Get("UBot.Lure.UseAttackingSkills", false),
            ["lureStopOnDeadPartyMembersEnabled"] = PlayerConfig.Get("UBot.Lure.StopIfNumPartyMemberDead", false),
            ["lureStopDeadPartyMembers"] = Math.Clamp(PlayerConfig.Get("UBot.Lure.NumPartyMemberDead", 0), 0, 8),
            ["lureStopOnPartyMembersEnabled"] = PlayerConfig.Get("UBot.Lure.StopIfNumPartyMember", false),
            ["lureStopPartyMembersLe"] = Math.Clamp(PlayerConfig.Get("UBot.Lure.NumPartyMember", 0), 0, 8),
            ["lureStopOnPartyMembersOnSpotEnabled"] = PlayerConfig.Get("UBot.Lure.StopIfNumPartyMembersOnSpot", false),
            ["lureStopPartyMembersOnSpotLe"] = Math.Clamp(PlayerConfig.Get("UBot.Lure.NumPartyMembersOnSpot", 0), 0, 8),
            ["lureStopOnMonsterTypeEnabled"] = PlayerConfig.Get("UBot.Lure.StopIfNumMonsterType", false),
            ["lureMonsterType"] = FormatMonsterTypeToken(selectedMonsterType),
            ["lureStopMonsterCount"] = Math.Clamp(PlayerConfig.Get("UBot.Lure.NumMonsterType", 1), 0, 50)
        };
    }

    private static bool ApplyLureBotbasePatch(IBotbase botbase, Dictionary<string, object?> patch)
    {
        var changed = false;
        changed |= SetPlayerString("UBot.Lure.Walkback.File", patch, "lureLocationScript");
        changed |= SetPlayerInt("UBot.Lure.Area.Region", patch, "lureCenterRegion", 0, ushort.MaxValue);
        changed |= SetPlayerFloat("UBot.Lure.Area.X", patch, "lureCenterX");
        changed |= SetPlayerFloat("UBot.Lure.Area.Y", patch, "lureCenterY");
        changed |= SetPlayerFloat("UBot.Lure.Area.Z", patch, "lureCenterZ");
        changed |= SetPlayerInt("UBot.Lure.Area.Radius", patch, "lureRadius", 1, 200);
        changed |= SetPlayerString("UBot.Lure.SelectedScriptPath", patch, "lureScriptPath");
        changed |= SetPlayerBool("UBot.Lure.StayAtCenterFor", patch, "lureStayAtCenterForEnabled");
        changed |= SetPlayerInt("UBot.Lure.StayAtCenterForSeconds", patch, "lureStayAtCenterSeconds", 0, 3600);
        changed |= SetPlayerBool("UBot.Lure.UseHowlingShout", patch, "lureCastHowlingShout");
        changed |= SetPlayerBool("UBot.Lure.NoHowlingAtCenter", patch, "lureDontCastNearCenter");
        changed |= SetPlayerBool("UBot.Lure.UseNormalAttack", patch, "lureUseNormalAttackSwitch");
        changed |= SetPlayerBool("UBot.Lure.UseAttackingSkills", patch, "lureUseAttackSkillSwitch");
        changed |= SetPlayerBool("UBot.Lure.StopIfNumPartyMemberDead", patch, "lureStopOnDeadPartyMembersEnabled");
        changed |= SetPlayerInt("UBot.Lure.NumPartyMemberDead", patch, "lureStopDeadPartyMembers", 0, 8);
        changed |= SetPlayerBool("UBot.Lure.StopIfNumPartyMember", patch, "lureStopOnPartyMembersEnabled");
        changed |= SetPlayerInt("UBot.Lure.NumPartyMember", patch, "lureStopPartyMembersLe", 0, 8);
        changed |= SetPlayerBool("UBot.Lure.StopIfNumPartyMembersOnSpot", patch, "lureStopOnPartyMembersOnSpotEnabled");
        changed |= SetPlayerInt("UBot.Lure.NumPartyMembersOnSpot", patch, "lureStopPartyMembersOnSpotLe", 0, 8);
        changed |= SetPlayerBool("UBot.Lure.StopIfNumMonsterType", patch, "lureStopOnMonsterTypeEnabled");
        changed |= SetPlayerInt("UBot.Lure.NumMonsterType", patch, "lureStopMonsterCount", 0, 50);

        if (TryGetStringValue(patch, "lureMode", out var lureMode))
        {
            var mode = lureMode.Trim().ToLowerInvariant();
            var walkRandomly = mode != "stayatcenter" && mode != "usescript";
            var stayAtCenter = mode == "stayatcenter";
            var useScript = mode == "usescript";

            PlayerConfig.Set("UBot.Lure.WalkRandomly", walkRandomly);
            PlayerConfig.Set("UBot.Lure.StayAtCenter", stayAtCenter);
            PlayerConfig.Set("UBot.Lure.UseScript", useScript);
            changed = true;
        }

        if (patch.TryGetValue("lureMonsterType", out var lureMonsterTypeRaw) && lureMonsterTypeRaw != null)
        {
            var current = PlayerConfig.GetEnum("UBot.Lure.SelectedMonsterType", MonsterRarity.General);
            var next = current;

            if (lureMonsterTypeRaw is string token)
                next = ParseMonsterTypeToken(token, current);
            else if (TryConvertInt(lureMonsterTypeRaw, out var numericMonsterType))
                next = (MonsterRarity)Math.Clamp(numericMonsterType, byte.MinValue, byte.MaxValue);

            PlayerConfig.Set("UBot.Lure.SelectedMonsterType", (byte)next);
            changed = true;
        }

        if (changed)
            EventManager.FireEvent("OnSavePlayerConfig");

        return changed;
    }
    private static MonsterRarity ParseMonsterTypeToken(string value, MonsterRarity fallback = MonsterRarity.General)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;

        var normalized = value.Trim().ToLowerInvariant();
        return normalized switch
        {
            "general" => MonsterRarity.General,
            "champion" => MonsterRarity.Champion,
            "giant" => MonsterRarity.Giant,
            "generalparty" or "partygeneral" => MonsterRarity.GeneralParty,
            "championparty" or "partychampion" => MonsterRarity.ChampionParty,
            "giantparty" or "partygiant" => MonsterRarity.GiantParty,
            "elite" => MonsterRarity.Elite,
            "strong" or "elitestrong" => MonsterRarity.EliteStrong,
            "unique" => MonsterRarity.Unique,
            "event" => MonsterRarity.Event,
            _ => fallback
        };
    }

    private static string FormatMonsterTypeToken(MonsterRarity value)
    {
        return value switch
        {
            MonsterRarity.Champion => "Champion",
            MonsterRarity.Giant => "Giant",
            MonsterRarity.GeneralParty => "GeneralParty",
            MonsterRarity.ChampionParty => "ChampionParty",
            MonsterRarity.GiantParty => "GiantParty",
            MonsterRarity.Elite => "Elite",
            MonsterRarity.EliteStrong => "Strong",
            MonsterRarity.Unique => "Unique",
            MonsterRarity.Event => "Event",
            _ => "General"
        };
    }

    private static object BuildLureState(IBotbase botbase)
    {
        var area = botbase?.Area;
        var areaPosition = area?.Position ?? default;
        var playerPosition = Game.Player?.Position ?? default;

        return new Dictionary<string, object?>
        {
            ["selected"] = Kernel.Bot?.Botbase?.Name == botbase?.Name,
            ["centerRegion"] = (int)areaPosition.Region.Id,
            ["centerXSector"] = (int)areaPosition.Region.X,
            ["centerYSector"] = (int)areaPosition.Region.Y,
            ["centerX"] = Math.Round(areaPosition.XOffset, 2),
            ["centerY"] = Math.Round(areaPosition.YOffset, 2),
            ["centerZ"] = Math.Round(areaPosition.ZOffset, 2),
            ["radius"] = area?.Radius ?? Math.Clamp(PlayerConfig.Get("UBot.Lure.Area.Radius", 20), 1, 200),
            ["currentPosition"] = new Dictionary<string, object?>
            {
                ["region"] = (int)playerPosition.Region.Id,
                ["xSector"] = (int)playerPosition.Region.X,
                ["ySector"] = (int)playerPosition.Region.Y,
                ["x"] = Math.Round(playerPosition.XOffset, 2),
                ["y"] = Math.Round(playerPosition.YOffset, 2),
                ["z"] = Math.Round(playerPosition.ZOffset, 2)
            },
            ["hasCenter"] = areaPosition.Region.Id != 0 || areaPosition.XOffset != 0 || areaPosition.YOffset != 0
        };
    }

    internal Dictionary<string, object?> BuildConfig() => BuildLureBotbaseConfig();
    internal bool ApplyPatch(IBotbase botbase, Dictionary<string, object?> patch) => ApplyLureBotbasePatch(botbase, patch);
    internal object BuildState(IBotbase botbase) => BuildLureState(botbase);
}

internal sealed class UbotTradeBotbaseService : UbotServiceBase
{
    private sealed class TradeRouteListDefinition
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Scripts { get; set; } = new();
    }

    private static Dictionary<string, object?> BuildTradeBotbaseConfig()
    {
        var routeLists = LoadTradeRouteListsFromPlayerConfig();
        var selectedRouteListIndex = Math.Clamp(
            PlayerConfig.Get("UBot.Trade.SelectedRouteListIndex", 0),
            0,
            Math.Max(0, routeLists.Count - 1));

        return new Dictionary<string, object?>
        {
            ["tradeTracePlayerName"] = PlayerConfig.Get("UBot.Trade.TracePlayerName", string.Empty),
            ["tradeTracePlayer"] = PlayerConfig.Get("UBot.Trade.TracePlayer", false),
            ["tradeUseRouteScripts"] = PlayerConfig.Get("UBot.Trade.UseRouteScripts", true),
            ["tradeSelectedRouteListIndex"] = selectedRouteListIndex,
            ["tradeRouteLists"] = routeLists.Select(routeList => new Dictionary<string, object?>
            {
                ["name"] = routeList.Name,
                ["scripts"] = routeList.Scripts.Cast<object?>().ToList()
            }).Cast<object?>().ToList(),
            ["tradeRunTownScript"] = PlayerConfig.Get("UBot.Trade.RunTownScript", false),
            ["tradeWaitForHunter"] = PlayerConfig.Get("UBot.Trade.WaitForHunter", false),
            ["tradeAttackThiefPlayers"] = PlayerConfig.Get("UBot.Trade.AttackThiefPlayers", false),
            ["tradeAttackThiefNpcs"] = PlayerConfig.Get("UBot.Trade.AttackThiefNpcs", false),
            ["tradeCounterAttack"] = PlayerConfig.Get("UBot.Trade.CounterAttack", false),
            ["tradeProtectTransport"] = PlayerConfig.Get("UBot.Trade.ProtectTransport", false),
            ["tradeCastBuffs"] = PlayerConfig.Get("UBot.Trade.CastBuffs", false),
            ["tradeMountTransport"] = PlayerConfig.Get("UBot.Trade.MountTransport", false),
            ["tradeMaxTransportDistance"] = Math.Clamp(PlayerConfig.Get("UBot.Trade.MaxTransportDistance", 15), 1, 300),
            ["tradeSellGoods"] = PlayerConfig.Get("UBot.Trade.SellGoods", true),
            ["tradeBuyGoods"] = PlayerConfig.Get("UBot.Trade.BuyGoods", true),
            ["tradeBuyGoodsQuantity"] = Math.Max(0, PlayerConfig.Get("UBot.Trade.BuyGoodsQuantity", 0)),
            ["tradeRecorderScriptPath"] = PlayerConfig.Get("UBot.Desktop.Trade.RecorderScriptPath", string.Empty)
        };
    }

    private static bool ApplyTradeBotbasePatch(IBotbase botbase, Dictionary<string, object?> patch)
    {
        var changed = false;
        var selectedRouteListIndex = PlayerConfig.Get("UBot.Trade.SelectedRouteListIndex", 0);
        var routeLists = LoadTradeRouteListsFromPlayerConfig();
        var routeListsChanged = false;

        bool? tracePlayerToggle = null;
        bool? useRouteScriptsToggle = null;

        changed |= SetPlayerString("UBot.Trade.TracePlayerName", patch, "tradeTracePlayerName");
        changed |= SetPlayerBool("UBot.Trade.RunTownScript", patch, "tradeRunTownScript");
        changed |= SetPlayerBool("UBot.Trade.WaitForHunter", patch, "tradeWaitForHunter");
        changed |= SetPlayerBool("UBot.Trade.AttackThiefPlayers", patch, "tradeAttackThiefPlayers");
        changed |= SetPlayerBool("UBot.Trade.AttackThiefNpcs", patch, "tradeAttackThiefNpcs");
        changed |= SetPlayerBool("UBot.Trade.CounterAttack", patch, "tradeCounterAttack");
        changed |= SetPlayerBool("UBot.Trade.ProtectTransport", patch, "tradeProtectTransport");
        changed |= SetPlayerBool("UBot.Trade.CastBuffs", patch, "tradeCastBuffs");
        changed |= SetPlayerBool("UBot.Trade.MountTransport", patch, "tradeMountTransport");
        changed |= SetPlayerInt("UBot.Trade.MaxTransportDistance", patch, "tradeMaxTransportDistance", 1, 300);
        changed |= SetPlayerBool("UBot.Trade.SellGoods", patch, "tradeSellGoods");
        changed |= SetPlayerBool("UBot.Trade.BuyGoods", patch, "tradeBuyGoods");
        changed |= SetPlayerInt("UBot.Trade.BuyGoodsQuantity", patch, "tradeBuyGoodsQuantity", 0, int.MaxValue);
        changed |= SetPlayerString("UBot.Desktop.Trade.RecorderScriptPath", patch, "tradeRecorderScriptPath");

        if (TryGetBoolValue(patch, "tradeTracePlayer", out var tracePlayer))
            tracePlayerToggle = tracePlayer;

        if (TryGetBoolValue(patch, "tradeUseRouteScripts", out var useRouteScripts))
            useRouteScriptsToggle = useRouteScripts;

        if (TryGetIntValue(patch, "tradeSelectedRouteListIndex", out var parsedRouteListIndex))
        {
            selectedRouteListIndex = parsedRouteListIndex;
            changed = true;
        }

        if (patch.TryGetValue("tradeRouteLists", out var tradeRouteListsRaw) && tradeRouteListsRaw != null)
        {
            routeLists = ParseTradeRouteListsPatch(tradeRouteListsRaw);
            routeListsChanged = true;
            changed = true;
        }

        if (tracePlayerToggle.HasValue || useRouteScriptsToggle.HasValue)
        {
            var finalUseRouteScripts = PlayerConfig.Get("UBot.Trade.UseRouteScripts", true);
            var finalTracePlayer = PlayerConfig.Get("UBot.Trade.TracePlayer", false);

            if (useRouteScriptsToggle.HasValue)
                finalUseRouteScripts = useRouteScriptsToggle.Value;
            if (tracePlayerToggle.HasValue)
                finalTracePlayer = tracePlayerToggle.Value;

            if (useRouteScriptsToggle.HasValue && !tracePlayerToggle.HasValue)
                finalTracePlayer = !finalUseRouteScripts;
            else if (!useRouteScriptsToggle.HasValue && tracePlayerToggle.HasValue)
                finalUseRouteScripts = !finalTracePlayer;
            else if (useRouteScriptsToggle.HasValue && tracePlayerToggle.HasValue && finalUseRouteScripts == finalTracePlayer)
                finalTracePlayer = !finalUseRouteScripts;

            PlayerConfig.Set("UBot.Trade.UseRouteScripts", finalUseRouteScripts);
            PlayerConfig.Set("UBot.Trade.TracePlayer", finalTracePlayer);
            changed = true;
        }

        routeLists = NormalizeTradeRouteLists(routeLists);
        if (routeListsChanged)
            SaveTradeRouteListsToPlayerConfig(routeLists);

        selectedRouteListIndex = Math.Clamp(selectedRouteListIndex, 0, Math.Max(0, routeLists.Count - 1));
        PlayerConfig.Set("UBot.Trade.SelectedRouteListIndex", selectedRouteListIndex);

        if (changed)
            EventManager.FireEvent("OnSavePlayerConfig");

        return changed;
    }

    private static List<TradeRouteListDefinition> ParseTradeRouteListsPatch(object raw)
    {
        if (raw is not IEnumerable enumerable || raw is string)
            return LoadTradeRouteListsFromPlayerConfig();

        var routeLists = new List<TradeRouteListDefinition>();
        foreach (var item in enumerable)
        {
            if (!TryConvertObjectToDictionary(item, out var row))
                continue;

            var listName = TryGetStringValue(row, "name", out var parsedName) ? parsedName : string.Empty;
            var scripts = new List<string>();
            if (row.TryGetValue("scripts", out var scriptsRaw) && scriptsRaw is IEnumerable scriptsEnum && scriptsRaw is not string)
            {
                foreach (var script in scriptsEnum)
                {
                    var value = script?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                        scripts.Add(value);
                }
            }

            routeLists.Add(new TradeRouteListDefinition
            {
                Name = listName,
                Scripts = scripts
            });
        }

        return NormalizeTradeRouteLists(routeLists);
    }

    private static List<TradeRouteListDefinition> LoadTradeRouteListsFromPlayerConfig()
    {
        var names = PlayerConfig.GetArray<string>("UBot.Trade.RouteScriptList", ';')
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToList();

        if (names.Count == 0)
            names.Add("Default");

        var routeLists = new List<TradeRouteListDefinition>(names.Count);
        foreach (var name in names)
        {
            var scripts = PlayerConfig
                .GetArray<string>($"UBot.Trade.RouteScriptList.{name}")
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            routeLists.Add(new TradeRouteListDefinition
            {
                Name = name,
                Scripts = scripts
            });
        }

        return NormalizeTradeRouteLists(routeLists);
    }

    private static void SaveTradeRouteListsToPlayerConfig(IReadOnlyList<TradeRouteListDefinition> routeLists)
    {
        var normalizedLists = NormalizeTradeRouteLists(routeLists);
        var previousNames = PlayerConfig.GetArray<string>("UBot.Trade.RouteScriptList", ';')
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var currentNames = normalizedLists
            .Select(routeList => routeList.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var oldName in previousNames.Where(name => !currentNames.Contains(name, StringComparer.OrdinalIgnoreCase)))
            PlayerConfig.SetArray($"UBot.Trade.RouteScriptList.{oldName}", Array.Empty<string>());

        PlayerConfig.SetArray("UBot.Trade.RouteScriptList", currentNames, ";");
        foreach (var routeList in normalizedLists)
            PlayerConfig.SetArray($"UBot.Trade.RouteScriptList.{routeList.Name}", routeList.Scripts.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static List<TradeRouteListDefinition> NormalizeTradeRouteLists(IEnumerable<TradeRouteListDefinition> routeLists)
    {
        var normalized = new List<TradeRouteListDefinition>();
        var takenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var routeList in routeLists ?? Array.Empty<TradeRouteListDefinition>())
        {
            var baseName = NormalizeTradeRouteListName(routeList?.Name, "Route List");
            var finalName = baseName;
            var suffix = 2;
            while (!takenNames.Add(finalName))
                finalName = $"{baseName} {suffix++}";

            var scripts = (routeList?.Scripts ?? new List<string>())
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            normalized.Add(new TradeRouteListDefinition
            {
                Name = finalName,
                Scripts = scripts
            });
        }

        if (normalized.Count == 0)
        {
            normalized.Add(new TradeRouteListDefinition
            {
                Name = "Default",
                Scripts = new List<string>()
            });
            return normalized;
        }

        if (!normalized.Any(routeList => routeList.Name.Equals("Default", StringComparison.OrdinalIgnoreCase)))
        {
            normalized.Insert(0, new TradeRouteListDefinition
            {
                Name = "Default",
                Scripts = new List<string>()
            });
        }

        return normalized;
    }

    private static string NormalizeTradeRouteListName(string value, string fallback)
    {
        var raw = (value ?? string.Empty).Trim();
        if (raw.Length == 0)
            raw = fallback;

        var cleaned = new string(raw.Where(ch => char.IsLetterOrDigit(ch) || ch == ' ' || ch == '-' || ch == '_').ToArray()).Trim();
        if (cleaned.Length == 0)
            cleaned = fallback;
        if (cleaned.Length > 48)
            cleaned = cleaned.Substring(0, 48).Trim();
        return cleaned;
    }

    private static object BuildTradeState(IBotbase botbase)
    {
        var player = Game.Player;
        var routeLists = LoadTradeRouteListsFromPlayerConfig();
        var selectedRouteListIndex = Math.Clamp(
            PlayerConfig.Get("UBot.Trade.SelectedRouteListIndex", 0),
            0,
            Math.Max(0, routeLists.Count - 1));

        var selectedRouteList = routeLists.Count > 0
            ? routeLists[selectedRouteListIndex]
            : new TradeRouteListDefinition { Name = "Default", Scripts = new List<string>() };

        var routeRows = selectedRouteList.Scripts
            .Select(BuildTradeRouteRow)
            .Cast<object?>()
            .ToList();

        var currentRouteFile = ScriptManager.File ?? string.Empty;
        var jobInfo = player?.JobInformation;

        return new Dictionary<string, object?>
        {
            ["selected"] = Kernel.Bot?.Botbase?.Name == botbase?.Name,
            ["useRouteScripts"] = PlayerConfig.Get("UBot.Trade.UseRouteScripts", true),
            ["tracePlayer"] = PlayerConfig.Get("UBot.Trade.TracePlayer", false),
            ["selectedRouteList"] = selectedRouteList.Name,
            ["selectedRouteListIndex"] = selectedRouteListIndex,
            ["routeRows"] = routeRows,
            ["scriptRunning"] = ScriptManager.Running,
            ["currentRouteFile"] = currentRouteFile,
            ["currentRouteName"] = string.IsNullOrWhiteSpace(currentRouteFile) ? string.Empty : Path.GetFileNameWithoutExtension(currentRouteFile),
            ["hasTransport"] = player?.JobTransport != null,
            ["transportDistance"] = player?.JobTransport != null ? Math.Round(player.JobTransport.Position.DistanceToPlayer(), 1) : -1d,
            ["jobOverview"] = new Dictionary<string, object?>
            {
                ["difficulty"] = player?.TradeInfo?.Scale ?? 0,
                ["alias"] = jobInfo?.Name ?? string.Empty,
                ["level"] = (int)(jobInfo?.Level ?? 0),
                ["experience"] = jobInfo?.Experience ?? 0L,
                ["type"] = (jobInfo?.Type ?? JobType.None).ToString()
            }
        };
    }

    private static Dictionary<string, object?> BuildTradeRouteRow(string scriptPath)
    {
        var normalizedPath = scriptPath?.Trim() ?? string.Empty;
        var routeName = string.IsNullOrWhiteSpace(normalizedPath)
            ? "(empty)"
            : Path.GetFileNameWithoutExtension(normalizedPath);
        if (string.IsNullOrWhiteSpace(routeName))
            routeName = Path.GetFileName(normalizedPath);

        var info = ReadTradeRouteScriptInfo(normalizedPath);
        return new Dictionary<string, object?>
        {
            ["path"] = normalizedPath,
            ["name"] = routeName,
            ["startRegion"] = info.StartRegion,
            ["endRegion"] = info.EndRegion,
            ["numSteps"] = info.StepCount,
            ["missing"] = info.Missing
        };
    }

    private static (string StartRegion, string EndRegion, int StepCount, bool Missing) ReadTradeRouteScriptInfo(string scriptPath)
    {
        if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
            return ("-", "-", 0, true);

        try
        {
            Position first = default;
            Position last = default;
            var found = false;
            var steps = 0;

            foreach (var line in File.ReadLines(scriptPath))
            {
                if (!TryParseTradeMoveCommand(line, out var point))
                    continue;

                if (!found)
                {
                    first = point;
                    found = true;
                }

                last = point;
                steps++;
            }

            if (!found)
                return ("-", "-", 0, false);

            return (
                first.Region.Id == 0 ? "-" : first.Region.Id.ToString(CultureInfo.InvariantCulture),
                last.Region.Id == 0 ? "-" : last.Region.Id.ToString(CultureInfo.InvariantCulture),
                steps,
                false);
        }
        catch
        {
            return ("-", "-", 0, true);
        }
    }

    private static bool TryParseTradeMoveCommand(string line, out Position point)
    {
        point = default;
        if (string.IsNullOrWhiteSpace(line))
            return false;

        var trimmed = line.Trim();
        if (trimmed.StartsWith("#", StringComparison.Ordinal) || trimmed.StartsWith("//", StringComparison.Ordinal))
            return false;

        var split = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (split.Length < 6 || !split[0].Equals("move", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var xOffset)
            || !float.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var yOffset)
            || !float.TryParse(split[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var zOffset)
            || !byte.TryParse(split[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var xSector)
            || !byte.TryParse(split[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ySector))
        {
            return false;
        }

        point = new Position(xSector, ySector, xOffset, yOffset, zOffset);
        return true;
    }

    internal Dictionary<string, object?> BuildConfig() => BuildTradeBotbaseConfig();
    internal bool ApplyPatch(IBotbase botbase, Dictionary<string, object?> patch) => ApplyTradeBotbasePatch(botbase, patch);
    internal object BuildState(IBotbase botbase) => BuildTradeState(botbase);
}

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

internal sealed class UbotTargetAssistPluginService : UbotServiceBase
{
    private static Dictionary<string, object?> BuildTargetAssistPluginConfig()
    {
        var roleModeRaw = PlayerConfig.Get("UBot.TargetAssist.RoleMode", "Civil");
        var roleMode = roleModeRaw.Equals("Thief", StringComparison.OrdinalIgnoreCase)
            ? "thief"
            : roleModeRaw.Equals("HunterTrader", StringComparison.OrdinalIgnoreCase)
                ? "hunterTrader"
                : "civil";

        return new Dictionary<string, object?>
        {
            ["enabled"] = PlayerConfig.Get("UBot.TargetAssist.Enabled", false),
            ["maxRange"] = Math.Clamp(PlayerConfig.Get("UBot.TargetAssist.MaxRange", 40f), 5f, 400f),
            ["includeDeadTargets"] = PlayerConfig.Get("UBot.TargetAssist.IncludeDeadTargets", false),
            ["ignoreSnowShieldTargets"] = PlayerConfig.Get("UBot.TargetAssist.IgnoreSnowShieldTargets", true),
            ["ignoreBloodyStormTargets"] = PlayerConfig.Get("UBot.TargetAssist.IgnoreBloodyStormTargets", false),
            ["ignoredGuilds"] = PlayerConfig.GetArray<string>("UBot.TargetAssist.IgnoredGuilds", '|').Cast<object?>().ToList(),
            ["customPlayers"] = PlayerConfig.GetArray<string>("UBot.TargetAssist.CustomPlayers", '|').Cast<object?>().ToList(),
            ["onlyCustomPlayers"] = PlayerConfig.Get("UBot.TargetAssist.OnlyCustomPlayers", false),
            ["roleMode"] = roleMode,
            ["targetCycleKey"] = PlayerConfig.Get("UBot.TargetAssist.TargetCycleKey", "Oem3")
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
            PlayerConfig.Set("UBot.TargetAssist.MaxRange", (float)Math.Clamp(maxRange, 5d, 400d));
            changed = true;
        }

        if (TryGetStringListValue(patch, "ignoredGuilds", out var ignoredGuilds))
        {
            PlayerConfig.SetArray(
                "UBot.TargetAssist.IgnoredGuilds",
                ignoredGuilds.Select(value => value.Trim())
                    .Where(value => !string.IsNullOrWhiteSpace(value))
                    .Distinct(StringComparer.OrdinalIgnoreCase),
                "|");
            changed = true;
        }

        if (TryGetStringListValue(patch, "customPlayers", out var customPlayers))
        {
            PlayerConfig.SetArray(
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
            PlayerConfig.Set("UBot.TargetAssist.RoleMode", roleMode);
            changed = true;
        }

        if (changed)
            EventManager.FireEvent("OnSavePlayerConfig");

        return changed;
    }

    private static object BuildTargetAssistState()
    {
        const int effectTransferParam = 1701213281;
        var bloodyStormCodeTokens = new[] { "FANSTORM", "FAN_STORM" };

        var enabled = PlayerConfig.Get("UBot.TargetAssist.Enabled", false);
        var maxRange = Math.Clamp(PlayerConfig.Get("UBot.TargetAssist.MaxRange", 40f), 5f, 400f);
        var includeDeadTargets = PlayerConfig.Get("UBot.TargetAssist.IncludeDeadTargets", false);
        var ignoreSnowShieldTargets = PlayerConfig.Get("UBot.TargetAssist.IgnoreSnowShieldTargets", true);
        var ignoreBloodyStormTargets = PlayerConfig.Get("UBot.TargetAssist.IgnoreBloodyStormTargets", false);
        var onlyCustomPlayers = PlayerConfig.Get("UBot.TargetAssist.OnlyCustomPlayers", false);

        var roleModeRaw = PlayerConfig.Get("UBot.TargetAssist.RoleMode", "Civil");
        var roleMode = roleModeRaw.Equals("Thief", StringComparison.OrdinalIgnoreCase)
            ? "thief"
            : roleModeRaw.Equals("HunterTrader", StringComparison.OrdinalIgnoreCase)
                ? "hunterTrader"
                : "civil";

        var ignoredGuilds = PlayerConfig.GetArray<string>("UBot.TargetAssist.IgnoredGuilds", '|')
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var ignoredGuildSet = new HashSet<string>(ignoredGuilds, StringComparer.OrdinalIgnoreCase);

        var customPlayers = PlayerConfig.GetArray<string>("UBot.TargetAssist.CustomPlayers", '|')
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var customPlayerSet = new HashSet<string>(customPlayers, StringComparer.OrdinalIgnoreCase);

        var candidateCount = 0;
        var nearestTargetName = string.Empty;
        var nearestTargetDistance = -1d;

        if (enabled
            && Game.Ready
            && Game.Player != null
            && Game.Player.State.LifeState == LifeState.Alive
            && SpawnManager.TryGetEntities<SpawnedPlayer>(out var players))
        {
            var candidates = players
                .Where(player => player != null && player.UniqueId != Game.Player.UniqueId)
                .Where(player => !string.IsNullOrWhiteSpace(player.Name))
                .Where(player => includeDeadTargets || player.State.LifeState == LifeState.Alive)
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
            ["targetCycleKey"] = PlayerConfig.Get("UBot.TargetAssist.TargetCycleKey", "Oem3"),
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

            if (record.Params.Contains(effectTransferParam))
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

internal sealed class UbotMapPluginService : UbotServiceBase
{
    private static Dictionary<string, object?> BuildMapPluginConfig()
    {
        var config = LoadPluginJsonConfig(MapPluginName);
        var showFilter = NormalizeMapShowFilterValue(
            PlayerConfig.Get("UBot.Desktop.Map.ShowFilter",
                PlayerConfig.Get("UBot.Desktop.Map.EntityFilter", "All")));

        var collisionDetection = GlobalConfig.Get("UBot.EnableCollisionDetection",
            PlayerConfig.Get("UBot.Desktop.Map.CollisionDetection", false));
        var autoSelectUniques = PlayerConfig.Get("UBot.Map.AutoSelectUnique",
            PlayerConfig.Get("UBot.Desktop.Map.AutoSelectUniques", false));

        config["showFilter"] = showFilter;
        config["entityFilter"] = showFilter;
        config["collisionDetection"] = collisionDetection;
        config["autoSelectUniques"] = autoSelectUniques;
        config["autoSelectUnique"] = autoSelectUniques;
        config["resetToPlayerAt"] = PlayerConfig.Get("UBot.Desktop.Map.ResetToPlayerAt", 0L);
        return config;
    }

    private static bool ApplyMapPluginPatch(Dictionary<string, object?> patch)
    {
        var changed = false;
        if (TryGetStringValue(patch, "showFilter", out var showFilter) || TryGetStringValue(patch, "entityFilter", out showFilter))
        {
            var normalized = NormalizeMapShowFilterValue(showFilter);
            PlayerConfig.Set("UBot.Desktop.Map.ShowFilter", normalized);
            PlayerConfig.Set("UBot.Desktop.Map.EntityFilter", normalized);
            changed = true;
        }

        if (TryGetBoolValue(patch, "collisionDetection", out var collision))
        {
            GlobalConfig.Set("UBot.EnableCollisionDetection", collision);
            PlayerConfig.Set("UBot.Desktop.Map.CollisionDetection", collision);
            changed = true;
        }

        if (TryGetBoolValue(patch, "autoSelectUniques", out var autoSelect) || TryGetBoolValue(patch, "autoSelectUnique", out autoSelect))
        {
            PlayerConfig.Set("UBot.Map.AutoSelectUnique", autoSelect);
            PlayerConfig.Set("UBot.Desktop.Map.AutoSelectUniques", autoSelect);
            changed = true;
        }

        if (changed)
            EventManager.FireEvent("OnSavePlayerConfig");
        return changed;
    }

    internal Dictionary<string, object?> BuildConfig() => BuildMapPluginConfig();
    internal bool ApplyPatch(Dictionary<string, object?> patch) => ApplyMapPluginPatch(patch);
}

internal sealed class UbotProtectionPluginService : UbotServiceBase
{
    private static Dictionary<string, object?> BuildProtectionPluginConfig()
    {
        return new Dictionary<string, object?>
        {
            ["hpPotionEnabled"] = PlayerConfig.Get("UBot.Protection.checkUseHPPotionsPlayer", true),
            ["hpPotionThreshold"] = Math.Clamp(PlayerConfig.Get("UBot.Protection.numPlayerHPPotionMin", 75), 0, 100),
            ["mpPotionEnabled"] = PlayerConfig.Get("UBot.Protection.checkUseMPPotionsPlayer", true),
            ["mpPotionThreshold"] = Math.Clamp(PlayerConfig.Get("UBot.Protection.numPlayerMPPotionMin", 75), 0, 100),
            ["vigorHpEnabled"] = PlayerConfig.Get("UBot.Protection.checkUseVigorHP", false),
            ["vigorHpThreshold"] = Math.Clamp(PlayerConfig.Get("UBot.Protection.numPlayerHPVigorPotionMin", 50), 0, 100),
            ["vigorMpEnabled"] = PlayerConfig.Get("UBot.Protection.checkUseVigorMP", false),
            ["vigorMpThreshold"] = Math.Clamp(PlayerConfig.Get("UBot.Protection.numPlayerMPVigorPotionMin", 50), 0, 100),
            ["skillHpEnabled"] = PlayerConfig.Get("UBot.Protection.checkUseSkillHP", false),
            ["skillHpThreshold"] = Math.Clamp(PlayerConfig.Get("UBot.Protection.numPlayerSkillHPMin", 50), 0, 100),
            ["mpSkillEnabled"] = PlayerConfig.Get("UBot.Protection.checkUseSkillMP", false),
            ["mpSkillThreshold"] = Math.Clamp(PlayerConfig.Get("UBot.Protection.numPlayerSkillMPMin", 50), 0, 100),
            ["deadDelayEnabled"] = PlayerConfig.Get("UBot.Protection.checkDead", false),
            ["stopInTown"] = PlayerConfig.Get("UBot.Protection.checkStopBotOnReturnToTown", false),
            ["noArrows"] = PlayerConfig.Get("UBot.Protection.checkNoArrows", false),
            ["fullInventory"] = PlayerConfig.Get("UBot.Protection.checkInventory", false),
            ["fullPetInventory"] = PlayerConfig.Get("UBot.Protection.checkFullPetInventory", false),
            ["hpPotionsLow"] = PlayerConfig.Get("UBot.Protection.checkNoHPPotions", false),
            ["mpPotionsLow"] = PlayerConfig.Get("UBot.Protection.checkNoMPPotions", false),
            ["lowDurability"] = PlayerConfig.Get("UBot.Protection.checkDurability", false),
            ["levelUp"] = PlayerConfig.Get("UBot.Protection.checkLevelUp", false),
            ["shardFatigue"] = PlayerConfig.Get("UBot.Protection.checkShardFatigue", false),
            ["useUniversalPills"] = PlayerConfig.Get("UBot.Protection.checkUseUniversalPills", true),
            ["useBadStatusSkill"] = PlayerConfig.Get("UBot.Protection.checkUseBadStatusSkill", false),
            ["increaseInt"] = Math.Clamp(PlayerConfig.Get("UBot.Protection.numIncInt", 0), 0, 3),
            ["increaseStr"] = Math.Clamp(PlayerConfig.Get("UBot.Protection.numIncStr", 0), 0, 3),
            ["petHpPotionEnabled"] = PlayerConfig.Get("UBot.Protection.checkUsePetHP", false),
            ["petHgpPotionEnabled"] = PlayerConfig.Get("UBot.Protection.checkUseHGP", false),
            ["petAbnormalRecoveryEnabled"] = PlayerConfig.Get("UBot.Protection.checkUseAbnormalStatePotion", true),
            ["reviveGrowthFellow"] = PlayerConfig.Get("UBot.Protection.checkReviveAttackPet", false),
            ["autoSummonGrowthFellow"] = PlayerConfig.Get("UBot.Protection.checkAutoSummonAttackPet", false),
            ["hpSkillId"] = PlayerConfig.Get("UBot.Protection.HpSkill", 0U).ToString(),
            ["mpSkillId"] = PlayerConfig.Get("UBot.Protection.MpSkill", 0U).ToString(),
            ["badStatusSkillId"] = PlayerConfig.Get("UBot.Protection.BadStatusSkill", 0U).ToString()
        };
    }

    private static bool ApplyProtectionPluginPatch(Dictionary<string, object?> patch)
    {
        var changed = false;
        changed |= SetPlayerBool("UBot.Protection.checkUseHPPotionsPlayer", patch, "hpPotionEnabled");
        changed |= SetPlayerInt("UBot.Protection.numPlayerHPPotionMin", patch, "hpPotionThreshold", 0, 100);
        changed |= SetPlayerBool("UBot.Protection.checkUseMPPotionsPlayer", patch, "mpPotionEnabled");
        changed |= SetPlayerInt("UBot.Protection.numPlayerMPPotionMin", patch, "mpPotionThreshold", 0, 100);
        changed |= SetPlayerBool("UBot.Protection.checkUseVigorHP", patch, "vigorHpEnabled");
        changed |= SetPlayerInt("UBot.Protection.numPlayerHPVigorPotionMin", patch, "vigorHpThreshold", 0, 100);
        changed |= SetPlayerBool("UBot.Protection.checkUseVigorMP", patch, "vigorMpEnabled");
        changed |= SetPlayerInt("UBot.Protection.numPlayerMPVigorPotionMin", patch, "vigorMpThreshold", 0, 100);
        changed |= SetPlayerBool("UBot.Protection.checkUseSkillHP", patch, "skillHpEnabled");
        changed |= SetPlayerInt("UBot.Protection.numPlayerSkillHPMin", patch, "skillHpThreshold", 0, 100);
        changed |= SetPlayerBool("UBot.Protection.checkUseSkillMP", patch, "mpSkillEnabled");
        changed |= SetPlayerInt("UBot.Protection.numPlayerSkillMPMin", patch, "mpSkillThreshold", 0, 100);

        changed |= SetPlayerBool("UBot.Protection.checkDead", patch, "deadDelayEnabled");
        changed |= SetPlayerBool("UBot.Protection.checkStopBotOnReturnToTown", patch, "stopInTown");
        changed |= SetPlayerBool("UBot.Protection.checkNoArrows", patch, "noArrows");
        changed |= SetPlayerBool("UBot.Protection.checkInventory", patch, "fullInventory");
        changed |= SetPlayerBool("UBot.Protection.checkFullPetInventory", patch, "fullPetInventory");
        changed |= SetPlayerBool("UBot.Protection.checkNoHPPotions", patch, "hpPotionsLow");
        changed |= SetPlayerBool("UBot.Protection.checkNoMPPotions", patch, "mpPotionsLow");
        changed |= SetPlayerBool("UBot.Protection.checkDurability", patch, "lowDurability");
        changed |= SetPlayerBool("UBot.Protection.checkLevelUp", patch, "levelUp");
        changed |= SetPlayerBool("UBot.Protection.checkShardFatigue", patch, "shardFatigue");
        changed |= SetPlayerBool("UBot.Protection.checkUseUniversalPills", patch, "useUniversalPills");
        changed |= SetPlayerBool("UBot.Protection.checkUseBadStatusSkill", patch, "useBadStatusSkill");
        changed |= SetPlayerInt("UBot.Protection.numIncInt", patch, "increaseInt", 0, 3);
        changed |= SetPlayerInt("UBot.Protection.numIncStr", patch, "increaseStr", 0, 3);
        changed |= SetPlayerBool("UBot.Protection.checkUsePetHP", patch, "petHpPotionEnabled");
        changed |= SetPlayerBool("UBot.Protection.checkUseHGP", patch, "petHgpPotionEnabled");
        changed |= SetPlayerBool("UBot.Protection.checkUseAbnormalStatePotion", patch, "petAbnormalRecoveryEnabled");
        changed |= SetPlayerBool("UBot.Protection.checkReviveAttackPet", patch, "reviveGrowthFellow");
        changed |= SetPlayerBool("UBot.Protection.checkAutoSummonAttackPet", patch, "autoSummonGrowthFellow");

        if (TryGetUIntValue(patch, "hpSkillId", out var hpSkill))
        {
            PlayerConfig.Set("UBot.Protection.HpSkill", hpSkill);
            changed = true;
        }
        if (TryGetUIntValue(patch, "mpSkillId", out var mpSkill))
        {
            PlayerConfig.Set("UBot.Protection.MpSkill", mpSkill);
            changed = true;
        }
        if (TryGetUIntValue(patch, "badStatusSkillId", out var badStatusSkill))
        {
            PlayerConfig.Set("UBot.Protection.BadStatusSkill", badStatusSkill);
            changed = true;
        }

        return changed;
    }

    internal Dictionary<string, object?> BuildConfig() => BuildProtectionPluginConfig();
    internal bool ApplyPatch(Dictionary<string, object?> patch) => ApplyProtectionPluginPatch(patch);
}

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
        config["imbueSkillId"] = PlayerConfig.Get("UBot.Desktop.Skills.ImbueSkillId", 0U);
        config["resurrectionSkillId"] = PlayerConfig.Get("UBot.Skills.ResurrectionSkill", 0U);
        config["teleportSkillId"] = PlayerConfig.Get("UBot.Skills.TeleportSkill", 0U);
        config["selectedMasteryId"] = PlayerConfig.Get("UBot.Skills.selectedMastery", 0U);

        for (var i = 0; i < AttackRarityByIndex.Length; i++)
            config[$"attackSkills_{i}"] = PlayerConfig.GetArray<uint>($"UBot.Skills.Attacks_{i}").Distinct().ToList();

        config["buffSkills"] = PlayerConfig.GetArray<uint>("UBot.Skills.Buffs").Distinct().ToList();
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
        return new Dictionary<string, object?>
        {
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["playerReady"] = Game.Player != null,
            ["skillCatalog"] = BuildSkillCatalog(),
            ["masteryCatalog"] = BuildMasteryCatalog(),
            ["activeBuffs"] = BuildActiveBuffSnapshot()
        };
    }

    private static List<Dictionary<string, object?>> BuildSkillCatalog()
    {
        var entries = new List<Dictionary<string, object?>>();
        foreach (var skill in CollectKnownAndAbilitySkills())
        {
            var record = skill.Record;
            if (record == null)
                continue;

            var name = record.GetRealName();
            if (string.IsNullOrWhiteSpace(name))
                name = record.Basic_Code;
            if (string.IsNullOrWhiteSpace(name))
                name = $"Skill {skill.Id}";

            var isPassive = skill.IsPassive;
            var isAttack = skill.IsAttack;
            var isImbue = skill.IsImbue;
            bool isLowLevel;
            try
            {
                isLowLevel = skill.IsLowLevel();
            }
            catch
            {
                isLowLevel = false;
            }

            entries.Add(new Dictionary<string, object?>
            {
                ["id"] = skill.Id,
                ["name"] = name,
                ["isPassive"] = isPassive,
                ["isAttack"] = isAttack,
                ["isBuff"] = !isPassive && !isAttack,
                ["isImbue"] = isImbue,
                ["isLowLevel"] = isLowLevel,
                ["icon"] = record.UI_IconFile
            });
        }

        return entries
            .OrderBy(row => row.TryGetValue("name", out var n) ? n?.ToString() : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.TryGetValue("id", out var id) && id is uint u ? u : 0)
            .ToList();
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
}

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

internal sealed class UbotPluginStateAuxService : UbotServiceBase
{
    private readonly UbotConnectionService _connectionService;
    internal UbotPluginStateAuxService(UbotConnectionService connectionService)
    {
        _connectionService = connectionService;
    }
    private static object BuildQuestState()
    {
        var quests = Game.Player?.QuestLog?.ActiveQuests?.Values;
        var completed = Game.Player?.QuestLog?.CompletedQuests?.Length ?? 0;
        var activeRows = new List<Dictionary<string, object?>>();
        if (quests != null)
        {
            foreach (var quest in quests)
            {
                activeRows.Add(new Dictionary<string, object?>
                {
                    ["id"] = quest.Id,
                    ["name"] = quest.Quest?.GetTranslatedName() ?? quest.Quest?.CodeName ?? quest.Id.ToString(CultureInfo.InvariantCulture),
                    ["status"] = quest.Status.ToString(),
                    ["objectiveCount"] = quest.Objectives?.Length ?? 0,
                    ["remainingTime"] = quest.RemainingTime
                });
            }
        }

        return new Dictionary<string, object?>
        {
            ["activeCount"] = activeRows.Count,
            ["completedCount"] = completed,
            ["active"] = activeRows
                .OrderBy(quest => quest["name"]?.ToString() ?? string.Empty)
                .Take(200)
                .Cast<object?>()
                .ToList()
        };
    }

    private object BuildStatisticsState()
    {
        SpawnManager.TryGetEntities<SpawnedMonster>(out var monsters);
        var monsterCount = monsters?.Count() ?? 0;
        var inventoryCount = Game.Player?.Inventory?.GetNormalPartItems().Count ?? 0;

        return new Dictionary<string, object?>
        {
            ["status"] = _connectionService.CreateStatusSnapshot().StatusText,
            ["monsterCount"] = monsterCount,
            ["inventoryCount"] = inventoryCount,
            ["botRunning"] = Kernel.Bot != null && Kernel.Bot.Running,
            ["clientless"] = Game.Clientless
        };
    }

    internal object BuildQuestPluginState() => BuildQuestState();
    internal object BuildStatisticsPluginState() => BuildStatisticsState();
}

internal sealed class UbotCommandCenterPluginService : UbotServiceBase
{
    private readonly UbotCommandCenterService _commandCenterService;

    internal UbotCommandCenterPluginService(UbotCommandCenterService commandCenterService)
    {
        _commandCenterService = commandCenterService;
    }

    internal Dictionary<string, object?> BuildConfig()
    {
        return _commandCenterService.BuildCommandCenterPluginConfig();
    }

    internal bool ApplyPatch(Dictionary<string, object?> patch)
    {
        return _commandCenterService.ApplyCommandCenterPluginPatch(patch);
    }
}

