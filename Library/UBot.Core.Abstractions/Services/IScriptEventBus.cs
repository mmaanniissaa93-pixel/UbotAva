namespace UBot.Core.Abstractions.Services;

public interface IScriptEventBus
{
    void Fire(string eventName, params object[] args);
}
