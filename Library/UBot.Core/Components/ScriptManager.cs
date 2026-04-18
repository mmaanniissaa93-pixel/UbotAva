using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UBot.Core.Components.Scripting;
using UBot.Core.Event;
using UBot.Core.Objects;

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

    /// <summary>
    ///     Gets the initial directory
    /// </summary>
    public static string InitialDirectory => Path.Combine(Kernel.BasePath, "Data", "Scripts");

    /// <summary>
    ///     Gets or sets the file.
    /// </summary>
    /// <value>
    ///     The file.
    /// </value>
    public static string File { get; set; }

    /// <summary>
    ///     Gets or sets the commands.
    /// </summary>
    /// <value>
    ///     The commands.
    /// </value>
    public static string[] Commands { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this <see cref="ScriptManager" /> is running.
    /// </summary>
    /// <value>
    ///     <c>true</c> if running; otherwise, <c>false</c>.
    /// </value>
    public static bool Running { get; set; }

    /// <summary>
    ///     Gets the command handlers.
    /// </summary>
    /// <value>The command handlers.</value>
    public static List<IScriptCommand> CommandHandlers { get; private set; }

    /// <summary>
    ///     Gets the index of the current line.
    /// </summary>
    /// <value>
    ///     The index of the current line.
    /// </value>
    public static int CurrentLineIndex { get; private set; }

    /// <summary>
    ///     Gets or sets the argument separator.
    ///     Modify this value in case of custom script syntax support
    /// </summary>
    /// <value>
    ///     The argument separator.
    /// </value>
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
            .Where(p => type.IsAssignableFrom(p) && !p.IsInterface)
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

    /// <summary>
    ///     Loads the specified file.
    /// </summary>
    /// <param name="file">The file.</param>
    public static void Load(string file)
    {
        if (!System.IO.File.Exists(file))
        {
            Log.Notify($"Cannot load file [{file}] (File does not exist)!");
            return;
        }

        Running = false;
        File = file;
        Commands = System.IO.File.ReadAllLines(file);

        EventManager.FireEvent("OnLoadScript");
    }

    /// <summary>
    ///     Loads the specified commands.
    /// </summary>
    /// <param name="commands">The commands.</param>
    public static void Load(string[] commands)
    {
        Commands = commands;
        EventManager.FireEvent("OnLoadScript");
    }

    /// <summary>
    ///     Pauses the command execution.
    /// </summary>
    public static void Pause()
    {
        Paused = true;

        EventManager.FireEvent("OnPauseScript");
    }

    /// <summary>
    ///     Runs this instance.
    /// </summary>
    public static void RunScript(bool useNearbyWaypoint = true, bool ignoreBotRunning = false)
    {
        if (Commands == null || Commands.Length == 0)
        {
            LogScriptMessage("No script loaded.", 0, LogLevel.Warning);

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
            EventManager.FireEvent("OnScriptLintFailed", validationResult);
            return;
        }

        CurrentLineIndex = validationResult.StartLineIndex;

        if (CurrentLineIndex != 0)
            Log.Debug($"[Script] Found nearby walk position at line #{CurrentLineIndex}");

        if (Commands == null || Commands.Length == 0 || Commands.Length <= CurrentLineIndex)
        {
            Running = false;
            return;
        }

        var error = false;
        for (var lineIndex = CurrentLineIndex; lineIndex < Commands.Length; lineIndex++)
        {
            if (!Running || Paused || (!Kernel.Bot.Running && !ignoreBotRunning))
            {
                error = true;

                break;
            }

            CurrentLineIndex = lineIndex;

            Log.Debug($"[Script] Executing line #{lineIndex}");
            Log.Status("Running walk script");

            var scriptLine = Commands[lineIndex];
            var arguments = SplitArguments(scriptLine);
            var commandName = arguments.Length == 0 ? string.Empty : arguments[0];

            if (
                string.IsNullOrEmpty(commandName)
                || commandName.Trim().StartsWith("//")
                || commandName.Trim().StartsWith("#")
            )
            {
                CurrentLineIndex = lineIndex + 1;
                continue; //No command name given / empty line
            }

            IScriptCommand handler;
            lock (_commandHandlersLock)
                handler = CommandHandlers.FirstOrDefault(h =>
                    h.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase)
                );
            if (handler == null)
            {
                LogScriptMessage("No script command handler found.", lineIndex, LogLevel.Warning);
                CurrentLineIndex = lineIndex + 1;

                continue; //No matching handler found for this command
            }

            if (handler.IsBusy && Running && !Paused)
            {
                error = true;
                LogScriptMessage(
                    "The script command is still busy, stopping script execution.",
                    lineIndex,
                    LogLevel.Debug,
                    commandName
                );

                break;
            }

            EventManager.FireEvent("OnScriptStartExecuteCommand", handler, lineIndex);
            var executionResult = handler.Execute(arguments.Skip(1).ToArray());
            EventManager.FireEvent("OnScriptFinishExecuteCommand", handler, executionResult, lineIndex);

            if (executionResult == false)
            {
                LogScriptMessage(
                    "The execution of the script command failed.",
                    lineIndex,
                    LogLevel.Warning,
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
            result.Issues.Add(
                new ScriptValidationIssue(
                    0,
                    "<none>",
                    "No script is loaded.",
                    ScriptValidationSeverity.Warning
                )
            );
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
                result.Issues.Add(
                    new ScriptValidationIssue(
                        lineIndex + 1,
                        commandName,
                        "No script command handler found.",
                        ScriptValidationSeverity.Error
                    )
                );
                continue;
            }

            var expectedArguments = handler.Arguments?.Count ?? 0;
            if (commandArguments.Length < expectedArguments)
            {
                result.Issues.Add(
                    new ScriptValidationIssue(
                        lineIndex + 1,
                        commandName,
                        $"Missing arguments. Expected at least {expectedArguments}, got {commandArguments.Length}.",
                        ScriptValidationSeverity.Error
                    )
                );
                continue;
            }

            if (
                commandName.Equals("move", StringComparison.OrdinalIgnoreCase)
                && !ValidateMoveArguments(commandArguments)
            )
            {
                result.Issues.Add(
                    new ScriptValidationIssue(
                        lineIndex + 1,
                        commandName,
                        "Invalid move argument format.",
                        ScriptValidationSeverity.Error
                    )
                );
            }

            if (commandName.Equals("wait", StringComparison.OrdinalIgnoreCase))
            {
                if (!int.TryParse(commandArguments[0], out var waitTime))
                {
                    result.Issues.Add(
                        new ScriptValidationIssue(
                            lineIndex + 1,
                            commandName,
                            "Wait value must be a valid integer.",
                            ScriptValidationSeverity.Error
                        )
                    );
                }
                else if (waitTime < 0)
                {
                    result.Issues.Add(
                        new ScriptValidationIssue(
                            lineIndex + 1,
                            commandName,
                            "Wait value cannot be negative.",
                            ScriptValidationSeverity.Error
                        )
                    );
                }
            }
        }

        return result;
    }

    public static ScriptValidationResult DryRun(
        bool useNearbyWaypoint = true,
        string[] commands = null,
        bool logSimulation = true
    )
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
                Log.Debug($"[Script:DryRun] line #{index}: {scriptLine}");
        }

        return result;
    }

    /// <summary>
    ///     Stops this instance.
    /// </summary>
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

        EventManager.FireEvent("OnFinishScript", error);
    }

    /// <summary>
    ///     A convenience function that returns all positions in the walk script.
    ///     Warning: This method is not extendable at the moment, that means that there can not be
    ///     a custom implementation of the "move" command. The move command currently always needs to have the arguments
    ///     XOffset, YOffset, ZOffset, XSector, YSector.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    ///     Parses the position from the given arguments.
    /// </summary>
    /// <param name="args">The arguments.</param>
    /// <returns></returns>
    private static Position ParsePosition(string[] args)
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
            return default; //Invalid format

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

    /// <summary>
    ///     Logs the script message.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="line">The line.</param>
    /// <param name="level">The level.</param>
    /// <param name="command">The command.</param>
    private static void LogScriptMessage(
        string message,
        int line,
        LogLevel level = LogLevel.Notify,
        string command = null
    )
    {
        if (command == null)
            command = "<none>";

        Log.Append(level, $"[Script] {message} (command={command}; line={line})");
    }

    private static void LogValidationIssues(ScriptValidationResult validationResult)
    {
        foreach (var issue in validationResult.Issues)
        {
            var level = issue.Severity == ScriptValidationSeverity.Error
                ? LogLevel.Warning
                : LogLevel.Debug;
            LogScriptMessage(issue.Message, issue.LineNumber, level, issue.Command);
        }
    }

    private static bool IsCommentOrEmpty(string scriptLine)
    {
        if (string.IsNullOrWhiteSpace(scriptLine))
            return true;

        var trimmedCommand = scriptLine.Trim();
        return trimmedCommand.StartsWith("//") || trimmedCommand.StartsWith("#");
    }

    private static string[] SplitArguments(string scriptLine)
    {
        return scriptLine?.Split(
            new[] { ArgumentSeparator },
            StringSplitOptions.RemoveEmptyEntries
        ) ?? [];
    }

    private static int ResolveStartLineIndex(bool useNearbyWaypoint, bool overrideCommandsProvided)
    {
        if (!useNearbyWaypoint || overrideCommandsProvided)
            return 0;

        if (Commands == null || Commands.Length == 0 || !Game.Ready || Game.Player == null)
            return 0;

        try
        {
            return FindNearestMoveCommandLine();
        }
        catch (Exception ex)
        {
            Log.Debug($"[Script] Failed to find nearest move command: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    ///     Finds the nearest walk command line.
    /// </summary>
    /// <returns></returns>
    private static int FindNearestMoveCommandLine()
    {
        var playerPos = Game.Player.Movement.Source;

        var line = -1;
        var moveCommands = new Dictionary<int, Position>();

        foreach (var command in Commands)
        {
            line++;

            var trimmedCommand = command.Trim();
            if (
                trimmedCommand.StartsWith("//")
                || trimmedCommand.StartsWith("#")
                || string.IsNullOrWhiteSpace(trimmedCommand)
            )
                continue;

            var splitArguments = SplitArguments(trimmedCommand);
            if (splitArguments.Length == 0 || splitArguments[0] != "move")
                continue;

            var args = splitArguments.Skip(1).ToArray();
            var curPos = ParsePosition(args);
            var distance = curPos.DistanceToPlayer();

            if (distance < 100 && !playerPos.HasCollisionBetween(curPos))
                moveCommands.Add(line, curPos);
        }

        return moveCommands.Count == 0 ? 0 : moveCommands.MinBy(c => c.Value.DistanceToPlayer()).Key;
    }
}
