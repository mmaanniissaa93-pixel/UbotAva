using UBot.Core;
using UBot.Core.Components.Command;

namespace UBot.CommandCenter.Components.Command;

internal class StartCommandExecutor : ICommandExecutor
{
    public string CommandName => "start";

    public string CommandDescription => "Start the bot";

    public bool Execute(bool silent)
    {
        if (!silent)
            UBot.Core.RuntimeAccess.Session.ShowNotification($"[UBot] Starting bot [{UBot.Core.RuntimeAccess.Core.Bot?.Botbase.Title}]");

        UBot.Core.RuntimeAccess.Core.Bot?.Start();

        return UBot.Core.RuntimeAccess.Core.Bot?.Running == true;
    }
}
