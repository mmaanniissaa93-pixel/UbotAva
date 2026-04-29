using System.Collections.Generic;
using UBot.Core.Services;

namespace UBot.Core.Components.Scripting.Commands;

internal class StoreScriptCommand : IScriptCommand
{
    public string Name => "store";

    public bool IsBusy { get; private set; }

    public Dictionary<string, string> Arguments => new() { { "Codename", "The code name of the NPC" } };

    public bool Execute(string[] arguments = null)
    {
        if (arguments == null || arguments.Length == 0)
        {
            ServiceRuntime.Log?.Warn("[Script] Invalid store command: Position information missing.");
            return false;
        }

        try
        {
            IsBusy = true;
            ServiceRuntime.Log?.Notify("[Script] storing items...");
            ServiceRuntime.Shopping?.StoreItems(arguments[0]);

            if (ScriptManager.Runtime?.GetConfigBool("UBot.Inventory.AutoSort", false) == true)
                ServiceRuntime.Shopping?.SortItems(arguments[0]);

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
