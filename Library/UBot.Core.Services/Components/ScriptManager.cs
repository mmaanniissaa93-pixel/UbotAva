using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UBot.Core.Abstractions.Services;
using UBot.Core.Components.Scripting;
using UBot.Core.Objects;
using UBot.Core.Services;

namespace UBot.Core.Components;

public enum ScriptValidationSeverity
{
    Warning = 0,
    Error = 1,
}

public sealed class ScriptValidationIssue(
    int lineNumber,
    string command,
    string message,
    ScriptValidationSeverity severity
)
{
    public int LineNumber { get; } = lineNumber;
    public string Command { get; } = command ?? "<none>";
    public string Message { get; } = message ?? string.Empty;
    public ScriptValidationSeverity Severity { get; } = severity;
}

public sealed class ScriptValidationResult
{
    public List<ScriptValidationIssue> Issues { get; } = [];
    public int StartLineIndex { get; set; }
    public int SimulatedCommands { get; set; }
    public bool IsValid => Issues.All(issue => issue.Severity != ScriptValidationSeverity.Error);
}

public class ScriptManager
{
    private static readonly object _commandHandlersLock = new();

    public static string InitialDirectory => Path.Combine(Runtime?.BasePath ?? string.Empty, "Data", "Scripts");

    public static string File { get; set; }

    public static string[] Commands { get; set; }

    public static bool Running { get; set; }

    public static List<IScriptCommand> CommandHandlers { get; private set; }

    public static int CurrentLineIndex { get; private set; }

    public static char ArgumentSeparator { get; set; } = ' ';

    public static bool Paused { get; private set; }

    public static void Initialize()
    {
        lock (_commandHandlersLock)
            CommandHandlers = new List<IScriptCommand>(10);

        var type = typeof(IScriptCommand);
        var types = AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract)
            .ToArray();

