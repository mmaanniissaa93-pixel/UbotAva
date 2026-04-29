using System.Collections.Generic;
using UBot.Core.Services;

namespace UBot.Core.Components.Scripting.Commands;

internal class RepairScriptCommand : IScriptCommand
{
    public string Name => "repair";

    public bool IsBusy { get; private set; }

    public Dictionary<string, string> Arguments => new() { { "Codename", "The code name of the NPC" } };

    public bool Execute(string[] arguments = null)
    {
        if (arguments == null || arguments.Length != Arguments.Count)
        {
            ServiceRuntime.Log?.Warn("[Script] Invalid repair command: NPC code name information missing.");
            return false;
        }

        if (!ScriptManager.Running)
        {
            IsBusy = false;
            return false;
        }

        try
        {
            IsBusy = true;
            ServiceRuntime.Log?.Notify("[Script] Repairing items...");
            ServiceRuntime.Shopping?.RepairItems(arguments[0]);
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
