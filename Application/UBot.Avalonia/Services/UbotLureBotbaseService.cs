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

internal sealed class UbotLureBotbaseService : UbotServiceBase
{
    private static Dictionary<string, object?> BuildLureBotbaseConfig()
    {
        var region = UBot.Core.RuntimeAccess.Player.Get<ushort>("UBot.Lure.Area.Region");
        var x = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.Area.X", 0f);
        var y = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.Area.Y", 0f);
        var z = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.Area.Z", 0f);
        var selectedMonsterType = UBot.Core.RuntimeAccess.Player.GetEnum("UBot.Lure.SelectedMonsterType", MonsterRarity.General);
        var useScript = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.UseScript", false);
        var stayAtCenter = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.StayAtCenter", false);

        var lureMode = useScript
            ? "useScript"
            : stayAtCenter
                ? "stayAtCenter"
                : "walkRandomly";

        return new Dictionary<string, object?>
        {
            ["lureLocationScript"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.Walkback.File", string.Empty),
            ["lureCenterRegion"] = (int)region,
            ["lureCenterXSector"] = (int)((CoreRegion)region).X,
            ["lureCenterYSector"] = (int)((CoreRegion)region).Y,
            ["lureCenterX"] = Math.Round(x, 2),
            ["lureCenterY"] = Math.Round(y, 2),
            ["lureCenterZ"] = Math.Round(z, 2),
            ["lureRadius"] = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.Area.Radius", 20), 1, 200),
            ["lureMode"] = lureMode,
            ["lureScriptPath"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.SelectedScriptPath", string.Empty),
            ["lureStayAtCenterForEnabled"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.StayAtCenterFor", false),
            ["lureStayAtCenterSeconds"] = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.StayAtCenterForSeconds", 10), 0, 3600),
            ["lureCastHowlingShout"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.UseHowlingShout", false),
            ["lureDontCastNearCenter"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.NoHowlingAtCenter", true),
            ["lureUseNormalAttackSwitch"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.UseNormalAttack", false),
            ["lureUseAttackSkillSwitch"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.UseAttackingSkills", false),
            ["lureStopOnDeadPartyMembersEnabled"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.StopIfNumPartyMemberDead", false),
            ["lureStopDeadPartyMembers"] = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.NumPartyMemberDead", 0), 0, 8),
            ["lureStopOnPartyMembersEnabled"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.StopIfNumPartyMember", false),
            ["lureStopPartyMembersLe"] = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.NumPartyMember", 0), 0, 8),
            ["lureStopOnPartyMembersOnSpotEnabled"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.StopIfNumPartyMembersOnSpot", false),
            ["lureStopPartyMembersOnSpotLe"] = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.NumPartyMembersOnSpot", 0), 0, 8),
            ["lureStopOnMonsterTypeEnabled"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.StopIfNumMonsterType", false),
            ["lureMonsterType"] = FormatMonsterTypeToken(selectedMonsterType),
            ["lureStopMonsterCount"] = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.NumMonsterType", 1), 0, 50)
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

            UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.WalkRandomly", walkRandomly);
            UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.StayAtCenter", stayAtCenter);
            UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.UseScript", useScript);
            changed = true;
        }

        if (patch.TryGetValue("lureMonsterType", out var lureMonsterTypeRaw) && lureMonsterTypeRaw != null)
        {
            var current = UBot.Core.RuntimeAccess.Player.GetEnum("UBot.Lure.SelectedMonsterType", MonsterRarity.General);
            var next = current;

            if (lureMonsterTypeRaw is string token)
                next = ParseMonsterTypeToken(token, current);
            else if (TryConvertInt(lureMonsterTypeRaw, out var numericMonsterType))
                next = (MonsterRarity)Math.Clamp(numericMonsterType, byte.MinValue, byte.MaxValue);

            UBot.Core.RuntimeAccess.Player.Set("UBot.Lure.SelectedMonsterType", (byte)next);
            changed = true;
        }

        if (changed)
            UBot.Core.RuntimeAccess.Events.FireEvent("OnSavePlayerConfig");

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
        var playerPosition = UBot.Core.RuntimeAccess.Session.Player?.Position ?? default;

        return new Dictionary<string, object?>
        {
            ["selected"] = UBot.Core.RuntimeAccess.Core.Bot?.Botbase?.Name == botbase?.Name,
            ["centerRegion"] = (int)areaPosition.Region.Id,
            ["centerXSector"] = (int)areaPosition.Region.X,
            ["centerYSector"] = (int)areaPosition.Region.Y,
            ["centerX"] = Math.Round(areaPosition.XOffset, 2),
            ["centerY"] = Math.Round(areaPosition.YOffset, 2),
            ["centerZ"] = Math.Round(areaPosition.ZOffset, 2),
            ["radius"] = area?.Radius ?? Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Lure.Area.Radius", 20), 1, 200),
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

