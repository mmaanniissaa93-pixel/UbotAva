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
            Game.ShowNotification(
                $"[UBot] Setting training area to X={Game.Player.Position.X:0.00} Y={Game.Player.Position.Y:0.00} R=50"
            );

        PlayerConfig.Set("UBot.Area.Region", Game.Player.Position.Region);
        PlayerConfig.Set("UBot.Area.X", Game.Player.Position.XOffset.ToString("0.0"));
        PlayerConfig.Set("UBot.Area.Y", Game.Player.Position.YOffset.ToString("0.0"));
        PlayerConfig.Set("UBot.Area.Z", Game.Player.Position.ZOffset.ToString("0.0"));
        PlayerConfig.Get("UBot.Area.Radius", 50);

        EventManager.FireEvent("OnSetTrainingArea");

        return true;
    }
}
