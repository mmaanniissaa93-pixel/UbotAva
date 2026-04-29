using System.Collections.Generic;
using UBot.Core.Services;

namespace UBot.Core.Components.Scripting.Commands;

internal class TeleportScriptCommand : IScriptCommand
{
    public string Name => "teleport";

    public bool IsBusy { get; private set; }

    public Dictionary<string, string> Arguments =>
        new() { { "Codename", "The code name of the NPC" }, { "Destination", "The id of the destination" } };

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
            ServiceRuntime.Log?.Notify("[Script] Teleporting...");
            return ExecuteTeleport(arguments);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static bool ExecuteTeleport(IReadOnlyList<string> arguments)
    {
        if (arguments.Count < 2)
            return false;

        var npcCodeName = arguments[0];
        return uint.TryParse(arguments[1], out var destination)
            && ScriptManager.Runtime?.Teleport(npcCodeName, destination) == true;
    }

    public void Stop()
    {
        IsBusy = false;
    }
}
