using System;
using System.Threading.Tasks;
using UBot.Core.Abstractions.Services;
using UBot.Core.ProtocolServices;
using UBot.Core.Services;

namespace UBot.Core.Components;

public static class ClientManager
{
    private static ClientManagerRuntime _runtime;

    public static bool IsRunning => Runtime.IsRunning;

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
        if (ServiceRuntime.ClientNativeRuntime == null)
            ServiceRuntime.ClientNativeRuntime = new ClientNativeRuntimeAdapter();

        if (ServiceRuntime.ClientLaunchConfigProvider == null)
            ServiceRuntime.ClientLaunchConfigProvider = new CoreClientLaunchConfigProvider();

        ServiceRuntime.ClientLaunchPolicy = launchPolicy ?? throw new ArgumentNullException(nameof(launchPolicy));
        Initialize(new ClientManagerRuntime(
            ServiceRuntime.ClientLaunchPolicy,
            ServiceRuntime.ClientNativeRuntime,
            ServiceRuntime.ClientLaunchConfigProvider));
    }

    public static void Initialize(ClientManagerRuntime runtime)
    {
        _runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
    }

    public static Task<bool> Start()
    {
        return Runtime.StartAsync();
    }

    public static void Kill()
    {
        Runtime.Kill();
    }

    public static void SetTitle(string title)
    {
        Runtime.SetTitle(title);
    }

    public static void SetVisible(bool visible)
    {
        Runtime.SetVisible(visible);
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

    private static ClientManagerRuntime Runtime
    {
        get
        {
            EnsureInitialized();
            return _runtime;
        }
    }
}

public sealed class ClientManagerRuntime : IDisposable
{
    private readonly IClientLaunchPolicy _launchPolicy;
    private readonly IClientNativeRuntime _nativeRuntime;
    private readonly IClientLaunchConfigProvider _configProvider;

    public ClientManagerRuntime(
        IClientLaunchPolicy launchPolicy,
        IClientNativeRuntime nativeRuntime,
        IClientLaunchConfigProvider configProvider)
    {
        _launchPolicy = launchPolicy ?? throw new ArgumentNullException(nameof(launchPolicy));
        _nativeRuntime = nativeRuntime ?? throw new ArgumentNullException(nameof(nativeRuntime));
        _configProvider = configProvider ?? throw new ArgumentNullException(nameof(configProvider));
    }

    public bool IsRunning => _nativeRuntime.IsRunning;

    public Task<bool> StartAsync() => _launchPolicy.StartAsync();

    public void Kill() => _nativeRuntime.Kill();

    public void SetTitle(string title) => _nativeRuntime.SetTitle(title);

    public void SetVisible(bool visible) => _nativeRuntime.SetVisible(visible);

    public void Dispose()
    {
        _nativeRuntime.Kill();
    }
}
