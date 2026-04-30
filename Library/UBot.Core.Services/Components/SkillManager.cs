using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UBot.Core.Abstractions;
using UBot.Core.Abstractions.Services;
using UBot.Core.Client.ReferenceObjects;
using UBot.Core.Objects;
using UBot.Core.Objects.Skill;
using UBot.Core.Objects.Spawn;
using UBot.Core.Services;
using UBot.GameData.ReferenceObjects;
using UBot.Protocol.Commands.Agent.Skill;

namespace UBot.Core.Components;

public static class SkillManager
{
    private static ISkillService _service = new SkillService();

    public static uint LastCastedSkillId
    {
        get => _service.LastCastedSkillId;
        set => _service.LastCastedSkillId = value;
    }

    public static Dictionary<MonsterRarity, List<SkillInfo>> Skills => ((SkillService)_service).Skills;
    public static SkillInfo ResurrectionSkill { get => ((SkillService)_service).ResurrectionSkill; set => ((SkillService)_service).ResurrectionSkill = value; }
    public static SkillInfo ImbueSkill { get => ((SkillService)_service).ImbueSkill; set => ((SkillService)_service).ImbueSkill = value; }
    public static List<SkillInfo> Buffs => ((SkillService)_service).Buffs;
    public static SkillInfo TeleportSkill { get => ((SkillService)_service).TeleportSkill; set => ((SkillService)_service).TeleportSkill = value; }
    public static bool UseSkillsInOrder => _service.UseSkillsInOrder;
    public static bool IsLastCastedBasic => _service.IsLastCastedBasic;

    public static void Initialize()
    {
        Initialize(new SkillService());
    }

    public static void Initialize(ISkillService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _service.Initialize();
        ServiceRuntime.Skill = _service;
        ServiceRuntime.Log?.Debug($"Initialized [SkillManager] for [{Skills.Count}] different mob rarities!");
    }

    public static void ResetBaseSkills() => _service.ResetBaseSkills();
    public static void NotifySkillCasted(uint skillId) => _service.NotifySkillCasted(skillId);
    public static void SetSkills(MonsterRarity monsterRarity, List<SkillInfo> skills) => ((SkillService)_service).SetSkills(monsterRarity, skills);
    public static SkillInfo GetNextSkill() => _service.GetNextSkill() as SkillInfo;
    public static bool CheckSkillRequired(RefSkill skill) => _service.CheckSkillRequired(skill);
    public static bool CastSkill(SkillInfo skill, uint targetId = 0) => _service.CastSkill(skill, targetId);
    public static Task<bool> CastSkillAsync(SkillInfo skill, uint targetId = 0) => _service.CastSkillAsync(skill, targetId);
    public static bool CastSkillOld(SkillInfo skill, uint targetId = 0) => _service.CastSkillOld(skill, targetId);
    public static Task<bool> CastSkillOldAsync(SkillInfo skill, uint targetId = 0) => _service.CastSkillOldAsync(skill, targetId);
    public static void CastBuff(SkillInfo skill, uint target = 0, bool awaitBuffResponse = true) => _service.CastBuff(skill, target, awaitBuffResponse);
    public static Task CastBuffAsync(SkillInfo skill, uint target = 0, bool awaitBuffResponse = true) => _service.CastBuffAsync(skill, target, awaitBuffResponse);
    public static void CastSkillAt(SkillInfo skill, Position target) => _service.CastSkillAt(skill, target);
    public static Task CastSkillAtAsync(SkillInfo skill, Position target) => _service.CastSkillAtAsync(skill, target);
    public static bool CastAutoAttack() => _service.CastAutoAttack();
    public static Task<bool> CastAutoAttackAsync() => _service.CastAutoAttackAsync();
    public static void CastSkillAt(uint skillId, Position position) => _service.CastSkillAt(skillId, position);
    public static void CancelBuff(uint skillId) => _service.CancelBuff(skillId);
    public static bool CancelAction() => _service.CancelAction();
    public static Task<bool> CancelActionAsync() => _service.CancelActionAsync();
}

