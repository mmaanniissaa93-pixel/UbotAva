using System.Numerics;
using System.Runtime.CompilerServices;
using UBot.Core.Network;
using UBot.Core.Objects.Item;
using UBot.Core.Objects.Party;
using UBot.Core.Objects.Skill;
using UBot.Core.Objects.Spawn;
using UBot.NavMeshApi;
using UBot.Protocol.Legacy;
using UBot.Core;

namespace UBot.Core.Objects;

public static class DomainPacketReader
{
    public static Position ReadPosition(this Packet packet)
    {
        return new Position
        {
            Region = packet.ReadUShort(),
            XOffset = packet.ReadFloat(),
            ZOffset = packet.ReadFloat(),
            YOffset = packet.ReadFloat(),
            Angle = packet.ReadShort(),
        };
    }

    public static Position ReadPositionInt(this Packet packet)
    {
        return new Position
        {
            Region = packet.ReadUShort(),
            XOffset = packet.ReadInt(),
            ZOffset = packet.ReadInt(),
            YOffset = packet.ReadInt(),
        };
    }

    public static Position ReadPositionConditional(this Packet packet, bool parseLayerWorldId = true)
    {
        Position position = new() { Region = packet.ReadUShort() };

        if (!position.Region.IsDungeon)
        {
            position.XOffset = packet.ReadShort();
            position.ZOffset = packet.ReadShort();
            position.YOffset = packet.ReadShort();
        }
        else
        {
            position.XOffset = packet.ReadInt();
            position.ZOffset = packet.ReadInt();
            position.YOffset = packet.ReadInt();
        }

        if (parseLayerWorldId)
        {
            position.WorldId = packet.ReadShort();
            position.LayerId = packet.ReadShort();
        }

        return position;
    }

    public static Movement ReadMotionMovement(this Packet packet)
    {
        var result = new Movement { HasDestination = packet.ReadBool() };

        if (result.HasDestination)
        {
            result.Destination = packet.ReadPositionConditional(false);
        }
        else
        {
            packet.ReadByte();
            result.HasAngle = true;
            result.Angle = packet.ReadShort();
        }

        result.HasSource = packet.ReadBool();
        if (result.HasSource)
        {
            result.Source = new Position { Region = packet.ReadUShort() };

            if (result.Source.Region.IsDungeon)
            {
                result.Source.XOffset = packet.ReadInt() / 10f;
                result.Source.ZOffset = packet.ReadFloat();
                result.Source.YOffset = packet.ReadInt() / 10f;
            }
            else
            {
                result.Source.XOffset = packet.ReadShort() / 10f;
                result.Source.ZOffset = packet.ReadFloat();
                result.Source.YOffset = packet.ReadShort() / 10f;
            }
        }

        return result;
    }

    public static Movement ReadMovement(this Packet packet)
    {
        var result = new Movement
        {
            Source = packet.ReadPosition(),
            HasDestination = packet.ReadBool(),
            Type = (MovementType)packet.ReadByte(),
        };

        if (result.HasDestination)
        {
            result.Destination = packet.ReadPositionConditional(false);
        }
        else
        {
            packet.ReadByte();
            result.HasAngle = true;
            result.Angle = packet.ReadShort();
        }

        return result;
    }

    public static RentInfo ReadRentInfo(this Packet packet, GameClientType clientType)
    {
        var result = new RentInfo { Type = packet.ReadUInt() };

        switch (result.Type)
        {
            case 1:
                result.CanDelete = packet.ReadUShort();
                if (clientType >= GameClientType.Chinese_Old && clientType != GameClientType.Rigid)
                {
                    result.PeriodBeginTime = packet.ReadULong();
                    result.PeriodEndTime = packet.ReadULong();
                }
                else
                {
                    result.PeriodBeginTime = packet.ReadUInt();
                    result.PeriodEndTime = packet.ReadUInt();
                }
                break;

            case 2:
                result.CanDelete = packet.ReadUShort();
                result.CanRecharge = packet.ReadUShort();
                result.MeterRateTime = packet.ReadUInt();
                break;

            case 3:
                result.CanDelete = packet.ReadUShort();
                result.CanRecharge = packet.ReadUShort();
                if (clientType >= GameClientType.Chinese_Old && clientType != GameClientType.Rigid)
                {
                    result.PeriodBeginTime = packet.ReadULong();
                    result.PeriodEndTime = packet.ReadULong();
                    result.PackingTime = packet.ReadULong();
                }
                else
                {
                    result.PeriodBeginTime = packet.ReadUInt();
                    result.PeriodEndTime = packet.ReadUInt();
                    result.PackingTime = packet.ReadUInt();
                }
                break;
        }

        return result;
    }

