using System.Collections.Generic;
using System.Threading.Tasks;
using UBot.Core.Services;

namespace UBot.Core.Components.Scripting.Commands;

internal class DismountScriptCommand : IScriptCommand
{
    public string Name => "dismount";

    public bool IsBusy { get; private set; }

    public Dictionary<string, string> Arguments => null;

    public bool Execute(string[] arguments = null) => ExecuteAsync(arguments).GetAwaiter().GetResult();

    public async Task<bool> ExecuteAsync(string[] arguments = null)
    {
        try
        {
            IsBusy = true;
            ServiceRuntime.Log?.Notify("[Script] Dismounting vehicle...");

            if (ScriptManager.Runtime?.PlayerHasActiveVehicle != true)
                return true;

            ScriptManager.Runtime.DismountVehicle();
            await Task.Delay(500).ConfigureAwait(false);
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