public sealed class SkillService : ISkillService
{
    private static readonly uint[] EmptySkills = Array.Empty<uint>();
    private readonly object _skillLock = new();
    private int _lastIndex;
    private IEnumerable<uint> _baseSkills = EmptySkills;

    public uint LastCastedSkillId { get; set; }
    public Dictionary<MonsterRarity, List<SkillInfo>> Skills { get; private set; }
    public SkillInfo ResurrectionSkill { get; set; }
    public SkillInfo ImbueSkill { get; set; }
    public List<SkillInfo> Buffs { get; private set; }
    public SkillInfo TeleportSkill { get; set; }
    public bool UseSkillsInOrder => Config?.UseSkillsInOrder ?? false;

    public bool IsLastCastedBasic
    {
        get
        {
            EnsureBaseSkillsLoaded();
            return _baseSkills.Contains(LastCastedSkillId);
        }
    }

    public void Initialize()
    {
        Skills = Enum.GetValues(typeof(MonsterRarity))
            .Cast<MonsterRarity>()
            .ToDictionary(v => v, v => new List<SkillInfo>());
        Buffs = new List<SkillInfo>();
        ResetBaseSkills();
    }

    public void ResetBaseSkills()
    {
        _baseSkills = EmptySkills;
    }

    public void NotifySkillCasted(uint skillId)
    {
        lock (_skillLock)
        {
            LastCastedSkillId = skillId;
        }
    }

    public void SetSkills(object monsterRarity, object skills)
    {
        if (monsterRarity is MonsterRarity rarity && skills is List<SkillInfo> typedSkills)
            SetSkills(rarity, typedSkills);
    }

    public void SetSkills(MonsterRarity monsterRarity, List<SkillInfo> skills)
    {
        if (Skills == null || !Skills.ContainsKey(monsterRarity))
            return;

        Skills[monsterRarity] = skills;
    }

    public object GetNextSkill()
    {
        var entity = GameState?.SelectedEntity as SpawnedBionic;
        if (entity == null || Skills == null)
            return null;

        lock (_skillLock)
        {
            var rarity = MonsterRarity.General;

            if (TryGetMonsterRarity(entity, out var monsterRarity))
            {
                if (Skills.TryGetValue(monsterRarity, out var raritySkills) && raritySkills.Count > 0)
                    rarity = monsterRarity;
            }

            if (!Skills.TryGetValue(rarity, out var raritySkillsList) || raritySkillsList.Count == 0)
                return null;

            var player = Player;
            if (player == null)
                return null;

            var distance = player.Movement.Source.DistanceTo(entity.Movement.Source);
            var minDifference = int.MaxValue;
            var closestSkill = default(SkillInfo);

            if (entity.State.HitState == ActionHitStateFlag.KnockDown)
            {
                closestSkill = raritySkillsList.Find(p => p.Record.Params.Contains(25697));
            }
            else if (UseSkillsInOrder || distance < 10)
            {
                var counter = -1;
                var skillCount = raritySkillsList.Count;
                while (skillCount > 0 && counter < skillCount)
                {
                    counter++;
                    _lastIndex++;

                    if (_lastIndex > skillCount - 1)
                        _lastIndex = 0;

                    var selectedSkill = raritySkillsList[_lastIndex];
                    if (!selectedSkill.CanBeCasted)
                        continue;

                    closestSkill = selectedSkill;
                    break;
                }
            }
            else
            {
                for (var i = 0; i < raritySkillsList.Count; i++)
                {
                    var s = raritySkillsList[i];
                    if (!s.CanBeCasted)
                        continue;

                    var difference = Math.Abs(s.Record.Action_Range / 10 - distance);
                    if (minDifference > difference)
                    {
                        minDifference = (short)difference;
                        closestSkill = s;
                    }
                }
            }

            return closestSkill;
        }
    }

