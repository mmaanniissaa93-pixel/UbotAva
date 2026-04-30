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
            UBot.Core.RuntimeAccess.Session.ShowNotification($"[UBot] Stopping bot [{UBot.Core.RuntimeAccess.Core.Bot?.Botbase.Title}]");

        UBot.Core.RuntimeAccess.Core.Bot?.Stop();

        return UBot.Core.RuntimeAccess.Core.Bot?.Running == false;
    }
}
