using System.Collections.Generic;
using System.Threading.Tasks;
using UBot.Core.Services;

namespace UBot.Core.Components.Scripting.Commands;

internal class WaitScriptCommand : IScriptCommand
{
    public string Name => "wait";

    public bool IsBusy { get; private set; }

    public Dictionary<string, string> Arguments => new() { { "Time", "The time to wait in milliseconds" } };

    public bool Execute(string[] arguments = null) => ExecuteAsync(arguments).GetAwaiter().GetResult();

    public async Task<bool> ExecuteAsync(string[] arguments = null)
    {
        if (arguments == null || arguments.Length == 0)
        {
            ServiceRuntime.Log?.Warn("[Script] Invalid wait command: Waiting time information missing.");
            return false;
        }

        try
        {
            IsBusy = true;
            ServiceRuntime.Log?.Notify("[Script] Waiting...");

            if (!int.TryParse(arguments[0], out var time))
                return false;

            await Task.Delay(time).ConfigureAwait(false);
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