    public bool CheckSkillRequired(object skillRecord)
    {
        if (skillRecord is not RefSkill skill)
            return false;

        if (skill.ReqCommon_Mastery1 == 1)
            return true;

        var player = Player;
        if (player?.Inventory == null)
            return false;

        InventoryItem requiredItem;
        TypeIdFilter filter;

        var currentWeapon = player.Inventory.GetItemAt(6);
        if (skill.ReqCast_Weapon1 == WeaponType.Any)
        {
            var list = new List<TypeIdFilter>(8);

            for (var i = 0; i < skill.Params.Count; i++)
            {
                var param = skill.Params[i];
                if (param != 1919250793)
                    continue;

                var paramTypeId3 = (byte)skill.Params[++i];
                var paramTypeId4 = (byte)skill.Params[++i];
                list.Add(new TypeIdFilter(3, 1, paramTypeId3, paramTypeId4));
            }

            if (list.Count == 0)
                return true;

            filter = list.FirstOrDefault(p =>
                p.TypeID3 == currentWeapon?.Record.TypeID3 && p.TypeID4 == currentWeapon?.Record.TypeID4
            );
            if (filter != null)
                return true;

            filter = list.FirstOrDefault();
        }
        else
        {
            filter = new TypeIdFilter(p =>
                p.TypeID2 == 1
                && p.TypeID3 == 6
                && (
                    p.TypeID4 == (byte)skill.ReqCast_Weapon1
                    || ((byte)skill.ReqCast_Weapon2 != 0xFF && p.TypeID4 == (byte)skill.ReqCast_Weapon2)
                )
            );
        }

        requiredItem = player.Inventory.GetItemBest(filter);
        if (requiredItem == null)
            return false;

        var movingSlot = (byte)(requiredItem.Record.TypeID3 == 6 ? 6 : 7);
        if (requiredItem.Slot == movingSlot)
            return true;

        var result = requiredItem.Equip(movingSlot);

        if (movingSlot == 6 && requiredItem.Record.TwoHanded == 0)
        {
            filter = new TypeIdFilter(3, 1, 4, (byte)(player.Race == ObjectCountry.Chinese ? 1 : 2));
            var shieldItem = player.Inventory.GetItemBest(filter);
            if (shieldItem != null && shieldItem.Slot != 7)
                shieldItem.Equip(7);
        }

        return result;
    }

    public bool CastSkill(object skill, uint targetId = 0)
    {
        return CastSkillAsync(skill, targetId).GetAwaiter().GetResult();
    }

    public Task<bool> CastSkillAsync(object skill, uint targetId = 0)
    {
        if (skill is not SkillInfo skillInfo || skillInfo.Record is not RefSkill record)
            return Task.FromResult(false);

        var player = Player;
        if (player?.Skills?.HasSkill(skillInfo.Id) != true)
            return Task.FromResult(false);

        if (!SpawnManager.TryGetEntity<SpawnedBionic>(targetId, out var entity))
            return Task.FromResult(false);

        if (entity.State.LifeState == LifeState.Dead)
            return Task.FromResult(false);

        if (!CheckSkillRequired(record))
            return Task.FromResult(false);

        var packet = SkillUsePacketBuilder.BuildCastOnEntity(skillInfo.Id, targetId, GameState.ClientType);
        LogSkillAttack("Skill Attacking", targetId, entity);
        Runtime?.SendToServer(packet);

        return Task.FromResult(true);
    }

    public bool CastSkillOld(object skill, uint targetId = 0)
    {
        return CastSkillOldAsync(skill, targetId).GetAwaiter().GetResult();
    }

