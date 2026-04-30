using System;
using System.Collections.Generic;
using UBot.Core.Abstractions;
using UBot.Core.Abstractions.Network;
using UBot.Core.Abstractions.Services;
using UBot.Core.Client;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Party;
using UBot.Core.Objects.Spawn;
using UBot.Core.Plugins;
using UBot.FileSystem;
using GState = UBot.Core.Game;
using KState = UBot.Core.Kernel;
using GConfig = UBot.Core.GlobalConfig;
using PConfig = UBot.Core.PlayerConfig;
using PMgr = UBot.Core.Network.PacketManager;
using EMgr = UBot.Core.Event.EventManager;
using SR = UBot.Core.Services.ServiceRuntime;

namespace UBot.Core;

public static class RuntimeAccess
{
    public static GameSessionContext Session { get; } = new();
    public static KernelContext Core { get; } = new();
    public static GlobalSettings Global { get; } = new();
    public static PlayerSettings Player { get; } = new();
    public static PacketManagerContext Packets { get; } = new();
    public static EventBusContext Events { get; } = new();
    public static ServiceRuntimeContext Services { get; } = new();
}

public sealed class GameSessionContext
{
    public AcceptanceRequest AcceptanceRequest { get => GState.AcceptanceRequest; set => GState.AcceptanceRequest = value; }
    public byte[] MacAddress { get => GState.MacAddress; set => GState.MacAddress = value; }
    public ushort Port { get => GState.Port; set => GState.Port = value; }
    public IFileSystem MediaPk2 { get => GState.MediaPk2; set => GState.MediaPk2 = value; }
    public IFileSystem DataPk2 { get => GState.DataPk2; set => GState.DataPk2 = value; }
    public ReferenceManager ReferenceManager { get => GState.ReferenceManager; set => GState.ReferenceManager = value; }
    public Objects.Player Player { get => GState.Player; set => GState.Player = value; }
    public SpawnedBionic SelectedEntity { get => GState.SelectedEntity; set => GState.SelectedEntity = value; }
    public SpawnPacketInfo SpawnInfo { get => GState.SpawnInfo; set => GState.SpawnInfo = value; }
    public Packet ChunkedPacket { get => GState.ChunkedPacket; set => GState.ChunkedPacket = value; }
    public Party Party { get => GState.Party; set => GState.Party = value; }
    public bool Clientless { get => GState.Clientless; set => GState.Clientless = value; }
    public bool Started { get => GState.Started; set => GState.Started = value; }
    public bool Ready { get => GState.Ready; set => GState.Ready = value; }
    public GameClientType ClientType { get => GState.ClientType; set => GState.ClientType = value; }

    public void Start() => GState.Start();
    public bool InitializeArchiveFiles() => GState.InitializeArchiveFiles();
    public void Initialize() => GState.Initialize();
    public void ShowNotification(string message) => GState.ShowNotification(message);
}

public sealed class KernelContext
{
    public Proxy Proxy { get => KState.Proxy; set => KState.Proxy = value; }
    public Bot Bot { get => KState.Bot; set => KState.Bot = value; }
    public string Language { get => KState.Language; set => KState.Language = value; }
    public string LaunchMode { get => KState.LaunchMode; set => KState.LaunchMode = value; }
    public int TickCount => KState.TickCount;
    public string BasePath => KState.BasePath;
    public bool EnableCollisionDetection { get => KState.EnableCollisionDetection; set => KState.EnableCollisionDetection = value; }
    public bool Debug { get => KState.Debug; set => KState.Debug = value; }

    public void Initialize() => KState.Initialize();
    public void Shutdown() => KState.Shutdown();
}

public sealed class PacketManagerContext
{
    public int PendingCallbackCount => PMgr.PendingCallbackCount;

    public void RegisterHandler(IPacketHandler handler) => PMgr.RegisterHandler(handler);
    public void RemoveHandler(IPacketHandler handler) => PMgr.RemoveHandler(handler);
    public void RegisterHook(IPacketHook hook) => PMgr.RegisterHook(hook);
    public void RemoveHook(IPacketHook hook) => PMgr.RemoveHook(hook);
    public void RemoveCallback(AwaitCallback callback) => PMgr.RemoveCallback(callback);
    public void CallHandler(Packet packet, PacketDestination destination) => PMgr.CallHandler(packet, destination);
    public Packet CallHook(Packet packet, PacketDestination destination) => PMgr.CallHook(packet, destination);
    public void CallCallback(Packet packet) => PMgr.CallCallback(packet);
    public void SendPacket(Packet packet, PacketDestination destination) => PMgr.SendPacket(packet, destination);
    public void SendPacket(Packet packet, PacketDestination destination, params AwaitCallback[] callbacks) =>
        PMgr.SendPacket(packet, destination, callbacks);
    public void HandlePacket(Packet packet, PacketDestination destination) => PMgr.HandlePacket(packet, destination);
    public List<IPacketHandler> GetHandlers(ushort? opcode = null) => PMgr.GetHandlers(opcode);
    public List<IPacketHook> GetHooks(ushort? opcode = null) => PMgr.GetHooks(opcode);
}

