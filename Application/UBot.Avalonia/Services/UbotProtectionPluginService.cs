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

internal sealed class UbotProtectionPluginService : UbotServiceBase
{
    private static Dictionary<string, object?> BuildProtectionPluginConfig()
    {
        return new Dictionary<string, object?>
        {
            ["hpPotionEnabled"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckUseHPPotionsPlayer", true),
            ["hpPotionThreshold"] = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.ThresholdPlayerHPPotionMin", 75), 0, 100),
            ["mpPotionEnabled"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckUseMPPotionsPlayer", true),
            ["mpPotionThreshold"] = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.ThresholdPlayerMPPotionMin", 75), 0, 100),
            ["vigorHpEnabled"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckUseVigorHP", false),
            ["vigorHpThreshold"] = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.ThresholdPlayerHPVigorPotionMin", 50), 0, 100),
            ["vigorMpEnabled"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckUseVigorMP", false),
            ["vigorMpThreshold"] = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.ThresholdPlayerMPVigorPotionMin", 50), 0, 100),
            ["skillHpEnabled"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckUseSkillHP", false),
            ["skillHpThreshold"] = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.ThresholdPlayerSkillHPMin", 50), 0, 100),
            ["mpSkillEnabled"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckUseSkillMP", false),
            ["mpSkillThreshold"] = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.ThresholdPlayerSkillMPMin", 50), 0, 100),
            ["deadDelayEnabled"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckDead", false),
            ["stopInTown"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckStopBotOnReturnToTown", false),
            ["noArrows"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckNoArrows", false),
            ["fullInventory"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckInventory", false),
            ["fullPetInventory"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckFullPetInventory", false),
            ["hpPotionsLow"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckNoHPPotions", false),
            ["mpPotionsLow"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckNoMPPotions", false),
            ["lowDurability"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckDurability", false),
            ["levelUp"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckLevelUp", false),
            ["shardFatigue"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckShardFatigue", false),
            ["useUniversalPills"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckUseUniversalPills", true),
            ["useBadStatusSkill"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckUseBadStatusSkill", false),
            ["increaseInt"] = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.IncrementInt", 0), 0, 3),
            ["increaseStr"] = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.IncrementStr", 0), 0, 3),
            ["petHpPotionEnabled"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckUsePetHP", false),
            ["petHgpPotionEnabled"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckUseHGP", false),
            ["petAbnormalRecoveryEnabled"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckUseAbnormalStatePotion", true),
            ["reviveGrowthFellow"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckReviveAttackPet", false),
            ["autoSummonGrowthFellow"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.CheckAutoSummonAttackPet", false),
            ["hpSkillId"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.HpSkill", 0U).ToString(),
            ["mpSkillId"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.MpSkill", 0U).ToString(),
            ["badStatusSkillId"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.BadStatusSkill", 0U).ToString()
        };
    }

    private static bool ApplyProtectionPluginPatch(Dictionary<string, object?> patch)
    {
        var changed = false;
        changed |= SetPlayerBool("UBot.Protection.CheckUseHPPotionsPlayer", patch, "hpPotionEnabled");
        changed |= SetPlayerInt("UBot.Protection.ThresholdPlayerHPPotionMin", patch, "hpPotionThreshold", 0, 100);
        changed |= SetPlayerBool("UBot.Protection.CheckUseMPPotionsPlayer", patch, "mpPotionEnabled");
        changed |= SetPlayerInt("UBot.Protection.ThresholdPlayerMPPotionMin", patch, "mpPotionThreshold", 0, 100);
        changed |= SetPlayerBool("UBot.Protection.CheckUseVigorHP", patch, "vigorHpEnabled");
        changed |= SetPlayerInt("UBot.Protection.ThresholdPlayerHPVigorPotionMin", patch, "vigorHpThreshold", 0, 100);
        changed |= SetPlayerBool("UBot.Protection.CheckUseVigorMP", patch, "vigorMpEnabled");
        changed |= SetPlayerInt("UBot.Protection.ThresholdPlayerMPVigorPotionMin", patch, "vigorMpThreshold", 0, 100);
        changed |= SetPlayerBool("UBot.Protection.CheckUseSkillHP", patch, "skillHpEnabled");
        changed |= SetPlayerInt("UBot.Protection.ThresholdPlayerSkillHPMin", patch, "skillHpThreshold", 0, 100);
        changed |= SetPlayerBool("UBot.Protection.CheckUseSkillMP", patch, "mpSkillEnabled");
        changed |= SetPlayerInt("UBot.Protection.ThresholdPlayerSkillMPMin", patch, "mpSkillThreshold", 0, 100);

        changed |= SetPlayerBool("UBot.Protection.CheckDead", patch, "deadDelayEnabled");
        changed |= SetPlayerBool("UBot.Protection.CheckStopBotOnReturnToTown", patch, "stopInTown");
        changed |= SetPlayerBool("UBot.Protection.CheckNoArrows", patch, "noArrows");
        changed |= SetPlayerBool("UBot.Protection.CheckInventory", patch, "fullInventory");
        changed |= SetPlayerBool("UBot.Protection.CheckFullPetInventory", patch, "fullPetInventory");
        changed |= SetPlayerBool("UBot.Protection.CheckNoHPPotions", patch, "hpPotionsLow");
        changed |= SetPlayerBool("UBot.Protection.CheckNoMPPotions", patch, "mpPotionsLow");
        changed |= SetPlayerBool("UBot.Protection.CheckDurability", patch, "lowDurability");
        changed |= SetPlayerBool("UBot.Protection.CheckLevelUp", patch, "levelUp");
        changed |= SetPlayerBool("UBot.Protection.CheckShardFatigue", patch, "shardFatigue");
        changed |= SetPlayerBool("UBot.Protection.CheckUseUniversalPills", patch, "useUniversalPills");
        changed |= SetPlayerBool("UBot.Protection.CheckUseBadStatusSkill", patch, "useBadStatusSkill");
        changed |= SetPlayerInt("UBot.Protection.IncrementInt", patch, "increaseInt", 0, 3);
        changed |= SetPlayerInt("UBot.Protection.IncrementStr", patch, "increaseStr", 0, 3);
        changed |= SetPlayerBool("UBot.Protection.CheckUsePetHP", patch, "petHpPotionEnabled");
        changed |= SetPlayerBool("UBot.Protection.CheckUseHGP", patch, "petHgpPotionEnabled");
        changed |= SetPlayerBool("UBot.Protection.CheckUseAbnormalStatePotion", patch, "petAbnormalRecoveryEnabled");
        changed |= SetPlayerBool("UBot.Protection.CheckReviveAttackPet", patch, "reviveGrowthFellow");
        changed |= SetPlayerBool("UBot.Protection.CheckAutoSummonAttackPet", patch, "autoSummonGrowthFellow");

        if (TryGetUIntValue(patch, "hpSkillId", out var hpSkill))
        {
            UBot.Core.RuntimeAccess.Player.Set("UBot.Protection.HpSkill", hpSkill);
            changed = true;
        }
        if (TryGetUIntValue(patch, "mpSkillId", out var mpSkill))
        {
            UBot.Core.RuntimeAccess.Player.Set("UBot.Protection.MpSkill", mpSkill);
            changed = true;
        }
        if (TryGetUIntValue(patch, "badStatusSkillId", out var badStatusSkill))
        {
            UBot.Core.RuntimeAccess.Player.Set("UBot.Protection.BadStatusSkill", badStatusSkill);
            changed = true;
        }

        return changed;
    }

    internal Dictionary<string, object?> BuildConfig() => BuildProtectionPluginConfig();
    internal bool ApplyPatch(Dictionary<string, object?> patch) => ApplyProtectionPluginPatch(patch);
}

