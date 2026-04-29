using System.Collections.Generic;
using System.Threading.Tasks;

namespace UBot.Core.Components.Scripting;

public interface IScriptCommand
{
    string Name { get; }

    bool IsBusy { get; }

    Dictionary<string, string> Arguments { get; }

    bool Execute(string[] arguments = null);

    Task<bool> ExecuteAsync(string[] arguments = null)
    {
        return Task.FromResult(Execute(arguments));
    }

    void Stop();
}
