using System;
using System.Collections.Generic;
using System.Linq;
using UBot.Core.Abstractions.Services;
using UBot.Core.Components.Command;
using UBot.Core.Services;

namespace UBot.Core.Components;

public static class CommandManager
{
    private static List<ICommandExecutor> _commands;

    /// <summary>
    ///     Initializes the command manager.
    /// </summary>
    public static void Initialize()
    {
        _commands = new List<ICommandExecutor>(16);

        var type = typeof(ICommandExecutor);
        var types = AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(s => s.GetTypes())
            .Where(p => type.IsAssignableFrom(p) && !p.IsInterface)
            .ToArray();

        foreach (var handler in types)
        {
            var instance = (ICommandExecutor)Activator.CreateInstance(handler);

            _commands.Add(instance);
        }

        ServiceRuntime.Log?.Debug($"[CommandManager] Found and registered {_commands.Count} commands");
    }

    /// <summary>
    ///     Executes the given command
    /// </summary>
    public static bool Execute(string command, bool silent = false)
    {
        if (string.IsNullOrEmpty(command) || command == "none")
            return true;

        ServiceRuntime.Log?.Notify($"[CommandManager] Executing command {command}");

        var executor = GetExecutor(command);

        return executor != null && executor.Execute(silent);
    }

    /// <summary>
    ///     Gets a command executor instance by a command name
    /// </summary>
    public static ICommandExecutor GetExecutor(string commandName)
    {
        return _commands.FirstOrDefault(c => c.CommandName == commandName);
    }

    /// <summary>
    ///     Returns a list of all available command descriptions.
    ///     Key = Command name
    ///     Value = Command description
    /// </summary>
    public static Dictionary<string, string> GetCommandDescriptions()
    {
        var result = new Dictionary<string, string>(16) { { "none", "No action" } };

        foreach (var command in _commands)
            result.Add(command.CommandName, command.CommandDescription);

        return result;
    }
}

public sealed class CommandService : ICommandService
{
    public void Initialize() => CommandManager.Initialize();

    public bool Execute(string command, bool silent = false) => CommandManager.Execute(command, silent);

    public object GetExecutor(string commandName) => CommandManager.GetExecutor(commandName);

    public Dictionary<string, string> GetCommandDescriptions() => CommandManager.GetCommandDescriptions();
}