    public async Task<bool> CastSkillOldAsync(object skill, uint targetId = 0)
    {
        if (skill is not SkillInfo skillInfo || skillInfo.Record is not RefSkill record)
            return false;

        var player = Player;
        if (player?.Skills?.HasSkill(skillInfo.Id) != true)
            return false;

        if (!SpawnManager.TryGetEntity<SpawnedBionic>(targetId, out var entity))
            return false;

        if (entity.State.LifeState == LifeState.Dead)
            return false;

        if (!CheckSkillRequired(record))
            return false;

        var duration = CalculateLegacyCastDelay(skillInfo, entity, player);
        var packet = SkillUsePacketBuilder.BuildLegacyCastOnEntity(skillInfo.Id, targetId);
        var callback = Runtime?.CreateActionAckCallback();

        LogSkillAttack("Skill Attacking", targetId, entity);
        Runtime?.SendToServer(packet, callback);
        await Task.Delay(duration).ConfigureAwait(false);

        if (record.Basic_Activity != 1 && callback != null)
        {
            await Task.Run(() => Runtime.AwaitCallback(callback, duration)).ConfigureAwait(false);
            return Runtime.IsCallbackCompleted(callback);
        }

        return true;
    }

    public void CastBuff(object skill, uint target = 0, bool awaitBuffResponse = true)
    {
        CastBuffAsync(skill, target, awaitBuffResponse).GetAwaiter().GetResult();
    }

    public async Task CastBuffAsync(object skill, uint target = 0, bool awaitBuffResponse = true)
    {
        if (skill is not SkillInfo skillInfo || skillInfo.Id == 0 || skillInfo.Record is not RefSkill record)
            return;

        if (!CheckSkillRequired(record))
            return;

        var playerUniqueId = GameState.PlayerUniqueId;
        var targetId = target == 0 ? playerUniqueId : target;
        var targetsEntity = record.TargetGroup_Self || record.TargetGroup_Party;

        ServiceRuntime.Log?.Notify($"Casting skill (self-buff) [{record.GetRealName()}]");

        var packet = SkillUsePacketBuilder.BuildBuff(skillInfo.Id, targetsEntity, targetId);
        var asyncCallback = Runtime?.CreateBuffCastCallback(targetId, skillInfo.Id);
        var callback = Runtime?.CreateActionAckCallback();

        Runtime?.SendToServer(packet, asyncCallback, callback);

        if (awaitBuffResponse && asyncCallback != null)
        {
            var timeout = record.Action_CastingTime + record.Action_ActionDuration + record.Action_PreparingTime + 1500;
            await Task.Run(() => Runtime.AwaitCallback(asyncCallback, timeout)).ConfigureAwait(false);
        }

        if (record.Basic_Activity != 1 && awaitBuffResponse && callback != null)
            await Task.Run(() => Runtime.AwaitCallback(callback)).ConfigureAwait(false);
    }

    public void CastSkillAt(object skill, object target)
    {
        CastSkillAtAsync(skill, target).GetAwaiter().GetResult();
    }

    public async Task CastSkillAtAsync(object skill, object target)
    {
        if (skill is not SkillInfo skillInfo || skillInfo.Id == 0 || skillInfo.Record is not RefSkill record)
            return;

        if (target is not Position position || position.Region == 0 || position.DistanceToPlayer() > 100)
            return;

        if (!CheckSkillRequired(record))
            return;

        var packet = SkillUsePacketBuilder.BuildCastAtPosition(skillInfo.Id, position);
        var callback = Runtime?.CreateActionAckCallback();
        Runtime?.SendToServer(packet, callback);

        if (record.Basic_Activity != 1 && callback != null)
            await Task.Run(() => Runtime.AwaitCallback(callback, 1000)).ConfigureAwait(false);
    }

    public bool CastAutoAttack()
    {
        return CastAutoAttackAsync().GetAwaiter().GetResult();
    }

    public Task<bool> CastAutoAttackAsync()
    {
        if (GameState?.SelectedEntity is not SpawnedBionic entity)
            return Task.FromResult(false);

        if (entity.State.LifeState == LifeState.Dead)
            return Task.FromResult(false);

        var packet = SkillUsePacketBuilder.BuildAutoAttack(entity.UniqueId, GameState.ClientType);
        LogSkillAttack("Normal Attacking", entity.UniqueId, entity);
        Runtime?.SendToServer(packet);
        return Task.FromResult(true);
    }

