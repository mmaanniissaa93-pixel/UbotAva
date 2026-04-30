using System.Collections.Generic;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Objects;

namespace UBot.Training.Bundle.Resurrect;

internal class ResurrectBundle : IBundle
{
    /// <summary>
    ///     The Last resurrect party members
    /// </summary>
    public Dictionary<string, int> _lastResurrectedPlayers = new();

    public void Invoke()
    {
        if (UBot.Core.RuntimeAccess.Session.Party == null || UBot.Core.RuntimeAccess.Session.Party.Members == null || UBot.Core.RuntimeAccess.Session.Player.HasActiveVehicle)
            return;

        if (!UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Skills.checkResurrectParty"))
            return;

        ushort resDelay = UBot.Core.RuntimeAccess.Player.Get<ushort>("UBot.Skills.numResDelay", 120);
        ushort resRadius = UBot.Core.RuntimeAccess.Player.Get<ushort>("UBot.Skills.numResRadius", 100);

        foreach (var member in UBot.Core.RuntimeAccess.Session.Party.Members)
        {
            if (
                _lastResurrectedPlayers.ContainsKey(member.Name)
                && UBot.Core.RuntimeAccess.Core.TickCount - _lastResurrectedPlayers[member.Name] < resDelay * 1000
            )
                continue;

            if (
                (
                    member.Player?.Movement.Source.DistanceTo(UBot.Core.RuntimeAccess.Session.Player.Movement.Source)
                    ?? member.Position.DistanceTo(UBot.Core.RuntimeAccess.Session.Player.Movement.Source)
                ) > resRadius
                || (
                    member.Player?.Movement.Source.HasCollisionBetween(UBot.Core.RuntimeAccess.Session.Player.Movement.Source)
                    ?? member.Position.HasCollisionBetween(UBot.Core.RuntimeAccess.Session.Player.Movement.Source)
                )
            )
                continue;

            if (member.Player?.State.LifeState != LifeState.Dead && (member.HealthMana & 0x0F) != 0)
                continue;

            if (!_lastResurrectedPlayers.ContainsKey(member.Name))
                _lastResurrectedPlayers.Add(member.Name, UBot.Core.RuntimeAccess.Core.TickCount);
            else
                _lastResurrectedPlayers[member.Name] = UBot.Core.RuntimeAccess.Core.TickCount;

            var moved = UBot.Core.RuntimeAccess.Session.Player.MoveTo(member.Player?.Movement.Source ?? member.Position, true);
            if (!moved)
                continue;

            Log.Status($"Resurrecting player {member.Name}");
            SkillManager.ResurrectionSkill?.Cast(member.Player?.UniqueId ?? member.MemberId, true);
        }
    }

    /// <summary>
    ///     Refreshes this instance.
    /// </summary>
    public void Refresh()
    {
        //Nothing to do here
    }

    public void Stop()
    {
        //Nothing to do here
    }
}
