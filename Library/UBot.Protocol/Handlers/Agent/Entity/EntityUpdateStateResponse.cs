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
                var attackers = player.GetAttackers();
                if (attackers.Any(e => e.UniqueId == uniqueId)
                    && entity.State.LifeState == LifeState.Dead
                )
                    UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnKillEnemy");

                if (uniqueId == CoreGame.SelectedEntity?.UniqueId && entity.State.LifeState == LifeState.Dead)
                {
                    UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnKillSelectedEnemy");
                    CoreGame.SelectedEntity = null;
                }

                UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnUpdateEntityLifeState", uniqueId);

                if (uniqueId == player.UniqueId && entity.State.LifeState == LifeState.Dead)
                    UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnPlayerDied");

                break;

            case 1:

                var motionState = (MotionState)state;
                entity.State.MotionState = motionState;

                Log.Debug("[Entity] EntityUpdate: type=1 MotionState=" + motionState + " uniqueId=" + uniqueId);

                switch (motionState)
                {
                    case MotionState.Walking:

                        entity.Movement.Type = MovementType.Walking;

                        break;
                    case MotionState.Running:

                        entity.Movement.Type = MovementType.Running;

                        break;
                }

                UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnUpdateEntityMotionState", uniqueId);

                break;

            case 4:

                entity.State.BodyState = (BodyState)state;

                UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnUpdateEntityBodyState", uniqueId);
                break;

            case 7:

                entity.State.PvpState = (PvpState)state;
                UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnUpdateEntityPvpState", uniqueId);

                break;

            case 8:

                entity.State.BattleState = (BattleState)state;

                UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnUpdateEntityBattleState", uniqueId);

                break;

            case 11:

                var scrollState = (ScrollState)state;
                entity.State.ScrollState = scrollState;

                //Do not stop bot on scroll cancel, it will stop bot on death while teleporting.
                //if (uniqueId == CoreGame.Player.UniqueId)
                //    if (scrollState == ScrollState.Cancel && CoreKernel.Bot.Running)
                //        CoreKernel.Bot.Stop();

                UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnUpdateEntityScrollState", uniqueId);

                break;

            default:
                Log.Warn("EntityUpdate: Unknown update type " + type);
                break;
        }
    }
}





