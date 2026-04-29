namespace UBot.Core.Abstractions.Services;

public sealed class SpawnParseResult
{
    public SpawnParseResult(object entity, string eventName)
    {
        Entity = entity;
        EventName = eventName;
    }

    public object Entity { get; }
    public string EventName { get; }
}