    public static MagicOptionInfo ReadMagicOptionInfo(this Packet packet)
    {
        return new MagicOptionInfo { Id = packet.ReadUInt(), Value = packet.ReadUInt() };
    }

    public static BindingOption ReadBindingOption(this Packet packet, BindingOptionType type)
    {
        return new BindingOption
        {
            Type = type,
            Slot = packet.ReadByte(),
            Id = packet.ReadUInt(),
            Value = packet.ReadUInt(),
        };
    }

    public static SkillInfo ReadSkillInfo(this Packet packet)
    {
        return new SkillInfo(packet.ReadUInt(), packet.ReadBool());
    }

    public static MasteryInfo ReadMasteryInfo(this Packet packet)
    {
        return new MasteryInfo { Id = packet.ReadUInt(), Level = packet.ReadByte() };
    }

    public static PartyMember ReadPartyMember(this Packet packet)
    {
        var result = new PartyMember();

        packet.ReadByte();
        result.MemberId = packet.ReadUInt();
        result.Name = packet.ReadString();
        result.ObjectId = packet.ReadUInt();
        result.Level = packet.ReadByte();
        result.HealthMana = packet.ReadByte();
        result.Position = packet.ReadPositionConditional();
        result.Guild = packet.ReadString();

        packet.ReadByte();

        result.MasteryId1 = packet.ReadUInt();
        result.MasteryId2 = packet.ReadUInt();

        return result;
    }

    public static void Serialize(this Region region, Packet packet)
    {
        packet.WriteUShort(region.Id);
    }

    public static bool TryGetNavMeshTransform(this Position position, out NavMeshTransform transform)
    {
        transform = new NavMeshTransform(position.Region.Id, new Vector3(position.XOffset, position.ZOffset, position.YOffset));

        return NavMeshManager.ResolveCellAndHeight(transform);
    }

    public static bool HasCollisionBetween(this Position source, Position destination, NavMeshRaycastType type)
    {
        if (!LegacyKernel.EnableCollisionDetection)
            return false;

        if (!source.TryGetNavMeshTransform(out var srcTransform) || !destination.TryGetNavMeshTransform(out var destTransform))
            return false;

        return !NavMeshManager.Raycast(srcTransform, destTransform, type);
    }
}

public static class DomainRuntimeBridge
{
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    public static void Initialize()
    {
        Position.RuntimeContext = new CorePositionRuntimeContext();
        SkillInfo.RuntimeContext = new CoreSkillInfoRuntimeContext();
        PartyMember.RuntimeContext = new CorePartyMemberRuntimeContext();
    }

    private sealed class CorePositionRuntimeContext : IPositionRuntimeContext
    {
        public double DistanceToPlayer(Position position)
        {
            return LegacyGame.Player == null ? 0 : position.DistanceTo(LegacyGame.Player.Position);
        }

        public bool HasCollisionBetween(Position source, Position destination)
        {
            return source.HasCollisionBetween(destination, NavMeshRaycastType.Attack);
        }
    }

    private sealed class CoreSkillInfoRuntimeContext : ISkillInfoRuntimeContext
    {
        public int TickCount => LegacyKernel.TickCount;
        public int PlayerMana => LegacyGame.Player?.Mana ?? 0;

        public byte? GetMasteryLevel(uint masteryId)
        {
            return LegacyGame.Player?.Skills?.GetMasteryInfoById(masteryId)?.Level;
        }

        public void Cast(SkillInfo skill, uint target, bool buff)
        {
            if (buff)
                SkillManager.CastBuff(skill, target);
            else
                SkillManager.CastSkill(skill, target);
        }

        public void CastAt(SkillInfo skill, Position target)
        {
            SkillManager.CastSkillAt(skill, target);
        }
    }

    private sealed class CorePartyMemberRuntimeContext : IPartyMemberRuntimeContext
    {
        public object GetSpawnedPlayer(string name)
        {
            return SpawnManager.GetEntity<SpawnedPlayer>(p => p.Name == name);
        }

        public void Banish(PartyMember member)
        {
            if (!LegacyGame.Party.IsLeader)
                return;

            var packet = new Packet(0x7063);
            packet.WriteUInt(member.MemberId);

            PacketManager.SendPacket(packet, PacketDestination.Server);
        }
    }
}
