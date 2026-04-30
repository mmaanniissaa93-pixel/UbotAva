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
    public GameClientType ClientType => Game.ClientType;
    public IReferenceManager References => Game.ReferenceManager;
    public int TickCount => Kernel.TickCount;
    public bool IsBotRunning => Kernel.Bot?.Running == true;
    public bool IsPlayerInAction => Game.Player?.InAction == true;
    public string PlayerName => Game.Player?.Name;
    public uint PlayerUniqueId => Game.Player?.UniqueId ?? 0;
    public int PlayerLevel => Game.Player?.Level ?? 0;
    public object Player => Game.Player;
    public object SelectedEntity => Game.SelectedEntity;
    public object AcceptanceRequest => Game.AcceptanceRequest;

    public object GetReference(string kind, object key)
    {
        return kind switch
        {
            "RefObjChar" => Game.ReferenceManager.GetRefObjChar(Convert.ToUInt32(key)),
            "RefItem" => key is string codeName
                ? Game.ReferenceManager.GetRefItem(codeName)
                : Game.ReferenceManager.GetRefItem(Convert.ToUInt32(key)),
            "RefLevel" => Game.ReferenceManager.GetRefLevel(Convert.ToByte(key)),
            "RefSkill" => key is string skillCode
                ? Game.ReferenceManager.GetRefSkill(skillCode)
                : Game.ReferenceManager.GetRefSkill(Convert.ToUInt32(key)),
            "RefQuest" => Game.ReferenceManager.GetRefQuest(Convert.ToUInt32(key)),
            "AbilityItem" => ((uint itemId, byte optLevel))key is var tuple
                ? Game.ReferenceManager.GetAbilityItem(tuple.itemId, tuple.optLevel)
                : null,
            "ExtraAbilityItems" => ((uint itemId, byte optLevel))key is var tuple
                ? Game.ReferenceManager.GetExtraAbilityItems(tuple.itemId, tuple.optLevel)
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
        var player = Game.Player;
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

        PacketManager.SendPacket(packet, PacketDestination.Server, awaitCallback);
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
        PacketManager.SendPacket(packet, PacketDestination.Server, callback);
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

        PacketManager.SendPacket(packet, PacketDestination.Server, awaitCallback);
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

        PacketManager.SendPacket(packet, PacketDestination.Server, awaitResult);
        awaitResult.AwaitResponse();
        return awaitResult.IsCompleted;
    }

    public bool SendPartyInvite(uint playerUniqueId, bool isInParty, byte partyType)
    {
        var packet = new Packet((ushort)(isInParty ? 0x7062 : 0x7060));
        packet.WriteUInt(playerUniqueId);
        if (!isInParty)
            packet.WriteByte(partyType);

        PacketManager.SendPacket(packet, PacketDestination.Server);
        return true;
    }

    public void SendPartyLeave()
    {
        PacketManager.SendPacket(new Packet(0x7061), PacketDestination.Server);
    }

    public bool SendInventoryMove(byte sourceSlot, byte destinationSlot, ushort amount)
    {
        var packet = new Packet(0x7034);
        packet.WriteByte(0);
        packet.WriteByte(sourceSlot);
        packet.WriteByte(destinationSlot);
        packet.WriteUShort(amount);

        var asyncResult = CreateMoveCallback(sourceSlot, destinationSlot, 0x00);
        PacketManager.SendPacket(packet, PacketDestination.Server, asyncResult);
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
        PacketManager.SendPacket(packet, PacketDestination.Server, asyncResult);
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

        PacketManager.SendPacket(packet, PacketDestination.Server, asyncCallback);
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

        PacketManager.SendPacket(packet, PacketDestination.Server, asyncCallback);
        asyncCallback.AwaitResponse(500);
        return asyncCallback.IsCompleted;
    }

    public void SendUseInventoryItemFor(byte slot, int tid, uint uniqueId)
    {
        var packet = CreateUseInventoryItemPacket(slot, tid);
        packet.WriteUInt(uniqueId);
        PacketManager.SendPacket(packet, PacketDestination.Server);
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
        PacketManager.SendPacket(packet, PacketDestination.Server);
        return true;
    }

    public bool HasPendingPartyRequest()
    {
        return Game.AcceptanceRequest?.Type == InviteRequestType.Party1
            || (Game.AcceptanceRequest?.Type == InviteRequestType.Party2 && Game.AcceptanceRequest.Player != null);
    }

    public bool IsSelectedEntityNpc() => Game.SelectedEntity is SpawnedNpcNpc;

    public bool IsBehindObstacle(object position)
    {
        return Game.Player != null && Game.Player.Position.HasCollisionBetween((Position)position);
    }

    public double DistanceToPlayer(object position)
    {
        return Game.Player == null ? 0 : Game.Player.Movement.Source.DistanceTo((Position)position);
    }

    public bool StopBot()
    {
        Kernel.Bot.Stop();
        return true;
    }

    public bool GetConfigBool(string key) => PlayerConfig.Get<bool>(key);

    public void FireEvent(string eventName, params object[] args) => EventManager.FireEvent(eventName, args);

    public void LogDebug(string message) => Log.Debug(message);

    public void LogNotify(string message) => Log.Notify(message);

    private static Packet CreateUseInventoryItemPacket(byte slot, int tid)
    {
        var packet = new Packet(0x704C);
        packet.WriteByte(slot);

        if (Game.ClientType > GameClientType.Vietnam)
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
        GameStateRuntimeProvider.Instance = new CoreGameStateRuntimeContext();
        var eventBus = new CoreScriptEventBus();
        var feedback = new CoreUIFeedbackService();

        ProtocolRuntime.GameState = GameStateRuntimeProvider.Instance;
        ProtocolRuntime.PacketDispatcher = new CorePacketDispatcher();
        ProtocolRuntime.LegacyHandler = new CoreLegacyProtocolHandler();
        ProtocolRuntime.EventBus = eventBus;
        ProtocolRuntime.Feedback = feedback;
        ProtocolRuntime.SpawnController = new CoreSpawnController(eventBus, feedback);
        ProtocolRuntime.Shopping = new CoreShoppingController();
        ProtocolRuntime.Cos = new CoreCosController();

        ServiceRuntime.GameState = GameStateRuntimeProvider.Instance;
        ServiceRuntime.PacketDispatcher = ProtocolRuntime.PacketDispatcher;
        ServiceRuntime.Environment = new CoreServiceRuntimeEnvironment();
        ServiceRuntime.Log = new CoreServiceLog();
        ServiceRuntime.PickupRuntime = new CorePickupRuntime();
        ServiceRuntime.PickupSettings = new CorePickupSettings();
        ServiceRuntime.InventoryRuntime = new CoreInventoryRuntime();
        ServiceRuntime.ShoppingRuntime = new CoreShoppingRuntime();
        ServiceRuntime.AlchemyRuntime = new CoreAlchemyRuntime();
        ServiceRuntime.AlchemyProgress = new CoreAlchemyProgress(feedback);
        ServiceRuntime.ScriptRuntime = new CoreScriptRuntime();
        ServiceRuntime.ScriptProgress = new CoreScriptProgress(feedback);
        ServiceRuntime.SpawnRuntime = new CoreSpawnRuntime();
        ServiceRuntime.SkillRuntime = new SkillRuntimeAdapter();
        ServiceRuntime.SkillConfig = new CoreSkillConfig();
        ServiceRuntime.ClientConnectionRuntime = new ClientConnectionRuntimeAdapter();
        ServiceRuntime.Clientless = new ClientlessService();
        ServiceRuntime.ClientNativeRuntime = new ClientNativeRuntimeAdapter();
        ServiceRuntime.ClientLaunchConfigProvider = new CoreClientLaunchConfigProvider();
        ServiceRuntime.ClientLaunchPolicy = new ClientLaunchPolicyService();
        ServiceRuntime.ProfileStorage = new ProfileFileStorage(new CoreAppPaths());
        ServiceRuntime.Profile = new ProfileService();
        PickupManager.Initialize(new PickupService());
        ShoppingManager.Initialize(new ShoppingService());
        AlchemyManager.Initialize(new AlchemyService());
        LanguageManager.Initialize(new LanguageService());
        ClientManager.Initialize(ServiceRuntime.ClientLaunchPolicy);
    }
}

