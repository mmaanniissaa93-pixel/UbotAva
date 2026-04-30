using System.Threading.Tasks;
using UBot.Core.Abstractions;
using UBot.Core.Network;
using UBot.Core.Plugins;

namespace UBot.Core.Runtime;

public sealed class KernelRuntime : IKernelRuntime
{
    public object Proxy
    {
        get => Kernel.Proxy;
        set => Kernel.Proxy = value as Proxy;
    }

    public object Bot
    {
        get => Kernel.Bot;
        set => Kernel.Bot = value as Bot;
    }

    public string Language { get => Kernel.Language; set => Kernel.Language = value; }
    public string LaunchMode { get => Kernel.LaunchMode; set => Kernel.LaunchMode = value; }
    public int TickCount => Kernel.TickCount;
    public string BasePath => Kernel.BasePath;
    public bool EnableCollisionDetection { get => Kernel.EnableCollisionDetection; set => Kernel.EnableCollisionDetection = value; }
    public bool Debug { get => Kernel.Debug; set => Kernel.Debug = value; }

    public void Initialize() => Kernel.Initialize();

    public Task StartAsync()
    {
        Kernel.Initialize();
        return Task.CompletedTask;
    }

    public void Shutdown() => Kernel.Shutdown();
}
