using System.Collections.Generic;

namespace UBot.Core.Abstractions.Services;

public interface IScriptService
{
    string InitialDirectory { get; }
    string File { get; set; }
    string[] Commands { get; set; }
    bool Running { get; set; }
    int CurrentLineIndex { get; }
    char ArgumentSeparator { get; set; }
    bool Paused { get; }

    void Initialize();
    bool RegisterCommandHandler(object handler, bool replaceExisting = true);
    bool UnregisterCommandHandler(string commandName);
    void Load(string file);
    void Load(string[] commands);
    void Pause();
    void RunScript(bool useNearbyWaypoint = true, bool ignoreBotRunning = false);
    object LintScript(string[] commands = null);
    object DryRun(string[] commands, IReadOnlyDictionary<string, object> variables = null);
    void Stop(bool error = false);
    List<object> GetWalkScript();
}
