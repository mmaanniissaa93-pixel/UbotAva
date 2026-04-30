using CoreKernel = UBot.Protocol.Legacy.LegacyKernel;
using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using Game = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using System.Linq;
using UBot.Core.Objects;
using UBot.Core.Objects.Spawn;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Entity;

public class EntityUpdateStateResponse : IPacketHandler 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x30BF;

    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Client;

    /// <summary>
    ///     Handles the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    public void Invoke(Packet packet)
    {
        var player = (Player)CoreGame.Player;
        if (player == null)
            return;

        var uniqueId = packet.ReadUInt();

        var type = packet.ReadByte();
        var state = packet.ReadByte();

        if (!SpawnManager.TryGetEntityIncludingMe<SpawnedBionic>(uniqueId, out var entity))
            return;

        switch (type)
        {
            case 0:

                entity.State.LifeState = (LifeState)state;
                if ( /*uniqueId == CoreGame.SelectedEntity?.UniqueId || */
                    player.GetAttackers().Any(e => e.UniqueId == uniqueId)
                    && entity.State.LifeState == LifeState.Dead
                )
                    EventManager.FireEvent("OnKillEnemy");

                if (uniqueId == CoreGame.SelectedEntity?.UniqueId && entity.State.LifeState == LifeState.Dead)
                {
                    EventManager.FireEvent("OnKillSelectedEnemy");
                    CoreGame.SelectedEntity = null;
                }

                EventManager.FireEvent("OnUpdateEntityLifeState", uniqueId);

                if (uniqueId == player.UniqueId && entity.State.LifeState == LifeState.Dead)
                    EventManager.FireEvent("OnPlayerDied");

                break;

            case 1:

                var motionState = (MotionState)state;
                entity.State.MotionState = motionState;

                switch (motionState)
                {
                    case MotionState.Walking:

                        entity.Movement.Type = MovementType.Walking;

                        break;
                    case MotionState.Running:

                        entity.Movement.Type = MovementType.Running;

                        break;
                }

                EventManager.FireEvent("OnUpdateEntityMotionState", uniqueId);

                break;

            case 4:

                entity.State.BodyState = (BodyState)state;

                EventManager.FireEvent("OnUpdateEntityBodyState", uniqueId);
                break;

            case 7:

                entity.State.PvpState = (PvpState)state;
                EventManager.FireEvent("OnUpdateEntityPvpState", uniqueId);

                break;

            case 8:

                entity.State.BattleState = (BattleState)state;

                EventManager.FireEvent("OnUpdateEntityBattleState", uniqueId);

                break;

            case 11:

                var scrollState = (ScrollState)state;
                entity.State.ScrollState = scrollState;

                //Do not stop bot on scroll cancel, it will stop bot on death while teleporting.
                //if (uniqueId == CoreGame.Player.UniqueId)
                //    if (scrollState == ScrollState.Cancel && CoreKernel.Bot.Running)
                //        CoreKernel.Bot.Stop();

                EventManager.FireEvent("OnUpdateEntityScrollState", uniqueId);

                break;

            default:
                Log.Error("EntityUpdate: Unknown update type " + type);
                break;
        }
    }
}





