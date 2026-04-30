using UBot.Core.Abstractions.Services;
using UBot.Core.Event;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreScriptEventBus : IScriptEventBus
{
    private readonly ScriptEventBus _eventBus = new();

    public void SubscribeEvent(string eventName, System.Delegate handler) =>
        _eventBus.SubscribeEvent(eventName, handler);

    public void SubscribeEvent(string eventName, System.Delegate handler, object owner) =>
        _eventBus.SubscribeEvent(eventName, handler, owner);

    public void UnsubscribeEvent(string eventName, System.Delegate handler) =>
        _eventBus.UnsubscribeEvent(eventName, handler);

    public void UnsubscribeOwner(object owner) => _eventBus.UnsubscribeOwner(owner);

    public void RaiseEvent(string eventName, params object[] args) => _eventBus.RaiseEvent(eventName, args);

    public void Fire(string eventName, params object[] args)
    {
        _eventBus.Fire(eventName, args);
    }

    public void ClearSubscribers() => _eventBus.ClearSubscribers();
}
