using System;
using System.Linq;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Objects;
using UBot.Core.Objects.Skill;
using UBot.Skills.Components;

namespace UBot.Skills.Subscriber;

internal static class LoadCharacterSubscriber
{
    private static readonly object Owner = new object();
    private static int _lastMasteryUpdateTick;

    public static void SubscribeEvents()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnLoadCharacter", LoadSkillSettings, Owner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnResurrectionRequest", OnResurrectionRequest, Owner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnLoadCharacterStats", TryLearnSelectedMastery, Owner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnLearnSkillMastery", new System.Action<MasteryInfo>(_ => TryLearnSelectedMastery()), Owner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnLevelUp", new System.Action<byte>(_ => TryLearnSelectedMastery()), Owner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnExpSpUpdate", new System.Action(TryLearnSelectedMastery), Owner);
    }

    public static void UnsubscribeAll()
    {
        UBot.Core.RuntimeAccess.Events.UnsubscribeOwner(Owner);
    }

    private static void LoadSkillSettings()
    {
        if (UBot.Core.RuntimeAccess.Session.Player == null)
            return;

        UBot.Core.RuntimeAccess.Session.Player.TryGetAbilitySkills(out var abilitySkills);
        SkillManager.Buffs.Clear();
        foreach (var key in SkillManager.Skills.Keys.ToArray())
            SkillManager.Skills[key].Clear();

        foreach (var buffId in UBot.Core.RuntimeAccess.Player.GetArray<uint>("UBot.Skills.Buffs"))
        {
            var skillInfo = UBot.Core.RuntimeAccess.Session.Player.Skills.GetSkillInfoById(buffId);
            if (skillInfo == null)
            {
                skillInfo = abilitySkills.FirstOrDefault(p => p.Id == buffId);
                if (skillInfo == null)
                    continue;
            }

            SkillManager.Buffs.Add(skillInfo);
        }

        var imbueSkillId = UBot.Core.RuntimeAccess.Player.Get("UBot.Desktop.Skills.ImbueSkillId", 0U);
        SkillManager.ImbueSkill = imbueSkillId == 0
            ? null
            : (
                UBot.Core.RuntimeAccess.Session.Player.Skills.GetSkillInfoById(imbueSkillId)
                ?? abilitySkills.FirstOrDefault(p => p.Id == imbueSkillId)
            );

        var resurrectionSkillId = UBot.Core.RuntimeAccess.Player.Get("UBot.Skills.ResurrectionSkill", 0U);
        SkillManager.ResurrectionSkill = resurrectionSkillId == 0
            ? null
            : (
                UBot.Core.RuntimeAccess.Session.Player.Skills.GetSkillInfoById(resurrectionSkillId)
                ?? abilitySkills.FirstOrDefault(p => p.Id == resurrectionSkillId)
            );

        var teleportSkillId = UBot.Core.RuntimeAccess.Player.Get("UBot.Skills.TeleportSkill", 0U);
        SkillManager.TeleportSkill = teleportSkillId == 0
            ? null
            : (
                UBot.Core.RuntimeAccess.Session.Player.Skills.GetSkillInfoById(teleportSkillId)
                ?? abilitySkills.FirstOrDefault(p => p.Id == teleportSkillId)
            );

        for (var i = 0; i < 9; i++)
        {
            var skillIds = UBot.Core.RuntimeAccess.Player.GetArray<uint>("UBot.Skills.Attacks_" + i);

            foreach (var skillId in skillIds)
            {
                var skillInfo = UBot.Core.RuntimeAccess.Session.Player.Skills.GetSkillInfoById(skillId);
                if (skillInfo == null)
                {
                    skillInfo = abilitySkills.FirstOrDefault(p => p.Id == skillId);
                    if (skillInfo == null)
                        continue;
                }

                switch (i)
                {
                    case 1:
                        SkillManager.Skills[MonsterRarity.Champion].Add(skillInfo);
                        continue;
                    case 2:
                        SkillManager.Skills[MonsterRarity.Giant].Add(skillInfo);
                        continue;
                    case 3:
                        SkillManager.Skills[MonsterRarity.GeneralParty].Add(skillInfo);
                        continue;
                    case 4:
                        SkillManager.Skills[MonsterRarity.ChampionParty].Add(skillInfo);
                        continue;
                    case 5:
                        SkillManager.Skills[MonsterRarity.GiantParty].Add(skillInfo);
                        continue;
                    case 6:
                        SkillManager.Skills[MonsterRarity.Elite].Add(skillInfo);
                        continue;
                    case 7:
                        SkillManager.Skills[MonsterRarity.EliteStrong].Add(skillInfo);
                        continue;
                    case 8:
                        SkillManager.Skills[MonsterRarity.Unique].Add(skillInfo);
                        continue;
                    default:
                        SkillManager.Skills[MonsterRarity.General].Add(skillInfo);
                        continue;
                }
            }
        }

        TryLearnSelectedMastery();
    }

    private static void OnResurrectionRequest()
    {
        if (!UBot.Core.RuntimeAccess.Player.Get("UBot.Skills.checkAcceptResurrection", false))
            return;

        UBot.Core.RuntimeAccess.Session.AcceptanceRequest?.Accept();
    }

    private static void TryLearnSelectedMastery()
    {
        if (!UBot.Core.RuntimeAccess.Player.Get("UBot.Skills.checkLearnMastery", false))
            return;

        if (!UBot.Core.RuntimeAccess.Core.Bot.Running && !UBot.Core.RuntimeAccess.Player.Get("UBot.Skills.checkLearnMasteryBotStopped", false))
            return;

        if (UBot.Core.RuntimeAccess.Core.TickCount - _lastMasteryUpdateTick < 1000)
            return;

        var player = UBot.Core.RuntimeAccess.Session.Player;
        if (player?.Skills == null)
            return;

        var selectedMasteryId = UBot.Core.RuntimeAccess.Player.Get("UBot.Skills.selectedMastery", 0U);
        if (selectedMasteryId == 0)
            return;

        var selectedMastery = player.Skills.GetMasteryInfoById(selectedMasteryId);
        if (selectedMastery == null)
            return;

        var gap = Math.Max(0, UBot.Core.RuntimeAccess.Player.Get("UBot.Skills.numMasteryGap", 0));
        if (selectedMastery.Level + gap >= player.Level)
            return;

        if (player.SkillPoints == 0)
            return;

        _lastMasteryUpdateTick = UBot.Core.RuntimeAccess.Core.TickCount;
        LearnMasteryHandler.LearnMastery(selectedMasteryId);
    }
}
