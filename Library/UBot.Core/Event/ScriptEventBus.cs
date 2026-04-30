using System;
using UBot.Core.Abstractions.Services;
using EMgr = UBot.Core.Event.EventManager;

namespace UBot.Core.Event;

public sealed class ScriptEventBus : IScriptEventBus, IDisposable
{
    public void SubscribeEvent(string eventName, Delegate handler) =>
        EMgr.SubscribeEvent(eventName, handler);

    public void SubscribeEvent(string eventName, Delegate handler, object owner) =>
        EMgr.SubscribeEvent(eventName, handler, owner);

    public void UnsubscribeEvent(string eventName, Delegate handler) =>
        EMgr.UnsubscribeEvent(eventName, handler);

    public void UnsubscribeOwner(object owner) => EMgr.UnsubscribeOwner(owner);

    public void RaiseEvent(string eventName, params object[] args) =>
        EMgr.FireEvent(eventName, args);

    public void Fire(string eventName, params object[] args) => RaiseEvent(eventName, args);

    public void ClearSubscribers() => EMgr.ClearSubscribers();

    public void Dispose() => ClearSubscribers();
}
