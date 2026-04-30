using System.Linq;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Training.Bundle.Buff;

internal class BuffBundle : IBundle
{
    private bool _invoked;
    private bool _buffBetweenAttacks { get; set; }

    /// <summary>
    ///     Invokes this instance.
    /// </summary>
    public void Invoke()
    {
        if (_invoked)
            return;

        if ((UBot.Core.RuntimeAccess.Session.Player.Untouchable || UBot.Core.RuntimeAccess.Session.Player.InAction) && !_buffBetweenAttacks)
            return;
        if ((UBot.Core.RuntimeAccess.Session.Player.Untouchable || UBot.Core.RuntimeAccess.Session.Player.Berzerking) && _buffBetweenAttacks)
            return;

        try
        {
            _invoked = true;

            /*
             * #377
             * I think the bug now fixed but As a precaution, I find it appropriate to keep this solution here.
             * If the last fix is working, I will remove this code block from here in v2.4
             * Temporary fixer:
             * Issue: Sometimes the buffs dont removing with token from the active buffs list
             *      Problems:
             *          ActionBuffRemoveResponse:0xb072 does not calling
             *          There have another opcode for remove buff with token
             */
            foreach (
                var buff in SkillManager.Buffs.Union(new[] { SkillManager.ImbueSkill, SkillManager.ResurrectionSkill })
            )
            {
                if (buff == null)
                    continue;

                var isActive = UBot.Core.RuntimeAccess.Session.Player.State.HasActiveBuff(buff, out var info);
                if (isActive && buff.IsBugged && info.IsBugged)
                {
                    //#377 bug detected!
                    Log.Notify($"[#377] The buff [{buff.Token}-{buff.Record?.GetRealName()}] expired");

                    UBot.Core.RuntimeAccess.Events.FireEvent("OnRemoveBuff", buff);

                    var playerSkill = UBot.Core.RuntimeAccess.Session.Player.Skills.GetSkillInfoById(buff.Id);
                    playerSkill?.Reset();
                    UBot.Core.RuntimeAccess.Session.Player.State.TryRemoveActiveBuff(info.Token, out _);
                }
            }

            var buffs = SkillManager.Buffs.FindAll(p => !UBot.Core.RuntimeAccess.Session.Player.State.HasActiveBuff(p, out _) && p.CanBeCasted);
            if (buffs == null || buffs.Count == 0)
                return;

            Log.Status("Buffing");

            foreach (var buff in buffs)
            {
                if (UBot.Core.RuntimeAccess.Session.Player.State.LifeState != LifeState.Alive || UBot.Core.RuntimeAccess.Session.Player.HasActiveVehicle)
                    break;

                if (UBot.Core.RuntimeAccess.Session.Player.State.HasActiveBuff(buff, out _) && !buff.HasCooldown)
                    break;

                Log.Debug($"Trying to cast buff: {buff} {buff.Record.Basic_Code}");

                buff.Cast(buff: true);
            }
        }
        finally
        {
            _invoked = false;
        }
    }

    /// <summary>
    ///     Refreshes this instance.
    /// </summary>
    public void Refresh()
    {
        _buffBetweenAttacks = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Skills.checkCastBuffsBetweenAttacks", false);
        _invoked = false;
    }

    public void Stop()
    {
        _invoked = false;
    }
}
