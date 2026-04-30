using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Spawn;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Entity
{
    public class EntityUpdatePvpFlag : IPacketHandler 
    {
        public ushort Opcode => 0xB516;

        public PacketDestination Destination => PacketDestination.Client;

        public void Invoke(Packet packet)
        {
            if (!packet.ReadBool())
                return;

            var uniqueId = packet.ReadUInt();
            var flag = (PvpFlag)packet.ReadByte();

            if(CoreGame.Player.UniqueId == uniqueId)
            {
                var oldFlag = CoreGame.Player.PvpFlag;
                CoreGame.Player.PvpFlag = flag;
                Log.Notify($"Player pvp status updated from {oldFlag} to {flag}");
                return;
            }

            var entity = SpawnManager.GetEntity<SpawnedPlayer>(uniqueId);
            if (entity == null)
                return;

            var oldPvpFlag = entity.PvpCape;
            entity.PvpCape = flag;


            Log.Notify($"[{entity.Name}] pvp status updated from {oldPvpFlag} to {flag}");
        }
    }
}





