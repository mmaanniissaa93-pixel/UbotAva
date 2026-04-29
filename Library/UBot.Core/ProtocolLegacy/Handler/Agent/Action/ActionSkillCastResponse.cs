using CoreGame = global::UBot.Core.Game;
using UBot.Core.Abstractions;
using UBot.Core.Network;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Objects;
using UBot.Core.Objects.Spawn;
using System.Diagnostics;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Action;

internal class ActionSkillCastResponse
{
    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Client;

    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0xB070;

    /// <summary>
    ///     Invokes the specified packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    public void Invoke(Packet packet)
    {
        var result = packet.ReadByte();
        if (result != 0x01)
        {
            var errorCode = packet.ReadByte();

            switch (errorCode)
            {
                case 0x0C:
                    // Same other skills are already running
                    break;

                case 0x0E:
                    CoreGame.Player.EquipAmmunition();
                    break;

                case 0x05:
                    Log.Debug("Skill cooldown error. Still have time!");
                    break;

                case 0x06: // invalid target
                    break;

                case 0x10: // obstacle
                    EventManager.FireEvent("OnTargetBehindObstacle");
                    break;

                default:
                    Log.Error($"Invalid skill error code: 0x{errorCode:X2}");
                    break;
            }

            return;
        }

        var actionCode = packet.ReadByte();

        if (CoreGame.ClientType > GameClientType.Thailand)
            packet.ReadByte(); // always 0x30

        var action = new Objects.Action
        {
            Code = actionCode,
            SkillId = packet.ReadUInt(),
            ExecutorId = packet.ReadUInt(),
            Id = packet.ReadUInt(),
        };

        if (CoreGame.ClientType > GameClientType.Chinese && CoreGame.ClientType != GameClientType.Japanese)
            action.UnknownId = packet.ReadUInt();

        action.TargetId = packet.ReadUInt();
        if (
            CoreGame.ClientType == GameClientType.Turkey
            || CoreGame.ClientType == GameClientType.Global
            || CoreGame.ClientType == GameClientType.VTC_Game
            || CoreGame.ClientType == GameClientType.RuSro
            || CoreGame.ClientType == GameClientType.Korean
            || CoreGame.ClientType == GameClientType.Japanese
            || CoreGame.ClientType == GameClientType.Taiwan
        )
        {
            packet.ReadByte();
            action.Flag = (ActionStateFlag)packet.ReadByte();
        }
        else if (CoreGame.ClientType == GameClientType.Rigid)
        {
            action.Flag = (ActionStateFlag)packet.ReadByte();
            var flag = packet.ReadByte();
            Debug.WriteLine("Flag:" + flag);
        }
        else
        {
            action.Flag = (ActionStateFlag)packet.ReadByte();
        }

        /*if (CoreGame.ClientType >= GameClientType.Chinese)
            packet.ReadByte();

        action.Flag = (ActionStateFlag)packet.ReadByte();*/
        action.ReadPacket(packet);

        if (action.PlayerIsExecutor)
        {
            //CoreGame.Player.StopMoving();

            var skillInfo = CoreGame.Player.Skills.GetSkillInfoById(action.SkillId);
            if (skillInfo == null)
                skillInfo = SkillManager.Buffs.Find(p => p.Id == action.SkillId);

            if (skillInfo != null && !IsBuffLike(skillInfo))
                skillInfo.Update();

            EventManager.FireEvent("OnCastSkill", action.SkillId);

            return;
        }

        if (!action.TryGetExecutor<SpawnedBionic>(out var executor))
            return;

        executor.TargetId = action.TargetId;
        //executor.StopMoving();

        if (!action.PlayerIsTarget)
            return;

        EventManager.FireEvent("OnEnemySkillOnPlayer");

        executor.StartAttackingTimer();
    }

    private static bool IsBuffLike(Objects.Skill.SkillInfo skill)
    {
        var record = skill?.Record;
        if (record == null || skill.IsPassive)
            return false;

        if (record.TargetGroup_Enemy_M || record.TargetGroup_Enemy_P)
            return false;

        if (record.TargetGroup_Self || record.TargetGroup_Ally || record.TargetGroup_Party)
            return true;

        return !record.Target_Required && record.Params.Contains(1685418593);
    }
}






