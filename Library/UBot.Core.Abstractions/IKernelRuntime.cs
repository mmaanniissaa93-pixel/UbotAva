namespace UBot.Core.Abstractions;

public interface IKernelRuntime
{
    object Proxy { get; set; }
    object Bot { get; set; }
    string Language { get; set; }
    string LaunchMode { get; set; }
    int TickCount { get; }
    string BasePath { get; }
    bool EnableCollisionDetection { get; set; }
    bool Debug { get; set; }

    void Initialize();
    void Shutdown();
}