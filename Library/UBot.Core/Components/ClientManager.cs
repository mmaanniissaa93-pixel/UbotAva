using System;
using System.Threading.Tasks;
using UBot.Core.Abstractions.Services;
using UBot.Core.ProtocolServices;
using UBot.Core.Services;

namespace UBot.Core.Components;

public static class ClientManager
{
    public static bool IsRunning => NativeRuntime?.IsRunning == true;

    public static void Initialize()
    {
        if (ServiceRuntime.ClientNativeRuntime == null)
            ServiceRuntime.ClientNativeRuntime = new ClientNativeRuntimeAdapter();

        if (ServiceRuntime.ClientLaunchConfigProvider == null)
            ServiceRuntime.ClientLaunchConfigProvider = new CoreClientLaunchConfigProvider();

        Initialize(ServiceRuntime.ClientLaunchPolicy ?? new ClientLaunchPolicyService());
    }

    public static void Initialize(IClientLaunchPolicy launchPolicy)
    {
        ServiceRuntime.ClientLaunchPolicy = launchPolicy ?? throw new ArgumentNullException(nameof(launchPolicy));
    }

    public static Task<bool> Start()
    {
        EnsureInitialized();
        return ServiceRuntime.ClientLaunchPolicy.StartAsync();
    }

    public static void Kill()
    {
        EnsureInitialized();
        NativeRuntime?.Kill();
    }

    public static void SetTitle(string title)
    {
        EnsureInitialized();
        NativeRuntime?.SetTitle(title);
    }

    public static void SetVisible(bool visible)
    {
        EnsureInitialized();
        NativeRuntime?.SetVisible(visible);
    }

    private static void EnsureInitialized()
    {
        if (ServiceRuntime.ClientLaunchPolicy == null
            || ServiceRuntime.ClientNativeRuntime == null
            || ServiceRuntime.ClientLaunchConfigProvider == null)
        {
            Initialize();
        }
    }

    private static IClientNativeRuntime NativeRuntime => ServiceRuntime.ClientNativeRuntime;
}
