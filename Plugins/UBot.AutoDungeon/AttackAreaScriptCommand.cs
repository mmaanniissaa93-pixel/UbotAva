using System;
using System.Collections.Generic;
using System.Threading;
using UBot.Core;
using UBot.Core.Components.Scripting;
using UBot.Core.Objects;

namespace UBot.AutoDungeon;

internal class AttackAreaScriptCommand : IScriptCommand
{
    private volatile bool _stopRequested;

    public string Name => "AttackArea";

    public bool IsBusy { get; private set; }

    public Dictionary<string, string> Arguments =>
        new()
        {
            { "Radius", "Optional radius in meters" },
        };

    public bool Execute(string[] arguments = null)
    {
        if (!Game.Ready || Game.Player == null)
        {
            Log.Warn("[AutoDungeon] AttackArea failed: character is not in game.");
            return false;
        }

        try
        {
            IsBusy = true;
            _stopRequested = false;

            double? radius = null;
            if (arguments is { Length: > 0 } && !string.IsNullOrWhiteSpace(arguments[0]))
            {
                if (!double.TryParse(arguments[0], out var parsedRadius) || parsedRadius <= 0)
                {
                    Log.Warn("[AutoDungeon] AttackArea failed: invalid radius value.");
                    return false;
                }

                radius = Math.Round(parsedRadius, 2);
            }

            var startPosition = Game.Player.Movement.Source;
            var monitoredMonsters = AutoDungeonState.GetMonsterCounterSnapshot(startPosition, radius).Count;
            if (monitoredMonsters == 0)
            {
                Log.Notify("[AutoDungeon] No matching monsters found in this area.");
                return true;
            }

            Log.Notify(
                radius.HasValue
                    ? $"[AutoDungeon] AttackArea started. Radius: {radius.Value:0.##}"
                    : "[AutoDungeon] AttackArea started. Radius: max"
            );

            var originalArea = new TrainingAreaSnapshot(
                PlayerConfig.Get<ushort>("UBot.Area.Region"),
                PlayerConfig.Get<float>("UBot.Area.X"),
                PlayerConfig.Get<float>("UBot.Area.Y"),
                PlayerConfig.Get<float>("UBot.Area.Z"),
                Math.Clamp(PlayerConfig.Get("UBot.Area.Radius", 50), 5, 100)
            );

            var areaRadius = radius.HasValue
                ? Math.Clamp((int)Math.Round(radius.Value), 5, 100)
                : 100;

            AutoDungeonState.SetTrainingArea(startPosition, areaRadius);

            if (!Kernel.Bot.Running)
            {
                Kernel.Bot.Start();
                Thread.Sleep(250);
            }

            var cleared = AutoDungeonState.WaitUntilAreaCleared(
                startPosition,
                radius,
                timeoutSeconds: 600,
                continueCondition: () => !_stopRequested && Kernel.Bot.Running
            );

            AutoDungeonState.WaitForPotentialDrops(startPosition, areaRadius, maxSeconds: 10);

            var restorePosition = new Position(
                originalArea.Region,
                originalArea.X,
                originalArea.Y,
                originalArea.Z
            );
            AutoDungeonState.SetTrainingArea(restorePosition, originalArea.Radius);

            if (Game.Player.Movement.Source.DistanceTo(startPosition) > 4)
                Game.Player.MoveTo(startPosition, false);

            if (cleared)
                Log.Notify("[AutoDungeon] AttackArea finished. Area restored.");

            return true;
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

    private sealed record TrainingAreaSnapshot(ushort Region, float X, float Y, float Z, int Radius);
}
