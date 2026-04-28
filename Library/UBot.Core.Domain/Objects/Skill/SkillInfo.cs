using System;
using UBot.Core.Abstractions;

namespace UBot.Core.Objects.Skill;

public class SkillInfo
{
    private int _cooldownTick;
    private readonly int _duration;
    private int _lastCastTick;
    private int _testTick;

    public bool Enabled;
    public uint Id;

    public SkillInfo(uint id, uint token)
        : this(id, false)
    {
        Token = token;
        _lastCastTick = CurrentTick;
        _testTick = CurrentTick;
    }

    public SkillInfo(uint id, bool enabled)
    {
        Id = id;
        Enabled = enabled;

        var record = Record;
        if (record == null || IsPassive)
            return;

        var index = record.Params.IndexOf(1685418593);
        if (index != -1)
            _duration = record.Params[index + 1];
    }

    public dynamic Record => ReferenceProvider.Instance?.GetRefSkill(Id);

    public bool IsPassive => Record?.Basic_Activity == 0;

    public bool IsAttack => Record?.Params.Contains(6386804) == true;

    public bool IsDot => Record?.Basic_Code.StartsWith("SKILL_EU_WARLOCK_DOTA") == true;

    public bool IsImbue => Record?.Basic_Activity == 1 && IsAttack;

    public bool HasCooldown => CurrentTick - _cooldownTick < GetReuseDelay();

    public bool CanNotBeCasted
    {
        get
        {
            if (_lastCastTick == 0)
                return false;

            return CurrentTick - _lastCastTick < _duration;
        }
    }

    [Obsolete]
    public bool Isbugged => ComputeIsBugged();

    public bool IsBugged => ComputeIsBugged();

    public bool CanBeCasted
    {
        get
        {
            if (HasCooldown)
                return false;

            var record = Record;
            if (record == null)
                return false;

            if (RuntimeContext != null && RuntimeContext.PlayerMana < record.Consume_MP)
                return false;

            return !CanNotBeCasted;
        }
    }

    public uint Token { get; set; }

    public int RemainingMilliseconds => _duration == 0 ? 0 : Math.Max(0, _duration - (CurrentTick - _lastCastTick));

    public double RemainingPercent =>
        _duration == 0 ? 0.0 : Math.Max(0.0, Math.Min(1.0, (double)RemainingMilliseconds / _duration));

    public int CooldownRemainingMilliseconds
    {
        get
        {
            var reuse = GetReuseDelay();
            if (reuse <= 0)
                return 0;

            var elapsed = CurrentTick - _cooldownTick;
            return Math.Max(0, reuse - elapsed);
        }
    }

    public string CooldownFormatted
    {
        get
        {
            var ms = CooldownRemainingMilliseconds;
            var seconds = ms / 1000;
            var minutes = seconds / 60;
            seconds %= 60;
            return $"{minutes:D2}:{seconds:D2}";
        }
    }

    public double CooldownPercent
    {
        get
        {
            var reuse = GetReuseDelay();
            if (reuse <= 0)
                return 0.0;

            return Math.Max(0.0, Math.Min(1.0, (double)CooldownRemainingMilliseconds / reuse));
        }
    }

    public int CooldownEndTick
    {
        get
        {
            var reuse = GetReuseDelay();
            if (reuse <= 0)
                return 0;

            return _cooldownTick + reuse;
        }
    }

    public void Update()
    {
        _cooldownTick = CurrentTick;
        _lastCastTick = CurrentTick;
        _testTick = CurrentTick;
    }

    public void SetCoolDown(int milliseconds)
    {
        _cooldownTick = CurrentTick - milliseconds;
    }

    public void Reset()
    {
        _lastCastTick = 0;
        Token = 0;
    }

    public bool IsLowLevel()
    {
        var record = Record;
        if (record == null || RuntimeContext == null)
            return true;

        byte? masteryLevel = RuntimeContext.GetMasteryLevel((uint)record.ReqCommon_Mastery1);
        if (masteryLevel == null)
            return true;

        return record.ReqCommon_MasteryLevel1 < masteryLevel.Value - 20;
    }

    public void Cast(uint target = 0, bool buff = false)
    {
        RuntimeContext?.Cast(this, target, buff);
    }

    public void CastAt(Position target)
    {
        RuntimeContext?.CastAt(this, target);
    }

    public override string ToString()
    {
        return $"{Record}";
    }

    private bool ComputeIsBugged()
    {
        if (_testTick == 0)
            return false;

        return CurrentTick - _testTick > _duration + 10000 && _duration != 0;
    }

    private int GetReuseDelay()
    {
        var record = Record;
        return record == null ? 0 : (int)record.Action_ReuseDelay;
    }

    private static int CurrentTick => RuntimeContext?.TickCount ?? Environment.TickCount;

    public static ISkillInfoRuntimeContext RuntimeContext { get; set; }
}

public interface ISkillInfoRuntimeContext
{
    int TickCount { get; }
    int PlayerMana { get; }
    byte? GetMasteryLevel(uint masteryId);
    void Cast(SkillInfo skill, uint target, bool buff);
    void CastAt(SkillInfo skill, Position target);
}
