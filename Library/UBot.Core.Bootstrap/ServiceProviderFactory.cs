using System;
using Microsoft.Extensions.DependencyInjection;
using UBot.Core.Runtime;

namespace UBot.Core.Bootstrap;

public static class ServiceProviderFactory
{
    private static ServiceProvider _provider;

    public static IServiceProvider CreateServices()
    {
        if (_provider != null)
            return _provider;

        var services = new ServiceCollection();
        services.AddGameRuntime();
        _provider = services.BuildServiceProvider();
        CoreRuntimeBootstrapper.Initialize(_provider);
        return _provider;
    }

    public static void Dispose()
    {
        _provider?.Dispose();
        _provider = null;
    }
}
