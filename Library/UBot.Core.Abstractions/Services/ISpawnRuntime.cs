namespace UBot.Core.Abstractions.Services;

public interface ISpawnRuntime
{
    object Player { get; }
    object SelectedEntity { get; set; }

    object GetRefObjCommon(uint id);
    SpawnParseResult ParseSpawn(object packet, bool isGroup);
    void FireEvent(string eventName, params object[] args);
    void LogDebug(string message);
}
