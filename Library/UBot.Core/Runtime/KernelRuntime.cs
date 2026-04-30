using System.Threading.Tasks;
using UBot.Core.Abstractions;
using UBot.Core.Network;
using UBot.Core.Plugins;
using KState = UBot.Core.Kernel;

namespace UBot.Core.Runtime;

public sealed class KernelRuntime : IKernelRuntime
{
    public object Proxy
    {
        get => KState.Proxy;
        set => KState.Proxy = value as Proxy;
    }

    public object Bot
    {
        get => KState.Bot;
        set => KState.Bot = value as Bot;
    }

    public string Language { get => KState.Language; set => KState.Language = value; }
    public string LaunchMode { get => KState.LaunchMode; set => KState.LaunchMode = value; }
    public int TickCount => KState.TickCount;
    public string BasePath => KState.BasePath;
    public bool EnableCollisionDetection { get => KState.EnableCollisionDetection; set => KState.EnableCollisionDetection = value; }
    public bool Debug { get => KState.Debug; set => KState.Debug = value; }

    public void Initialize() => KState.Initialize();

    public Task StartAsync()
    {
        KState.Initialize();
        return Task.CompletedTask;
    }

    public void Shutdown() => KState.Shutdown();
}
