using System;
using System.Linq;
using UBot.Core.Abstractions.Services;
using UBot.Core.Components;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Cos;
using UBot.Core.Objects.Skill;
using UBot.Core.Objects.Spawn;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreScriptRuntime : IScriptRuntime
{
    public string BasePath => UBot.Core.RuntimeAccess.Core.BasePath;

    public bool GameReady => UBot.Core.RuntimeAccess.Session.Ready;

    public bool IsBotRunning => UBot.Core.RuntimeAccess.Core.Bot?.Running == true;

    public bool PlayerInAction => UBot.Core.RuntimeAccess.Session.Player?.InAction == true;

    public bool PlayerHasActiveVehicle => UBot.Core.RuntimeAccess.Session.Player?.HasActiveVehicle == true;

    public bool PlayerIsInDungeon => UBot.Core.RuntimeAccess.Session.Player?.IsInDungeon == true;

    public object PlayerPosition => UBot.Core.RuntimeAccess.Session.Player?.Position;

    public object PlayerMovementSource => UBot.Core.RuntimeAccess.Session.Player?.Movement.Source;

    public object FellowPosition => UBot.Core.RuntimeAccess.Session.Player?.Fellow?.Position;

    public object CreatePosition(byte xSector, byte ySector, float xOffset, float yOffset, float zOffset)
    {
        return new Position(xSector, ySector, xOffset, yOffset, zOffset);
    }

    public double Distance(object source, object destination)
    {
        return source is Position sourcePosition && destination is Position destinationPosition
            ? sourcePosition.DistanceTo(destinationPosition)
            : 0;
    }

    public double DistanceToPlayer(object position)
    {
        return position is Position typedPosition && UBot.Core.RuntimeAccess.Session.Player != null
            ? UBot.Core.RuntimeAccess.Session.Player.Movement.Source.DistanceTo(typedPosition)
            : 0;
    }

    public bool HasCollisionBetween(object source, object destination)
    {
        return source is Position sourcePosition
            && destination is Position destinationPosition
            && sourcePosition.HasCollisionBetween(destinationPosition);
    }

    public bool MovePlayerTo(object destination)
    {
        return destination is Position position && UBot.Core.RuntimeAccess.Session.Player?.MoveTo(position, false) == true;
    }

    public int EstimateMoveDelayMilliseconds(object source, object destination)
    {
        var distance = Distance(source, destination);
        var speed = GetMovementSpeed();
        if (distance <= 0 || speed <= 0)
            return 0;

        return Math.Max(0, Convert.ToInt32(distance / speed * 10000));
    }

    public bool GetConfigBool(string key, bool defaultValue) => UBot.Core.RuntimeAccess.Player.Get(key, defaultValue);

    public string GetConfigString(string key, string defaultValue) => UBot.Core.RuntimeAccess.Player.Get(key, defaultValue);

    public bool HasActiveSpeedBuff()
    {
        return UBot.Core.RuntimeAccess.Session.Player?.State.ActiveBuffs.FindIndex(p => p.Record.Params.Contains(1752396901)) >= 0;
    }

    public bool UseSpeedDrug()
    {
        var item = UBot.Core.RuntimeAccess.Session.Player?.Inventory.GetItem(
            new TypeIdFilter(3, 3, 13, 1),
            p => p.Record.Desc1.Contains("_SPEED_")
        );
        return item?.Use() == true;
    }

    public void SummonFellow()
    {
        UBot.Core.RuntimeAccess.Session.Player?.SummonFellow();
    }

    public void CastFellowSkill(string codeName)
    {
        UBot.Core.RuntimeAccess.Session.Player?.Fellow?.CastSkill(codeName);
    }

    public void MountFellow()
    {
        UBot.Core.RuntimeAccess.Session.Player?.Fellow?.Mount();
    }

    public void SummonVehicle()
    {
        UBot.Core.RuntimeAccess.Session.Player?.SummonVehicle();
    }

    public void DismountVehicle()
    {
        UBot.Core.RuntimeAccess.Session.Player?.Vehicle?.Dismount();
    }

    public bool Teleport(string npcCodeName, uint destination)
    {
        if (!SpawnManager.TryGetEntity<SpawnedBionic>(p => p.Record.CodeName == npcCodeName, out var entity))
        {
            Log.Debug("[Script] Could not find teleporter NPC " + npcCodeName);
            return false;
        }

        if (!entity.TrySelect())
            return false;

        var packet = new Packet(0x705A);
        packet.WriteUInt(entity.UniqueId);
        packet.WriteByte(0x02);
        packet.WriteUInt(destination);

        var gameReadyOpcode = (ushort)(UBot.Core.RuntimeAccess.Session.ClientType == GameClientType.Rigid ? 0x3077 : 0x3012);
        var callback = new AwaitCallback(null, gameReadyOpcode);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server, callback);

        callback.AwaitResponse(30_000);
        return true;
    }

    public object GetPlayerSkillByCodeName(string codeName)
    {
        return UBot.Core.RuntimeAccess.Session.Player?.Skills.GetSkillByCodeName(codeName);
    }

    public void CastBuff(object skill)
    {
        if (skill is SkillInfo skillInfo)
            SkillManager.CastBuff(skillInfo);
    }

    public void FireEvent(string eventName, params object[] args)
    {
        UBot.Core.RuntimeAccess.Events.FireEvent(eventName, args);
    }

    private static float GetMovementSpeed()
    {
        var vehicle = UBot.Core.RuntimeAccess.Session.Player?.Vehicle;
        SpawnedCos spawnedCos = null;
        if (vehicle != null && SpawnManager.TryGetEntity<SpawnedCos>(vehicle.UniqueId, out spawnedCos))
            return spawnedCos.ActualSpeed;

        return UBot.Core.RuntimeAccess.Session.Player?.ActualSpeed ?? 0;
    }
}
