using CoreGame = global::UBot.Core.Game;
using UBot.Core.Abstractions;
using UBot.Core.Network;
using UBot.Core.Components;
using UBot.Core.Components.Scripting.Commands;

namespace UBot.Core.ProtocolLegacy.Handler.Agent
{
    internal class AgentNotifyResponse
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






