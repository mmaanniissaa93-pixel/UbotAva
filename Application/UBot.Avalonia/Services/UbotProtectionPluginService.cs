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

