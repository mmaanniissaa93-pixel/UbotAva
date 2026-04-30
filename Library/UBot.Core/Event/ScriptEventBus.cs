using System;
using UBot.Core.Abstractions.Services;

namespace UBot.Core.Event;

public sealed class ScriptEventBus : IScriptEventBus, IDisposable
{
    public void SubscribeEvent(string eventName, Delegate handler) =>
        EventManager.SubscribeEvent(eventName, handler);

    public void SubscribeEvent(string eventName, Delegate handler, object owner) =>
        EventManager.SubscribeEvent(eventName, handler, owner);

    public void UnsubscribeEvent(string eventName, Delegate handler) =>
        EventManager.UnsubscribeEvent(eventName, handler);

    public void UnsubscribeOwner(object owner) => EventManager.UnsubscribeOwner(owner);

    public void RaiseEvent(string eventName, params object[] args) =>
        EventManager.FireEvent(eventName, args);

    public void Fire(string eventName, params object[] args) => RaiseEvent(eventName, args);

    public void ClearSubscribers() => EventManager.ClearSubscribers();

    public void Dispose() => ClearSubscribers();
}