public sealed class EventBusContext
{
    public void SubscribeEvent(string name, Delegate handler) => EMgr.SubscribeEvent(name, handler);
    public void SubscribeEvent(string name, System.Action handler) => EMgr.SubscribeEvent(name, handler);
    public void SubscribeEvent(string name, Delegate handler, object owner) => EMgr.SubscribeEvent(name, handler, owner);
    public void SubscribeEvent(string name, System.Action handler, object owner) => EMgr.SubscribeEvent(name, handler, owner);
    public void UnsubscribeEvent(string name, Delegate handler) => EMgr.UnsubscribeEvent(name, handler);
    public void UnsubscribeEvent(string name, System.Action handler) => EMgr.UnsubscribeEvent(name, handler);
    public void UnsubscribeOwner(object owner) => EMgr.UnsubscribeOwner(owner);
    public void FireEvent(string name, params object[] parameters) => EMgr.FireEvent(name, parameters);
    public int GetListenerCount() => EMgr.GetListenerCount();
    public int GetListenerCount(string eventName) => EMgr.GetListenerCount(eventName);
    public int GetOwnerCount() => EMgr.GetOwnerCount();
    public string[] GetEventNames() => EMgr.GetEventNames();
    public void ClearSubscribers() => EMgr.ClearSubscribers();
}

public sealed class ServiceRuntimeContext
{
    public IGameStateRuntimeContext GameState { get => SR.GameState; set => SR.GameState = value; }
    public IPacketDispatcher PacketDispatcher { get => SR.PacketDispatcher; set => SR.PacketDispatcher = value; }
    public IServiceRuntimeEnvironment Environment { get => SR.Environment; set => SR.Environment = value; }
    public IServiceLog Log { get => SR.Log; set => SR.Log = value; }
    public IPickupRuntime PickupRuntime { get => SR.PickupRuntime; set => SR.PickupRuntime = value; }
    public IPickupSettings PickupSettings { get => SR.PickupSettings; set => SR.PickupSettings = value; }
    public IPickupService Pickup { get => SR.Pickup; set => SR.Pickup = value; }
    public IInventoryRuntime InventoryRuntime { get => SR.InventoryRuntime; set => SR.InventoryRuntime = value; }
    public IShoppingRuntime ShoppingRuntime { get => SR.ShoppingRuntime; set => SR.ShoppingRuntime = value; }
    public IShoppingService Shopping { get => SR.Shopping; set => SR.Shopping = value; }
    public IAlchemyRuntime AlchemyRuntime { get => SR.AlchemyRuntime; set => SR.AlchemyRuntime = value; }
    public IAlchemyProgress AlchemyProgress { get => SR.AlchemyProgress; set => SR.AlchemyProgress = value; }
    public IAlchemyService Alchemy { get => SR.Alchemy; set => SR.Alchemy = value; }
    public IScriptRuntime ScriptRuntime { get => SR.ScriptRuntime; set => SR.ScriptRuntime = value; }
    public IScriptProgress ScriptProgress { get => SR.ScriptProgress; set => SR.ScriptProgress = value; }
    public ISpawnRuntime SpawnRuntime { get => SR.SpawnRuntime; set => SR.SpawnRuntime = value; }
    public ILanguageService Language { get => SR.Language; set => SR.Language = value; }
    public ISkillRuntime SkillRuntime { get => SR.SkillRuntime; set => SR.SkillRuntime = value; }
    public ISkillConfig SkillConfig { get => SR.SkillConfig; set => SR.SkillConfig = value; }
    public ISkillService Skill { get => SR.Skill; set => SR.Skill = value; }
    public IClientConnectionRuntime ClientConnectionRuntime { get => SR.ClientConnectionRuntime; set => SR.ClientConnectionRuntime = value; }
    public IClientlessService Clientless { get => SR.Clientless; set => SR.Clientless = value; }
    public IClientNativeRuntime ClientNativeRuntime { get => SR.ClientNativeRuntime; set => SR.ClientNativeRuntime = value; }
    public IClientLaunchConfigProvider ClientLaunchConfigProvider { get => SR.ClientLaunchConfigProvider; set => SR.ClientLaunchConfigProvider = value; }
    public IClientLaunchPolicy ClientLaunchPolicy { get => SR.ClientLaunchPolicy; set => SR.ClientLaunchPolicy = value; }
    public IProfileStorage ProfileStorage { get => SR.ProfileStorage; set => SR.ProfileStorage = value; }
    public IProfileService Profile { get => SR.Profile; set => SR.Profile = value; }
}
