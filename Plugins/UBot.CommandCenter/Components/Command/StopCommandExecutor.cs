using UBot.Core;
using UBot.Core.Components.Command;

namespace UBot.CommandCenter.Components.Command;

internal class StopCommandExecutor : ICommandExecutor
{
    public string CommandName => "stop";

    public string CommandDescription => "Stop the bot";

    public bool Execute(bool silent)
    {
        if (!silent)
            Game.ShowNotification($"[UBot] Stopping bot [{Kernel.Bot?.Botbase.Title}]");

        Kernel.Bot?.Stop();

        return Kernel.Bot?.Running == false;
    }
}
