namespace UBot.Core.Abstractions.Services;

public interface IClientlessService
{
    void Initialize();
    void GoClientless();
    void Shutdown();
    void RequestServerList();
    void OnAgentServerDisconnected();
    void OnAgentServerConnected();
}
