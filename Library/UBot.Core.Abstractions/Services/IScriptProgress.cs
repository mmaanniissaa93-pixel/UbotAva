namespace UBot.Core.Abstractions.Services;

public interface IScriptProgress
{
    void Report(ScriptProgressUpdate update);
}
