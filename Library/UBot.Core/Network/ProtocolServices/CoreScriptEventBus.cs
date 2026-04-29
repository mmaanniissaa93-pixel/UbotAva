using UBot.Core.Abstractions.Services;
using UBot.Core.Event;

namespace UBot.Core.Network.ProtocolServices;

internal sealed class CoreScriptEventBus : IScriptEventBus
{
    public void Fire(string eventName, params object[] args)
    {
        EventManager.FireEvent(eventName, args);
    }
}
