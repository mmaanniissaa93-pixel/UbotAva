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

internal sealed class UbotTrainingBotbaseService : UbotServiceBase
{
    private static Dictionary<string, object?> BuildTrainingBotbaseConfig()
    {
        return new Dictionary<string, object?>
        {
            ["areaRegion"] = (int)UBot.Core.RuntimeAccess.Player.Get<ushort>("UBot.Area.Region"),
            ["areaX"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Area.X", 0f),
            ["areaY"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Area.Y", 0f),
            ["areaZ"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Area.Z", 0f),
            ["areaRadius"] = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Area.Radius", 50), 5, 100),
            ["walkScript"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Walkback.File", string.Empty),
            ["useMount"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Training.checkUseMount", true),
            ["castBuffs"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Training.checkCastBuffs", true),
            ["useSpeedDrug"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Training.checkUseSpeedDrug", true),
            ["useReverse"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Training.checkBoxUseReverse", false),
            ["berserkWhenFull"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Training.checkBerzerkWhenFull", false),
            ["berserkByMonsterAmount"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Training.checkBerzerkMonsterAmount", false),
            ["berserkMonsterAmount"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Training.numBerzerkMonsterAmount", 5),
            ["berserkByAvoidance"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Training.checkBerzerkAvoidance", false),
            ["berserkByMonsterRarity"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Training.checkBerserkOnMonsterRarity", false),
            ["ignoreDimensionPillar"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Training.checkBoxDimensionPillar", false),
            ["attackWeakerFirst"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Training.checkAttackWeakerFirst", false),
            ["dontFollowMobs"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Training.checkBoxDontFollowMobs", false),
            ["avoidanceList"] = UBot.Core.RuntimeAccess.Player.GetArray<string>("UBot.Avoidance.Avoid").ToList(),
            ["preferList"] = UBot.Core.RuntimeAccess.Player.GetArray<string>("UBot.Avoidance.Prefer").ToList(),
            ["berserkList"] = UBot.Core.RuntimeAccess.Player.GetArray<string>("UBot.Avoidance.Berserk").ToList()
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
            UBot.Core.RuntimeAccess.Player.SetArray("UBot.Avoidance.Avoid", avoidance.ToArray());
            changed = true;
        }

        if (TryGetStringListValue(patch, "preferList", out var prefer))
        {
            UBot.Core.RuntimeAccess.Player.SetArray("UBot.Avoidance.Prefer", prefer.ToArray());
            changed = true;
        }

        if (TryGetStringListValue(patch, "berserkList", out var berserk))
        {
            UBot.Core.RuntimeAccess.Player.SetArray("UBot.Avoidance.Berserk", berserk.ToArray());
            changed = true;
        }

        if (changed)
            UBot.Core.RuntimeAccess.Player.Save();

        return changed;
    }

    internal Dictionary<string, object?> BuildConfig() => BuildTrainingBotbaseConfig();
    internal bool ApplyPatch(Dictionary<string, object?> patch) => ApplyTrainingBotbasePatch(patch);
}

