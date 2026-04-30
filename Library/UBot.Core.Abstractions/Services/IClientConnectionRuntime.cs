namespace UBot.Core.Abstractions.Services;

public interface IClientConnectionRuntime
{
    bool IsClientless { get; set; }
    bool IsConnectedToGatewayServer { get; }
    bool IsConnectedToAgentServer { get; }

    int GetReloginDelayMilliseconds();
    void ShutdownClientConnection();
    void StartGame();
    void SendServerListRequest();
    void SendKeepAlive();
}