    public void CastSkillAt(uint skillId, object position)
    {
        if (position is not Position target)
            return;

        var player = Player;
        if (player?.Skills?.HasSkill(skillId) != true)
            return;

        var packet = SkillUsePacketBuilder.BuildCastAtPositionExact(skillId, target);
        Runtime?.SendToServer(packet);
    }

    public void CancelBuff(uint skillId)
    {
        var player = Player;
        if (player?.Skills?.HasSkill(skillId) != true)
            return;

        var packet = SkillUsePacketBuilder.BuildCancelBuff(skillId);
        Runtime?.SendToServer(packet);
    }

    public bool CancelAction()
    {
        return CancelActionAsync().GetAwaiter().GetResult();
    }

    public async Task<bool> CancelActionAsync()
    {
        var packet = SkillUsePacketBuilder.BuildCancelAction();
        var callback = Runtime?.CreateActionAckCallback();

        Runtime?.SendToServer(packet, callback);

        if (callback == null)
            return false;

        await Task.Run(() => Runtime.AwaitCallback(callback)).ConfigureAwait(false);
        return Runtime.IsCallbackCompleted(callback);
    }

    private void EnsureBaseSkillsLoaded()
    {
        if (_baseSkills != null && !ReferenceEquals(_baseSkills, EmptySkills))
            return;

        _baseSkills = Config?.GetBaseSkills()?.ToArray() ?? EmptySkills;
    }

    private int CalculateLegacyCastDelay(SkillInfo skill, SpawnedBionic entity, Player player)
    {
        var distance = entity.DistanceToPlayer;
        var speed = player.ActualSpeed;
        var movingSleep = 0d;

        var skillRecord = skill.Record as RefSkill;
        var tel3Index = skillRecord.Params.FindIndex(new Predicate<int>(p => p == 1952803891));
        if (tel3Index != -1)
        {
            var tel3speed = skillRecord.Params[++tel3Index];
            var tel3meter = skillRecord.Params[++tel3Index] / 10;

            if (distance < tel3meter)
                movingSleep = distance / tel3speed;
            else
                movingSleep = (distance - tel3meter) / speed + tel3meter / tel3speed;
        }
        else
        {
            var range = skillRecord.Action_Range / 10;
            if (distance - 3 > range)
                movingSleep = (distance - range) / speed;
        }

        var duration = movingSleep < 0 ? 0 : (int)(movingSleep * 10000.0);
        var altSkill = skillRecord;
        while (altSkill != null)
        {
            duration += altSkill.Action_CastingTime + altSkill.Action_ActionDuration + altSkill.Action_PreparingTime;

            if (altSkill.Basic_ChainCode != 0)
                altSkill = Config?.GetRefSkill(altSkill.Basic_ChainCode) as RefSkill;
            else
                break;
        }

        return duration < 100 ? 1000 : duration;
    }

    private static bool TryGetMonsterRarity(SpawnedBionic entity, out MonsterRarity rarity)
    {
        rarity = MonsterRarity.General;

        var property = entity.GetType().GetProperty("Rarity");
        if (property == null)
            return false;

        var value = property.GetValue(entity);
        if (value is MonsterRarity typedRarity)
        {
            rarity = typedRarity;
            return true;
        }

        return false;
    }

    private static void LogSkillAttack(string action, uint targetId, SpawnedBionic entity)
    {
        ServiceRuntime.Log?.Debug(
            $"{action} to: {targetId} State: {entity.State.LifeState} Health: {entity.Health} HasHealth: {entity.HasHealth} Dst: {Math.Round(entity.DistanceToPlayer, 1)}"
        );
    }

    private static Player Player => ServiceRuntime.GameState?.Player as Player;
    private static IGameStateRuntimeContext GameState => ServiceRuntime.GameState;
    private static ISkillRuntime Runtime => ServiceRuntime.SkillRuntime;
    private static ISkillConfig Config => ServiceRuntime.SkillConfig;
}
