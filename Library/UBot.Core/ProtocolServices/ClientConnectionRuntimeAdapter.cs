using UBot.Core.Abstractions.Services;
using UBot.Core.Network;

namespace UBot.Core.ProtocolServices;

internal sealed class ClientConnectionRuntimeAdapter : IClientConnectionRuntime
{
    public bool IsClientless
    {
        get => Game.Clientless;
        set => Game.Clientless = value;
    }

    public bool IsConnectedToGatewayServer => Kernel.Proxy?.IsConnectedToGatewayserver == true;

    public bool IsConnectedToAgentServer => Kernel.Proxy?.IsConnectedToAgentserver == true;

    public int GetReloginDelayMilliseconds()
    {
        if (GlobalConfig.Get("UBot.General.EnableWaitAfterDC", false))
            return GlobalConfig.Get<int>("UBot.General.WaitAfterDC") * 60 * 1000;

        return 10000;
    }

    public void ShutdownClientConnection()
    {
        Kernel.Proxy?.Client?.Shutdown();
    }

    public void StartGame()
    {
        Game.Start();
    }

    public void SendServerListRequest()
    {
        PacketManager.SendPacket(new Packet(0x6101, true), PacketDestination.Server);
    }

    public void SendKeepAlive()
    {
        PacketManager.SendPacket(new Packet(0x2002), PacketDestination.Server);
    }
}
