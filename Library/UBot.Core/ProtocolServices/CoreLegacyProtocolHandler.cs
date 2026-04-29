using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UBot.Core.Abstractions.Services;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreLegacyProtocolHandler : IProtocolLegacyHandler
{
    private readonly Dictionary<string, Type> _types;
    private readonly Dictionary<string, object> _instances = new();

    public CoreLegacyProtocolHandler()
    {
        _types = typeof(CoreLegacyProtocolHandler)
            .Assembly.GetTypes()
            .Where(type => type.Namespace != null && type.Namespace.StartsWith("UBot.Core.ProtocolLegacy"))
            .GroupBy(type => type.Name)
            .ToDictionary(group => group.Key, group => group.First());
    }

    public void Invoke(string handlerName, object packet)
    {
        var instance = GetInstance(handlerName);
        instance?.GetType().GetMethod("Invoke")?.Invoke(instance, new[] { packet });
    }

    public object ReplacePacket(string hookName, object packet)
    {
        var instance = GetInstance(hookName);
        if (instance == null)
            return packet;

        return instance.GetType().GetMethod("ReplacePacket")?.Invoke(instance, new[] { packet });
    }

    private object GetInstance(string name)
    {
        if (_instances.TryGetValue(name, out var instance))
            return instance;

        if (!_types.TryGetValue(name, out var type))
            return null;

        instance = Activator.CreateInstance(type);
        _instances[name] = instance;
        return instance;
    }
}

