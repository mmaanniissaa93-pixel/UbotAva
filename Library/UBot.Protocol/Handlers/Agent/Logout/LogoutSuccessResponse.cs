using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Handlers.Agent.Logout;

public class LogoutSuccessResponse : IPacketHandler
{
    public ushort Opcode => 0x300A;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        ProtocolRuntime.LegacyHandler?.Invoke(nameof(LogoutSuccessResponse), packet);
    }
}
