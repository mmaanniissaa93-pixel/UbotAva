using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UBot.Core.Network;

namespace UBot.Core.Network;

public static class NetworkHandlerRegistry
{
    public static void RegisterAll()
    {
        RegisterHandlers();
        RegisterHooks();
    }

    private static void RegisterHandlers()
    {
        var type = typeof(IPacketHandler);
        var types = AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(SafeGetTypes)
            .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract)
            .ToArray();

        foreach (var handler in types)
        {
            var instance = (IPacketHandler)Activator.CreateInstance(handler);

            PacketManager.RegisterHandler(instance);
        }
    }

    private static void RegisterHooks()
    {
        var type = typeof(IPacketHook);
        var types = AppDomain
            .CurrentDomain.GetAssemblies()
            .SelectMany(SafeGetTypes)
            .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract)
            .ToArray();

        foreach (var hook in types)
        {
            var instance = (IPacketHook)Activator.CreateInstance(hook);

            PacketManager.RegisterHook(instance);
        }
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.Where(t => t != null)!;
        }
        catch
        {
            return Enumerable.Empty<Type>();
        }
    }
}