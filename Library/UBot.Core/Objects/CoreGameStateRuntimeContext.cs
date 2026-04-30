using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using UBot.Core.Abstractions;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.IO;
using UBot.Core.Network;
using UBot.Core.ProtocolServices;
using UBot.Core.Objects.Inventory;
using UBot.Core.Objects.Spawn;
using UBot.Core.Services;
using UBot.Protocol;

namespace UBot.Core.Objects;

internal sealed class CoreGameStateRuntimeContext : IGameStateRuntimeContext
{
    private readonly IGameSession _game;
    private readonly IKernelRuntime _kernel;
    private readonly IGlobalSettings _globalSettings;
    private readonly IPlayerSettings _playerSettings;
    private readonly UBot.Core.Abstractions.Network.IPacketDispatcher _packetDispatcher;
    private readonly UBot.Core.Abstractions.Services.IScriptEventBus _eventBus;

    public CoreGameStateRuntimeContext()
        : this(
            Runtime.GameSession.Shared,
            new Runtime.KernelRuntime(),
            new GlobalSettings(),
            new PlayerSettings(),
            new Network.PacketDispatcher(),
            new Event.ScriptEventBus())
    {
    }

    public CoreGameStateRuntimeContext(
        IGameSession game,
        IKernelRuntime kernel,
        IGlobalSettings globalSettings,
        IPlayerSettings playerSettings,
        UBot.Core.Abstractions.Network.IPacketDispatcher packetDispatcher,
        UBot.Core.Abstractions.Services.IScriptEventBus eventBus)
    {
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _globalSettings = globalSettings ?? throw new ArgumentNullException(nameof(globalSettings));
        _playerSettings = playerSettings ?? throw new ArgumentNullException(nameof(playerSettings));
        _packetDispatcher = packetDispatcher ?? throw new ArgumentNullException(nameof(packetDispatcher));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public GameClientType ClientType => _game.ClientType;
    public IReferenceManager References => _game.ReferenceManager;
    public int TickCount => _kernel.TickCount;
    public bool IsBotRunning => UBot.Core.RuntimeAccess.Core.Bot?.Running == true;
    public bool IsPlayerInAction => UBot.Core.RuntimeAccess.Session.Player?.InAction == true;
    public string PlayerName => UBot.Core.RuntimeAccess.Session.Player?.Name;
    public uint PlayerUniqueId => UBot.Core.RuntimeAccess.Session.Player?.UniqueId ?? 0;
    public int PlayerLevel => UBot.Core.RuntimeAccess.Session.Player?.Level ?? 0;
    public object Player => _game.Player;
    public object SelectedEntity => _game.SelectedEntity;
    public object AcceptanceRequest => _game.AcceptanceRequest;

    public object GetReference(string kind, object key)
    {
        return kind switch
        {
            "RefObjChar" => UBot.Core.RuntimeAccess.Session.ReferenceManager.GetRefObjChar(Convert.ToUInt32(key)),
            "RefItem" => key is string codeName
                ? UBot.Core.RuntimeAccess.Session.ReferenceManager.GetRefItem(codeName)
                : UBot.Core.RuntimeAccess.Session.ReferenceManager.GetRefItem(Convert.ToUInt32(key)),
            "RefLevel" => UBot.Core.RuntimeAccess.Session.ReferenceManager.GetRefLevel(Convert.ToByte(key)),
            "RefSkill" => key is string skillCode
                ? UBot.Core.RuntimeAccess.Session.ReferenceManager.GetRefSkill(skillCode)
                : UBot.Core.RuntimeAccess.Session.ReferenceManager.GetRefSkill(Convert.ToUInt32(key)),
            "RefQuest" => UBot.Core.RuntimeAccess.Session.ReferenceManager.GetRefQuest(Convert.ToUInt32(key)),
            "AbilityItem" => ((uint itemId, byte optLevel))key is var tuple
                ? UBot.Core.RuntimeAccess.Session.ReferenceManager.GetAbilityItem(tuple.itemId, tuple.optLevel)
                : null,
            "ExtraAbilityItems" => ((uint itemId, byte optLevel))key is var tuple
                ? UBot.Core.RuntimeAccess.Session.ReferenceManager.GetExtraAbilityItems(tuple.itemId, tuple.optLevel)
                : null,
            _ => null,
        };
    }

    public object GetEntity(Type entityType, object key)
    {
        if (key is string name && typeof(SpawnedPlayer).IsAssignableFrom(entityType))
            return SpawnManager.GetEntity<SpawnedPlayer>(p => p.Name == name);

        var method = typeof(SpawnManager)
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .First(m => m.Name == nameof(SpawnManager.GetEntity) && m.GetParameters()[0].ParameterType == typeof(uint))
            .MakeGenericMethod(entityType);
        return method.Invoke(null, new object[] { Convert.ToUInt32(key) });
    }

    public object GetEntities(Type entityType, Func<object, bool> predicate)
    {
        if (entityType == typeof(SpawnedBionic))
        {
            return SpawnManager.TryGetEntities<SpawnedBionic>(e => predicate(e), out var entities)
                ? entities.ToList()
                : null;
        }

        return null;
    }

    public bool SendPlayerMove(object destination, bool sleep)
    {
        var target = (Position)destination;
        var player = UBot.Core.RuntimeAccess.Session.Player;
        var distance = player.Movement.Source.DistanceTo(target);
        if (distance > 150)
        {
            Log.Debug($"Player.Move: Target position too far away! Target distance: {Math.Round(distance, 2)}");
            return false;
        }

        if (player.HasActiveVehicle)
        {
            player.Vehicle.MoveTo(target, sleep);
            return true;
        }

        var packet = new Packet(0x7021);
        packet.WriteByte(1);

        if (!player.IsInDungeon)
        {
            target.Region.Serialize(packet);
            packet.WriteShort(target.XOffset);
            packet.WriteShort(target.ZOffset);
            packet.WriteShort(target.YOffset);
        }
        else
        {
            player.Position.Region.Serialize(packet);
            packet.WriteInt(target.XOffset);
            packet.WriteInt(target.ZOffset);
            packet.WriteInt(target.YOffset);
        }

        var awaitCallback = new AwaitCallback(
            response => response.ReadUInt() == player.UniqueId
                ? AwaitCallbackResult.Success
                : AwaitCallbackResult.ConditionFailed,
            0xB021
        );

        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server, awaitCallback);
        awaitCallback.AwaitResponse();

        if (!awaitCallback.IsCompleted)
            return false;

        if (sleep)
            Thread.Sleep(Convert.ToInt32(distance / player.ActualSpeed * 10000));

        return true;
    }

