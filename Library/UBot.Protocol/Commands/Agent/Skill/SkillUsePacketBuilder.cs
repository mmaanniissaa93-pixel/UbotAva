using UBot.Core.Abstractions;
using UBot.Core;
using UBot.Core.Network;
using UBot.Core.Objects;

namespace UBot.Protocol.Commands.Agent.Skill;

public static class SkillUsePacketBuilder
{
    public static Packet BuildCastOnEntity(uint skillId, uint targetId, GameClientType clientType)
    {
        var packet = new Packet(0x7074);
        packet.WriteByte(ActionCommandType.Execute);
        packet.WriteByte(ActionType.Cast);
        packet.WriteUInt(skillId);
        packet.WriteByte(ActionTarget.Entity);

        if (clientType < GameClientType.Thailand)
            packet.WriteByte(1);

        packet.WriteUInt(targetId);
        return packet;
    }

    public static Packet BuildLegacyCastOnEntity(uint skillId, uint targetId)
    {
        var packet = new Packet(0x7074);
        packet.WriteByte(ActionCommandType.Execute);
        packet.WriteByte(ActionType.Cast);
        packet.WriteUInt(skillId);
        packet.WriteByte(ActionTarget.Entity);
        packet.WriteUInt(targetId);
        return packet;
    }

    public static Packet BuildBuff(uint skillId, bool targetsEntity, uint targetId)
    {
        var packet = new Packet(0x7074);
        packet.WriteByte(ActionCommandType.Execute);
        packet.WriteByte(ActionType.Cast);
        packet.WriteUInt(skillId);

        if (targetsEntity)
        {
            packet.WriteByte(ActionTarget.Entity);
            packet.WriteUInt(targetId);
        }
        else
        {
            packet.WriteByte(ActionTarget.None);
        }

        return packet;
    }

    public static Packet BuildCastAtPosition(uint skillId, IPosition target)
    {
        var packet = new Packet(0x7074);
        packet.WriteByte(ActionCommandType.Execute);
        packet.WriteByte(ActionType.Cast);
        packet.WriteUInt(skillId);
        packet.WriteByte(ActionTarget.Area);
        packet.WriteUShort(target.RegionID);

        if (target.Region?.IsDungeon == true)
        {
            packet.WriteShort((short)target.XOffset);
            packet.WriteShort((short)target.YOffset);
            packet.WriteShort((short)target.ZOffset);
        }
        else
        {
            packet.WriteInt((int)target.XOffset);
            packet.WriteInt((int)target.ZOffset);
            packet.WriteInt((int)target.YOffset);
        }

        return packet;
    }

    public static Packet BuildCastAtPositionExact(uint skillId, IPosition target)
    {
        var packet = new Packet(0x7074);
        packet.WriteByte(ActionCommandType.Execute);
        packet.WriteByte(ActionType.Cast);
        packet.WriteUInt(skillId);
        packet.WriteByte(ActionTarget.Area);
        packet.WriteUShort(target.RegionID);
        packet.WriteFloat(target.XOffset);
        packet.WriteFloat(target.ZOffset);
        packet.WriteFloat(target.YOffset);
        return packet;
    }

    public static Packet BuildAutoAttack(uint targetId, GameClientType clientType)
    {
        var packet = new Packet(0x7074);
        packet.WriteByte(ActionCommandType.Execute);
        packet.WriteByte(ActionType.Attack);
        packet.WriteByte(ActionTarget.Entity);

        if (clientType < GameClientType.Thailand)
            packet.WriteByte(1);

        packet.WriteUInt(targetId);
        return packet;
    }

    public static Packet BuildCancelBuff(uint skillId)
    {
        var packet = new Packet(0x7074);
        packet.WriteByte(ActionCommandType.Execute);
        packet.WriteByte(ActionType.Dispel);
        packet.WriteUInt(skillId);
        packet.WriteByte(ActionTarget.None);
        return packet;
    }

    public static Packet BuildCancelAction()
    {
        var packet = new Packet(0x7074);
        packet.WriteByte(ActionCommandType.Cancel);
        return packet;
    }
}
