using System.Collections.Generic;

namespace UBot.Core.Abstractions.Services;

public interface ICommandService
{
    void Initialize();
    bool Execute(string command, bool silent = false);
    object GetExecutor(string commandName);
    Dictionary<string, string> GetCommandDescriptions();
}
