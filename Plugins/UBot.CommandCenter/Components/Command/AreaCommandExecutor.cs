using UBot.Core;
using UBot.Core.Components.Command;
using UBot.Core.Event;

namespace UBot.CommandCenter.Components.Command;

internal class AreaCommandExecutor : ICommandExecutor
{
    public string CommandName => "area";

    public string CommandDescription => "Set the training area";

    public bool Execute(bool silent)
    {
        if (!silent)
            UBot.Core.RuntimeAccess.Session.ShowNotification(
                $"[UBot] Setting training area to X={UBot.Core.RuntimeAccess.Session.Player.Position.X:0.00} Y={UBot.Core.RuntimeAccess.Session.Player.Position.Y:0.00} R=50"
            );

        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Region", UBot.Core.RuntimeAccess.Session.Player.Position.Region);
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.X", UBot.Core.RuntimeAccess.Session.Player.Position.XOffset.ToString("0.0"));
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Y", UBot.Core.RuntimeAccess.Session.Player.Position.YOffset.ToString("0.0"));
        UBot.Core.RuntimeAccess.Player.Set("UBot.Area.Z", UBot.Core.RuntimeAccess.Session.Player.Position.ZOffset.ToString("0.0"));
        UBot.Core.RuntimeAccess.Player.Get("UBot.Area.Radius", 50);

        UBot.Core.RuntimeAccess.Events.FireEvent("OnSetTrainingArea");

        return true;
    }
}
