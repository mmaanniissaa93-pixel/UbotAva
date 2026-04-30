using UBot.Core.Abstractions.Services;
using UBot.Core.Network;

namespace UBot.Core.ProtocolServices;

internal sealed class ClientConnectionRuntimeAdapter : IClientConnectionRuntime
{
    public bool IsClientless
    {
        get => UBot.Core.RuntimeAccess.Session.Clientless;
        set => UBot.Core.RuntimeAccess.Session.Clientless = value;
    }

    public bool IsConnectedToGatewayServer => UBot.Core.RuntimeAccess.Core.Proxy?.IsConnectedToGatewayserver == true;

    public bool IsConnectedToAgentServer => UBot.Core.RuntimeAccess.Core.Proxy?.IsConnectedToAgentserver == true;

    public int GetReloginDelayMilliseconds()
    {
        if (UBot.Core.RuntimeAccess.Global.Get("UBot.General.EnableWaitAfterDC", false))
            return UBot.Core.RuntimeAccess.Global.Get<int>("UBot.General.WaitAfterDC") * 60 * 1000;

        return 10000;
    }

    public void ShutdownClientConnection()
    {
        UBot.Core.RuntimeAccess.Core.Proxy?.Client?.Shutdown();
    }

    public void StartGame()
    {
        UBot.Core.RuntimeAccess.Session.Start();
    }

    public void SendServerListRequest()
    {
        UBot.Core.RuntimeAccess.Packets.SendPacket(new Packet(0x6101, true), PacketDestination.Server);
    }

    public void SendKeepAlive()
    {
        UBot.Core.RuntimeAccess.Packets.SendPacket(new Packet(0x2002), PacketDestination.Server);
    }
}
