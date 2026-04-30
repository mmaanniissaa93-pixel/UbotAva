using System;

namespace UBot.Core.Abstractions.Services;

public interface IScriptEventBus
{
    void SubscribeEvent(string eventName, Delegate handler);
    void SubscribeEvent(string eventName, Delegate handler, object owner);
    void UnsubscribeEvent(string eventName, Delegate handler);
    void UnsubscribeOwner(object owner);
    void RaiseEvent(string eventName, params object[] args);
    void Fire(string eventName, params object[] args);
    void ClearSubscribers();
}