    public bool SendEnterBerzerkMode()
    {
        var packet = new Packet(0x70A7);
        packet.WriteByte(0x1);

        var callback = new AwaitCallback(null, 0xB0A7);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server, callback);
        callback.AwaitResponse(500);
        return callback.IsCompleted;
    }

    public bool SendSelectEntity(uint uniqueId)
    {
        var packet = new Packet(0x7045);
        packet.WriteUInt(uniqueId);

        var awaitCallback = new AwaitCallback(
            response =>
            {
                var result = response.ReadByte() == 0x01;
                if (result)
                    return response.ReadUInt() == uniqueId
                        ? AwaitCallbackResult.Success
                        : AwaitCallbackResult.ConditionFailed;

                return AwaitCallbackResult.Fail;
            },
            0xB045
        );

        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server, awaitCallback);
        awaitCallback.AwaitResponse();
        return awaitCallback.IsCompleted;
    }

    public bool SendDeselectEntity(uint uniqueId)
    {
        var packet = new Packet(0x704B);
        packet.WriteUInt(uniqueId);

        var awaitResult = new AwaitCallback(
            response =>
            {
                var successFlag = response.ReadByte();
                if (successFlag == 2)
                {
                    var errorCode = response.ReadUShort();
                    Log.Debug($"Error deselecting Entity {uniqueId} [Code={errorCode:X4}]");
                    return AwaitCallbackResult.Fail;
                }

                return AwaitCallbackResult.Success;
            },
            0xB04B
        );

        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server, awaitResult);
        awaitResult.AwaitResponse();
        return awaitResult.IsCompleted;
    }

    public bool SendPartyInvite(uint playerUniqueId, bool isInParty, byte partyType)
    {
        var packet = new Packet((ushort)(isInParty ? 0x7062 : 0x7060));
        packet.WriteUInt(playerUniqueId);
        if (!isInParty)
            packet.WriteByte(partyType);

        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
        return true;
    }

    public void SendPartyLeave()
    {
        UBot.Core.RuntimeAccess.Packets.SendPacket(new Packet(0x7061), PacketDestination.Server);
    }

    public bool SendInventoryMove(byte sourceSlot, byte destinationSlot, ushort amount)
    {
        var packet = new Packet(0x7034);
        packet.WriteByte(0);
        packet.WriteByte(sourceSlot);
        packet.WriteByte(destinationSlot);
        packet.WriteUShort(amount);

        var asyncResult = CreateMoveCallback(sourceSlot, destinationSlot, 0x00);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server, asyncResult);
        asyncResult.AwaitResponse(500);
        return asyncResult.IsCompleted;
    }

    public bool SendStorageMove(byte sourceSlot, byte destinationSlot, ushort amount, object npc)
    {
        var bionic = (SpawnedBionic)npc;
        if (bionic?.UniqueId == 0)
            return false;

        var packet = new Packet(0x7034);
        packet.WriteByte(bionic.Record.CodeName.Contains("WAREHOUSE") ? 0x01 : 0x1D);
        packet.WriteByte(sourceSlot);
        packet.WriteByte(destinationSlot);
        packet.WriteUShort(amount);
        packet.WriteUInt(bionic.UniqueId);

        var asyncResult = CreateMoveCallback(sourceSlot, destinationSlot, 0x01, 0x1D);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server, asyncResult);
        asyncResult.AwaitResponse(500);
        return asyncResult.IsCompleted;
    }

    public bool SendUseInventoryItem(byte slot, int tid)
    {
        var packet = CreateUseInventoryItemPacket(slot, tid);
        var asyncCallback = new AwaitCallback(
            response => response.ReadByte() == 0x01 ? AwaitCallbackResult.Success : AwaitCallbackResult.Fail,
            0xB04C
        );

        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server, asyncCallback);
        asyncCallback.AwaitResponse(500);
        return asyncCallback.IsCompleted;
    }

    public bool SendUseInventoryItemTo(byte slot, int tid, byte destinationSlot, int mapId)
    {
        var packet = CreateUseInventoryItemPacket(slot, tid);
        packet.WriteByte(destinationSlot);

        if (mapId > -1)
            packet.WriteInt(mapId);

        var asyncCallback = new AwaitCallback(
            response => response.ReadByte() == 0x01 ? AwaitCallbackResult.Success : AwaitCallbackResult.Fail,
            0xB04C
        );

        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server, asyncCallback);
        asyncCallback.AwaitResponse(500);
        return asyncCallback.IsCompleted;
    }

    public void SendUseInventoryItemFor(byte slot, int tid, uint uniqueId)
    {
        var packet = CreateUseInventoryItemPacket(slot, tid);
        packet.WriteUInt(uniqueId);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
    }

    public bool SendDropInventoryItem(byte slot, bool cos, uint? cosUniqueId)
    {
        var packet = new Packet(0x7034);
        if (cos)
        {
            packet.WriteByte(InventoryOperation.SP_DROP_ITEM_COS);
            packet.WriteUInt(cosUniqueId ?? 0);
        }
        else
        {
            packet.WriteByte(InventoryOperation.SP_DROP_ITEM);
        }

        packet.WriteByte(slot);
        UBot.Core.RuntimeAccess.Packets.SendPacket(packet, PacketDestination.Server);
        return true;
    }

    public bool HasPendingPartyRequest()
    {
        return UBot.Core.RuntimeAccess.Session.AcceptanceRequest?.Type == InviteRequestType.Party1
            || (UBot.Core.RuntimeAccess.Session.AcceptanceRequest?.Type == InviteRequestType.Party2 && UBot.Core.RuntimeAccess.Session.AcceptanceRequest.Player != null);
    }

    public bool IsSelectedEntityNpc() => UBot.Core.RuntimeAccess.Session.SelectedEntity is SpawnedNpcNpc;

    public bool IsBehindObstacle(object position)
    {
        return UBot.Core.RuntimeAccess.Session.Player != null && UBot.Core.RuntimeAccess.Session.Player.Position.HasCollisionBetween((Position)position);
    }

    public double DistanceToPlayer(object position)
    {
        return UBot.Core.RuntimeAccess.Session.Player == null ? 0 : UBot.Core.RuntimeAccess.Session.Player.Movement.Source.DistanceTo((Position)position);
    }

    public bool StopBot()
    {
        UBot.Core.RuntimeAccess.Core.Bot.Stop();
        return true;
    }

    public bool GetConfigBool(string key) => _playerSettings.Get<bool>(key);

    public void FireEvent(string eventName, params object[] args) => _eventBus.RaiseEvent(eventName, args);

    public void LogDebug(string message) => Log.Debug(message);

    public void LogNotify(string message) => Log.Notify(message);

    private static Packet CreateUseInventoryItemPacket(byte slot, int tid)
    {
        var packet = new Packet(0x704C);
        packet.WriteByte(slot);

        if (UBot.Core.RuntimeAccess.Session.ClientType > GameClientType.Vietnam)
            packet.WriteInt(tid);
        else
            packet.WriteUShort((ushort)tid);

        return packet;
    }

    private static AwaitCallback CreateMoveCallback(byte sourceSlot, byte destinationSlot, params byte[] validOperations)
    {
        return new AwaitCallback(
            response =>
            {
                var result = response.ReadByte();
                if (result == 0x01)
                {
                    var operation = response.ReadByte();
                    if (!validOperations.Contains(operation))
                        return AwaitCallbackResult.ConditionFailed;

                    var source = response.ReadByte();
                    var destination = response.ReadByte();
                    return source == sourceSlot && destination == destinationSlot
                        ? AwaitCallbackResult.Success
                        : AwaitCallbackResult.Fail;
                }

                return AwaitCallbackResult.Fail;
            },
            0xB034
        );
    }
}

internal static class CoreGameStateRuntimeContextBootstrap
{
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Initialize()
    {
        Runtime.CoreRuntimeBootstrapper.Initialize();
    }
}
