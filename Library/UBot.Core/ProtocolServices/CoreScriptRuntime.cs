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
    public string BasePath => Kernel.BasePath;

    public bool GameReady => Game.Ready;

    public bool IsBotRunning => Kernel.Bot?.Running == true;

    public bool PlayerInAction => Game.Player?.InAction == true;

    public bool PlayerHasActiveVehicle => Game.Player?.HasActiveVehicle == true;

    public bool PlayerIsInDungeon => Game.Player?.IsInDungeon == true;

    public object PlayerPosition => Game.Player?.Position;

    public object PlayerMovementSource => Game.Player?.Movement.Source;

    public object FellowPosition => Game.Player?.Fellow?.Position;

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
        return position is Position typedPosition && Game.Player != null
            ? Game.Player.Movement.Source.DistanceTo(typedPosition)
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
        return destination is Position position && Game.Player?.MoveTo(position, false) == true;
    }

    public int EstimateMoveDelayMilliseconds(object source, object destination)
    {
        var distance = Distance(source, destination);
        var speed = GetMovementSpeed();
        if (distance <= 0 || speed <= 0)
            return 0;

        return Math.Max(0, Convert.ToInt32(distance / speed * 10000));
    }

    public bool GetConfigBool(string key, bool defaultValue) => PlayerConfig.Get(key, defaultValue);

    public string GetConfigString(string key, string defaultValue) => PlayerConfig.Get(key, defaultValue);

    public bool HasActiveSpeedBuff()
    {
        return Game.Player?.State.ActiveBuffs.FindIndex(p => p.Record.Params.Contains(1752396901)) >= 0;
    }

    public bool UseSpeedDrug()
    {
        var item = Game.Player?.Inventory.GetItem(
            new TypeIdFilter(3, 3, 13, 1),
            p => p.Record.Desc1.Contains("_SPEED_")
        );
        return item?.Use() == true;
    }

    public void SummonFellow()
    {
        Game.Player?.SummonFellow();
    }

    public void CastFellowSkill(string codeName)
    {
        Game.Player?.Fellow?.CastSkill(codeName);
    }

    public void MountFellow()
    {
        Game.Player?.Fellow?.Mount();
    }

    public void SummonVehicle()
    {
        Game.Player?.SummonVehicle();
    }

    public void DismountVehicle()
    {
        Game.Player?.Vehicle?.Dismount();
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

        var gameReadyOpcode = (ushort)(Game.ClientType == GameClientType.Rigid ? 0x3077 : 0x3012);
        var callback = new AwaitCallback(null, gameReadyOpcode);
        PacketManager.SendPacket(packet, PacketDestination.Server, callback);

        callback.AwaitResponse(30_000);
        return true;
    }

    public object GetPlayerSkillByCodeName(string codeName)
    {
        return Game.Player?.Skills.GetSkillByCodeName(codeName);
    }

    public void CastBuff(object skill)
    {
        if (skill is SkillInfo skillInfo)
            SkillManager.CastBuff(skillInfo);
    }

    public void FireEvent(string eventName, params object[] args)
    {
        Event.EventManager.FireEvent(eventName, args);
    }

    private static float GetMovementSpeed()
    {
        var vehicle = Game.Player?.Vehicle;
        SpawnedCos spawnedCos = null;
        if (vehicle != null && SpawnManager.TryGetEntity<SpawnedCos>(vehicle.UniqueId, out spawnedCos))
            return spawnedCos.ActualSpeed;

        return Game.Player?.ActualSpeed ?? 0;
    }
}
