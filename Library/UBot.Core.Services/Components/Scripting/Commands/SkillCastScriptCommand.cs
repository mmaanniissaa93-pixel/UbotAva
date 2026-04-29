using System.Collections.Generic;
using UBot.Core.Services;

namespace UBot.Core.Components.Scripting.Commands;

internal class SkillCastScriptCommand : IScriptCommand
{
    public string Name => "cast";

    public bool IsBusy { get; private set; }

    public Dictionary<string, string> Arguments => new() { { "SkillId", "The Id of the skill to be cast" } };

    public bool Execute(string[] arguments = null)
    {
        if (arguments == null || arguments.Length != Arguments.Count)
            return false;

        try
        {
            IsBusy = true;

            if (ScriptManager.Runtime?.PlayerHasActiveVehicle == true)
            {
                ServiceRuntime.Log?.Warn("[Script] Cast skill command failed: Player is on a vehicle.");
                return false;
            }

            var skill = ScriptManager.Runtime?.GetPlayerSkillByCodeName(arguments[0]);
            if (skill == null)
            {
                ServiceRuntime.Log?.Warn("[Script] Cast skill command failed: Skill not known.");
                return false;
            }

            ScriptManager.Runtime.CastBuff(skill);
            return true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Stop()
    {
        IsBusy = false;
    }
}
