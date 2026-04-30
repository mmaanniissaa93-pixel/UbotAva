using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Abstractions;
using UBot.Core.Network;
using UBot.Protocol.Legacy;
using UBot.Core;

namespace UBot.Protocol.Handlers.Agent.Cos
{
    public class AgentNotifyResponse : IPacketHandler
    {
        public ushort Opcode => 0x300C;

        public PacketDestination Destination => PacketDestination.Client;

        public void Invoke(Packet packet)
        {
            var noticeType = packet.ReadByte();
            if (CoreGame.ClientType > GameClientType.Thailand)
                packet.ReadByte();

            if (noticeType == 0x4 && ScriptManager.Running) //can't teleport while riding on vehicle
            {
                MoveScriptCommand.MustDismount = true;
            }
        }
    }
}






