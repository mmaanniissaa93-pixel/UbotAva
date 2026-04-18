using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Components.Command;

namespace UBot.CommandCenter.Components.Command;

internal class StartHereCommandExecutor : ICommandExecutor
{
    public string CommandName => "here";

    public string CommandDescription => "Set training area and start bot";

    public bool Execute(bool silent)
    {
        if (!silent)
            Game.ShowNotification("[UBot] Starting bot at the current location");

        return CommandManager.Execute("area", true) && CommandManager.Execute("start", true);
    }
}
