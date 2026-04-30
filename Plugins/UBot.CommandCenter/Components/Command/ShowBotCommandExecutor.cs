using UBot.Core;
using UBot.Core.Components.Command;
using UBot.Core.Event;

namespace UBot.CommandCenter.Components.Command;

internal class ShowBotCommandExecutor : ICommandExecutor
{
    public string CommandName => "show";

    public string CommandDescription => "Show the bot window";

    public bool Execute(bool silent)
    {
        if (!silent)
            UBot.Core.RuntimeAccess.Session.ShowNotification("[UBot] Showing bot window");

        UBot.Core.RuntimeAccess.Events.FireEvent("OnShowBotWindow");

        return true;
    }
}