        foreach (var handler in types)
        {
            var instance = (IScriptCommand)Activator.CreateInstance(handler);
            RegisterCommandHandler(instance);
        }
    }

    public static bool RegisterCommandHandler(IScriptCommand handler, bool replaceExisting = true)
    {
        if (handler == null || string.IsNullOrWhiteSpace(handler.Name))
            return false;

        lock (_commandHandlersLock)
        {
            CommandHandlers ??= new List<IScriptCommand>(10);

            var existing = CommandHandlers.FirstOrDefault(command =>
                command.Name.Equals(handler.Name, StringComparison.OrdinalIgnoreCase)
            );
            if (existing != null)
            {
                if (!replaceExisting)
                    return false;

                CommandHandlers.Remove(existing);
            }

            CommandHandlers.Add(handler);
        }

        return true;
    }

    public static bool UnregisterCommandHandler(string commandName)
    {
        if (string.IsNullOrWhiteSpace(commandName))
            return false;

        lock (_commandHandlersLock)
        {
            if (CommandHandlers == null || CommandHandlers.Count == 0)
                return false;

            var removed = CommandHandlers.RemoveAll(command =>
                command.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase)
            );
            return removed > 0;
        }
    }

    public static void Load(string file)
    {
        if (!System.IO.File.Exists(file))
        {
            ServiceRuntime.Log?.Notify($"Cannot load file [{file}] (File does not exist)!");
            return;
        }

        Running = false;
        File = file;
        Commands = System.IO.File.ReadAllLines(file);

        Runtime?.FireEvent("OnLoadScript");
        Report(ScriptExecutionState.Loaded, 0, "<load>", $"Loaded script {file}");
    }

    public static void Load(string[] commands)
    {
        Commands = commands;
        Runtime?.FireEvent("OnLoadScript");
        Report(ScriptExecutionState.Loaded, 0, "<load>", "Loaded script commands.");
    }

    public static void Pause()
    {
        Paused = true;

        Runtime?.FireEvent("OnPauseScript");
        Report(ScriptExecutionState.Paused, CurrentLineIndex, "<pause>", "Script paused.");
    }

    public static void RunScript(bool useNearbyWaypoint = true, bool ignoreBotRunning = false)
    {
        RunScriptAsync(useNearbyWaypoint, ignoreBotRunning).GetAwaiter().GetResult();
    }

    public static async Task RunScriptAsync(bool useNearbyWaypoint = true, bool ignoreBotRunning = false)
    {
        if (Commands == null || Commands.Length == 0)
        {
            LogScriptMessage("No script loaded.", 0, ScriptValidationSeverity.Warning);
            return;
        }

        if (Running && !Paused)
            return;

        Running = true;
        Paused = false;

        var validationResult = DryRun(useNearbyWaypoint, logSimulation: false);
        if (!validationResult.IsValid)
        {
            LogValidationIssues(validationResult);
            Running = false;
            Runtime?.FireEvent("OnScriptLintFailed", validationResult);
            Report(ScriptExecutionState.Failed, CurrentLineIndex, "<lint>", "Script validation failed.");
            return;
        }

        CurrentLineIndex = validationResult.StartLineIndex;

        if (CurrentLineIndex != 0)
            ServiceRuntime.Log?.Debug($"[Script] Found nearby walk position at line #{CurrentLineIndex}");

        if (Commands == null || Commands.Length == 0 || Commands.Length <= CurrentLineIndex)
        {
            Running = false;
            return;
        }

        var error = false;
        for (var lineIndex = CurrentLineIndex; lineIndex < Commands.Length; lineIndex++)
        {
            if (!Running || Paused || (!IsBotRunning && !ignoreBotRunning))
            {
                error = true;
                break;
            }

            CurrentLineIndex = lineIndex;

            ServiceRuntime.Log?.Debug($"[Script] Executing line #{lineIndex}");
            ServiceRuntime.Log?.Notify("Running walk script");

            var scriptLine = Commands[lineIndex];
            var arguments = SplitArguments(scriptLine);
            var commandName = arguments.Length == 0 ? string.Empty : arguments[0];

            if (IsCommentOrEmpty(scriptLine))
            {
                CurrentLineIndex = lineIndex + 1;
                continue;
            }

            IScriptCommand handler;
            lock (_commandHandlersLock)
                handler = CommandHandlers.FirstOrDefault(h =>
                    h.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase)
                );
            if (handler == null)
            {
                LogScriptMessage("No script command handler found.", lineIndex, ScriptValidationSeverity.Warning, commandName);
                CurrentLineIndex = lineIndex + 1;
                continue;
            }

            if (handler.IsBusy && Running && !Paused)
            {
                error = true;
                LogScriptMessage(
                    "The script command is still busy, stopping script execution.",
                    lineIndex,
                    ScriptValidationSeverity.Warning,
                    commandName
                );
                break;
            }

            Report(ScriptExecutionState.ExecutingCommand, lineIndex, commandName, $"Executing script command {commandName}.");
            Runtime?.FireEvent("OnScriptStartExecuteCommand", handler, lineIndex);
            var executionResult = await handler.ExecuteAsync(arguments.Skip(1).ToArray()).ConfigureAwait(false);
            Runtime?.FireEvent("OnScriptFinishExecuteCommand", handler, executionResult, lineIndex);
            Report(ScriptExecutionState.CommandFinished, lineIndex, commandName, $"Finished script command {commandName}.");

            if (executionResult == false)
            {
                LogScriptMessage(
                    "The execution of the script command failed.",
                    lineIndex,
                    ScriptValidationSeverity.Warning,
                    commandName
                );
                CurrentLineIndex = lineIndex + 1;
                continue;
            }

            CurrentLineIndex = lineIndex + 1;
        }

        if (!Paused)
            Stop(error);
    }

    public static ScriptValidationResult LintScript(string[] commands = null)
    {
        var result = new ScriptValidationResult();
        var sourceCommands = commands ?? Commands;

        if (sourceCommands == null || sourceCommands.Length == 0)
        {
            result.Issues.Add(new ScriptValidationIssue(0, "<none>", "No script is loaded.", ScriptValidationSeverity.Warning));
            return result;
        }

        for (var lineIndex = 0; lineIndex < sourceCommands.Length; lineIndex++)
        {
            var scriptLine = sourceCommands[lineIndex];
            if (IsCommentOrEmpty(scriptLine))
                continue;

            var arguments = SplitArguments(scriptLine);
            if (arguments.Length == 0)
                continue;

            var commandName = arguments[0];
            var commandArguments = arguments.Skip(1).ToArray();
            IScriptCommand handler;
            lock (_commandHandlersLock)
                handler = CommandHandlers?.FirstOrDefault(h =>
                    h.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase)
                );

            if (handler == null)
            {
                result.Issues.Add(new ScriptValidationIssue(lineIndex + 1, commandName, "No script command handler found.", ScriptValidationSeverity.Error));
                continue;
            }

            var expectedArguments = handler.Arguments?.Count ?? 0;
            if (commandArguments.Length < expectedArguments)
            {
                result.Issues.Add(new ScriptValidationIssue(
                    lineIndex + 1,
                    commandName,
                    $"Missing arguments. Expected at least {expectedArguments}, got {commandArguments.Length}.",
                    ScriptValidationSeverity.Error
                ));
                continue;
            }

            if (commandName.Equals("move", StringComparison.OrdinalIgnoreCase) && !ValidateMoveArguments(commandArguments))
                result.Issues.Add(new ScriptValidationIssue(lineIndex + 1, commandName, "Invalid move argument format.", ScriptValidationSeverity.Error));

            if (commandName.Equals("wait", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(commandArguments[0], out var waitTime))
                    result.Issues.Add(new ScriptValidationIssue(lineIndex + 1, commandName, "Wait value must be a valid integer.", ScriptValidationSeverity.Error));
                else if (waitTime < 0)
                    result.Issues.Add(new ScriptValidationIssue(lineIndex + 1, commandName, "Wait value cannot be negative.", ScriptValidationSeverity.Error));
            }
        }

        return result;
    }

    public static ScriptValidationResult DryRun(bool useNearbyWaypoint = true, string[] commands = null, bool logSimulation = true)
    {
        var result = LintScript(commands);
        if (!result.IsValid)
            return result;

        var sourceCommands = commands ?? Commands;
        var startLineIndex = ResolveStartLineIndex(useNearbyWaypoint, commands != null);
        result.StartLineIndex = startLineIndex;

        if (sourceCommands == null || sourceCommands.Length == 0 || sourceCommands.Length <= startLineIndex)
            return result;

        for (var index = startLineIndex; index < sourceCommands.Length; index++)
        {
            var scriptLine = sourceCommands[index];
            if (IsCommentOrEmpty(scriptLine))
                continue;

            var arguments = SplitArguments(scriptLine);
            if (arguments.Length == 0)
                continue;

            var commandName = arguments[0];
            IScriptCommand handler;
            lock (_commandHandlersLock)
                handler = CommandHandlers?.FirstOrDefault(h =>
                    h.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase)
                );
            if (handler == null)
                continue;

            result.SimulatedCommands++;

            if (logSimulation)
                ServiceRuntime.Log?.Debug($"[Script:DryRun] line #{index}: {scriptLine}");
        }

        return result;
    }

    public static void Stop(bool error = false)
    {
        Running = false;
        Commands = null;
        File = null;

        IScriptCommand[] handlersSnapshot;
        lock (_commandHandlersLock)
            handlersSnapshot = CommandHandlers?.ToArray() ?? [];

        foreach (var handler in handlersSnapshot)
            handler.Stop();

        Runtime?.FireEvent("OnFinishScript", error);
        Report(error ? ScriptExecutionState.Failed : ScriptExecutionState.Completed, CurrentLineIndex, "<stop>", error ? "Script stopped with errors." : "Script completed.");
    }

    public static List<Position> GetWalkScript()
    {
        if (Commands == null || Commands.Length == 0)
            return [];

        var walkCommands = Commands.Where(c => c.Trim().StartsWith("move"));

        return walkCommands
            .Select(command => SplitArguments(command).Skip(1).ToArray())
            .Select(ParsePosition)
            .ToList();
    }

    internal static string[] SplitArguments(string scriptLine)
    {
        return scriptLine?.Split(new[] { ArgumentSeparator }, StringSplitOptions.RemoveEmptyEntries) ?? [];
    }

    internal static Position ParsePosition(string[] args)
    {
        if (args == null || args.Length < 5)
            return default;

        if (
            !float.TryParse(args[0], out var xOffset)
            || !float.TryParse(args[1], out var yOffset)
            || !float.TryParse(args[2], out var zOffset)
            || !byte.TryParse(args[3], out var xSector)
            || !byte.TryParse(args[4], out var ySector)
        )
            return default;

        return new Position(xSector, ySector, xOffset, yOffset, zOffset);
    }

    private static bool ValidateMoveArguments(string[] args)
    {
        return args != null
            && args.Length >= 5
            && float.TryParse(args[0], out _)
            && float.TryParse(args[1], out _)
            && float.TryParse(args[2], out _)
            && byte.TryParse(args[3], out _)
            && byte.TryParse(args[4], out _);
    }

    private static void LogScriptMessage(string message, int line, ScriptValidationSeverity severity = ScriptValidationSeverity.Warning, string command = null)
    {
        command ??= "<none>";
        var formatted = $"[Script] {message} (command={command}; line={line})";
        if (severity == ScriptValidationSeverity.Error || severity == ScriptValidationSeverity.Warning)
            ServiceRuntime.Log?.Warn(formatted);
        else
            ServiceRuntime.Log?.Debug(formatted);
    }

    private static void LogValidationIssues(ScriptValidationResult validationResult)
    {
        foreach (var issue in validationResult.Issues)
            LogScriptMessage(issue.Message, issue.LineNumber, issue.Severity, issue.Command);
    }

    private static bool IsCommentOrEmpty(string scriptLine)
    {
        if (string.IsNullOrWhiteSpace(scriptLine))
            return true;

        var trimmedCommand = scriptLine.Trim();
        return trimmedCommand.StartsWith("//") || trimmedCommand.StartsWith("#");
    }

    private static int ResolveStartLineIndex(bool useNearbyWaypoint, bool overrideCommandsProvided)
    {
        if (!useNearbyWaypoint || overrideCommandsProvided)
            return 0;

        if (Commands == null || Commands.Length == 0 || Runtime == null || !Runtime.GameReady || Runtime.PlayerMovementSource == null)
            return 0;

        try
        {
            return FindNearestMoveCommandLine();
        }
        catch (Exception ex)
        {
            ServiceRuntime.Log?.Debug($"[Script] Failed to find nearest move command: {ex.Message}");
            return 0;
        }
    }

    private static int FindNearestMoveCommandLine()
    {
        var playerPos = Runtime.PlayerMovementSource;
        var line = -1;
        var moveCommands = new Dictionary<int, Position>();

        foreach (var command in Commands)
        {
            line++;

            var trimmedCommand = command.Trim();
            if (IsCommentOrEmpty(trimmedCommand))
                continue;

            var splitArguments = SplitArguments(trimmedCommand);
            if (splitArguments.Length == 0 || !splitArguments[0].Equals("move", StringComparison.OrdinalIgnoreCase))
                continue;

            var args = splitArguments.Skip(1).ToArray();
            var curPos = ParsePosition(args);
            var distance = Runtime.DistanceToPlayer(curPos);

            if (distance < 100 && !Runtime.HasCollisionBetween(playerPos, curPos))
                moveCommands.Add(line, curPos);
        }

        return moveCommands.Count == 0 ? 0 : moveCommands.MinBy(c => Runtime.DistanceToPlayer(c.Value)).Key;
    }

    internal static void Report(ScriptExecutionState state, int lineIndex, string command, string message, object position = null)
    {
        ServiceRuntime.ScriptProgress?.Report(new ScriptProgressUpdate(state, lineIndex, command, message, position));
    }

    private static bool IsBotRunning => Runtime?.IsBotRunning == true;

    internal static IScriptRuntime Runtime => ServiceRuntime.ScriptRuntime;
}
