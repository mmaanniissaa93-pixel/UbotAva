using System.Collections.Generic;
using System.Threading.Tasks;
using UBot.Core.Services;

namespace UBot.Core.Components.Scripting.Commands;

internal class SuppliesScriptCommand : IScriptCommand
{
    public string Name => "supply";

    public bool IsBusy { get; private set; }

    public Dictionary<string, string> Arguments => new() { { "Codename", "The code name of the NPC" } };

    public bool Execute(string[] arguments = null) => ExecuteAsync(arguments).GetAwaiter().GetResult();

    public async Task<bool> ExecuteAsync(string[] arguments = null)
    {
        if (arguments == null || arguments.Length < Arguments.Count)
        {
            ServiceRuntime.Log?.Warn("[Script] Invalid buy command: NPC code name information missing.");
            return false;
        }

        try
        {
            IsBusy = true;
            ServiceRuntime.Log?.Notify("[Script] Receiving supplies...");

            var shopping = ServiceRuntime.Shopping;
            if (shopping == null)
                return false;

            shopping.ReceiveSupplies(arguments[0]);
            while (!shopping.Finished)
                await Task.Delay(50).ConfigureAwait(false);

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
