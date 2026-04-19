using UBot.Core.Network;
using UBot.General.Components;

namespace UBot.General.PacketHandler
{
    internal class GatewayLoginRequestHook : IPacketHook
    {
        public ushort Opcode => 0x610A;

        public PacketDestination Destination => PacketDestination.Server;

        public Packet ReplacePacket(Packet packet)
        {
            AutoLogin.Cts?.Cancel();
            AutoLogin.SetAgentCredentialRewrite(
                AutoLogin.IsHandling || AutoLogin.IsAgentCredentialRewriteArmed
            );
            return packet;
        }
    }
}
