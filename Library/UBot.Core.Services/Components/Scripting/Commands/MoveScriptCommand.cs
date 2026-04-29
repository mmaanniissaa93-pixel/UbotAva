using System.Collections.Generic;
using System.Threading.Tasks;
using UBot.Core.Abstractions.Services;
using UBot.Core.Objects;
using UBot.Core.Services;

namespace UBot.Core.Components.Scripting.Commands;

public class MoveScriptCommand : IScriptCommand
{
    public string Name => "move";

    public bool IsBusy { get; private set; }

    public static bool MustDismount { get; set; }

    public Dictionary<string, string> Arguments =>
        new()
        {
            { "XSector", "The X sector of the region" },
            { "YSector", "The Y sector of the region" },
            { "XOffset", "The X offset inside the region" },
            { "YOffset", "The Y offset inside the region" },
            { "ZOffset", "The Z offset inside the region" },
        };

    public bool Execute(string[] arguments = null) => ExecuteAsync(arguments).GetAwaiter().GetResult();

    public async Task<bool> ExecuteAsync(string[] arguments = null)
    {
        if (arguments == null || arguments.Length != Arguments.Count)
        {
            ServiceRuntime.Log?.Warn("[Script] Invalid move command: Position information missing / invalid format.");
            return false;
        }

        if (IsBusy)
            return false;

        try
        {
            IsBusy = true;

            while (Runtime?.PlayerInAction == true)
                await Task.Delay(100).ConfigureAwait(false);

            const int retryAttempts = 5;
            var stepRetryCounter = 0;

            while (!await ExecuteMoveAsync(arguments).ConfigureAwait(false))
            {
                if (!IsBusy)
                    return false;

                if (stepRetryCounter++ >= retryAttempts)
                {
                    ServiceRuntime.Log?.Warn("[Script] The move command failed due to an unknown reason! Please check the walk script.");
                    return false;
                }

                ServiceRuntime.Log?.Debug($"[Script] Retry this step {stepRetryCounter}/{retryAttempts}...");
                await Task.Delay(1000).ConfigureAwait(false);
            }

            return true;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task<bool> ExecuteMoveAsync(IReadOnlyList<string> arguments)
    {
        if (
            !float.TryParse(arguments[0], out var xOffset)
            || !float.TryParse(arguments[1], out var yOffset)
            || !float.TryParse(arguments[2], out var zOffset)
            || !byte.TryParse(arguments[3], out var xSector)
            || !byte.TryParse(arguments[4], out var ySector)
        )
        {
            IsBusy = false;
            return false;
        }

        var runtime = Runtime;
        if (runtime == null || runtime.PlayerPosition == null)
            return false;

        var previousPosition = runtime.PlayerPosition;
        var position = runtime.CreatePosition(xSector, ySector, xOffset, yOffset, zOffset);

        UseMovementPreparations(runtime, previousPosition);

        var distance = runtime.Distance(position, previousPosition);
        if (distance > 100)
        {
            ServiceRuntime.Log?.Warn("[Script] Target position too far away, bot logic aborted!");
            IsBusy = false;
            return false;
        }

        ServiceRuntime.Log?.Debug(GetMoveMessage(position));
        ScriptManager.Report(ScriptExecutionState.Movement, ScriptManager.CurrentLineIndex, Name, GetMoveMessage(position), position);

        var result = runtime.MovePlayerTo(position);
        if (result)
            await Task.Delay(runtime.EstimateMoveDelayMilliseconds(previousPosition, position)).ConfigureAwait(false);

        if (!MustDismount)
            return result;

        MustDismount = false;
        runtime.DismountVehicle();
        var previousPositionResult = runtime.MovePlayerTo(previousPosition);
        if (previousPositionResult)
            await Task.Delay(runtime.EstimateMoveDelayMilliseconds(position, previousPosition)).ConfigureAwait(false);

        return previousPositionResult;
    }

    public void Stop()
    {
        IsBusy = false;
    }

    private static void UseMovementPreparations(IScriptRuntime runtime, object previousPosition)
    {
        if (runtime.GetConfigBool("UBot.Training.checkUseSpeedDrug", true))
        {
            if (!runtime.PlayerHasActiveVehicle && !runtime.PlayerInAction && !runtime.HasActiveSpeedBuff())
                runtime.UseSpeedDrug();
        }

        if (!runtime.GetConfigBool("UBot.Training.checkUseMount", true))
            return;

        if (
            !runtime.PlayerHasActiveVehicle
            && !runtime.PlayerInAction
            && !(ScriptManager.Running && ScriptManager.File == runtime.GetConfigString("UBot.Lure.SelectedScriptPath", string.Empty))
        )
        {
            runtime.SummonFellow();

            if (runtime.FellowPosition != null)
            {
                runtime.CastFellowSkill("P2SKILL_SPECIAL_SP_GET_A");

                var distanceToFellow = runtime.Distance(previousPosition, runtime.FellowPosition);
                if (distanceToFellow <= 5.0)
                    runtime.MountFellow();
                else
                    ServiceRuntime.Log?.Debug($"Can't mount fellow pet because it's {distanceToFellow}m away");
            }
        }

        if (!runtime.PlayerHasActiveVehicle && !runtime.PlayerIsInDungeon && !runtime.PlayerInAction)
            runtime.SummonVehicle();
    }

    private static string GetMoveMessage(object position)
    {
        if (position is Position pos)
            return $"[Script] Move to position {pos.Region}({pos.Region.X},{pos.Region.Y}) X={pos.X}, Y={pos.Y}";

        return "[Script] Move to position";
    }

    private static IScriptRuntime Runtime => ScriptManager.Runtime;
}
