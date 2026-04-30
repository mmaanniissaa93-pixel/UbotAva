using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Components.Command;
using UBot.Core.Objects;

namespace UBot.CommandCenter.Components.Command;

internal class BuffCommandExecutor : ICommandExecutor
{
    public string CommandName => "buff";

    public string CommandDescription => "Cast all buffs";

    public bool Execute(bool silent)
    {
        if (!silent)
            UBot.Core.RuntimeAccess.Session.ShowNotification("[UBot] Casting all buffs");

        var buffs = SkillManager.Buffs.FindAll(p => !UBot.Core.RuntimeAccess.Session.Player.State.HasActiveBuff(p, out _) && p.CanBeCasted);
        if (buffs.Count == 0)
            return true;

        Log.Status("Buffing");

        foreach (var buff in buffs)
        {
            if (
                UBot.Core.RuntimeAccess.Session.Player.State.LifeState != LifeState.Alive
                || UBot.Core.RuntimeAccess.Session.Player.HasActiveVehicle
                || UBot.Core.RuntimeAccess.Session.Player.Untouchable
            )
                break;

            Log.Debug($"[ActionMapper] Casting buff {buff} ({buff.Record.Basic_Code})");

            buff.Cast(buff: true);
        }

        return true;
    }
}
