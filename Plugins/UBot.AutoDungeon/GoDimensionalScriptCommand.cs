using System.Collections.Generic;
using System.Linq;
using UBot.Core.Components.Scripting;

namespace UBot.AutoDungeon;

internal class GoDimensionalScriptCommand : IScriptCommand
{
    private volatile bool _stopRequested;

    public string Name => "GoDimensional";

    public bool IsBusy { get; private set; }

    public Dictionary<string, string> Arguments =>
        new()
        {
            { "ItemName", "Optional dimensional item name" },
        };

    public bool Execute(string[] arguments = null)
    {
        try
        {
            IsBusy = true;
            _stopRequested = false;

            var itemName = arguments == null || arguments.Length == 0
                ? string.Empty
                : string.Join(" ", arguments.Where(arg => !string.IsNullOrWhiteSpace(arg))).Trim();

            if (_stopRequested)
                return false;

            return AutoDungeonState.StartGoDimensionalFlow(itemName);
        }
        finally
        {
            IsBusy = false;
        }
    }

    public void Stop()
    {
        _stopRequested = true;
        IsBusy = false;
    }
}
