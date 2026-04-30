#nullable enable annotations

using System;
using System.IO;
using UBot.Core.Abstractions;
using UBot.Core.Client;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Extensions;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Party;
using UBot.Core.Objects.Spawn;
using UBot.Core.Services;
using UBot.FileSystem;
using GState = UBot.Core.Game;
using KState = UBot.Core.Kernel;
using GConfig = UBot.Core.GlobalConfig;
using PMgr = UBot.Core.Network.PacketManager;
using EMgr = UBot.Core.Event.EventManager;
using SR = UBot.Core.Services.ServiceRuntime;

namespace UBot.Core.Runtime;

public sealed class GameSession : IGameSession
{
    public static GameSession Shared { get; } = new();

    private bool _skillEventsRegistered;
    private bool _clientlessEventsRegistered;

    public GameClientType ClientType
    {
        get => GState.ClientType;
        set
        {
            GState.ClientType = value;
            GameClientTypeAccessor.ActiveClientType = value;
        }
    }

    public object Player => GState.Player;

    public object SelectedEntity
    {
        get => GState.SelectedEntity;
        set => GState.SelectedEntity = value as SpawnedBionic;
    }

    public object AcceptanceRequest => GState.AcceptanceRequest;
    public bool Started { get => GState.Started; set => GState.Started = value; }
    public bool Ready => GState.Ready;
    public bool Clientless { get => GState.Clientless; set => GState.Clientless = value; }
    public ushort Port => GState.Port;
    public IReferenceManager ReferenceManager => GState.ReferenceManager;

    public void Start()
    {
        GState.Started = false;

        if (KState.Bot?.Running == true)
            KState.Bot.Stop();

        KState.Proxy?.Shutdown();

        var divisionIndex = GConfig.Get<int>("UBot.DivisionIndex");
        var serverIndex = GConfig.Get<int>("UBot.GatewayIndex");

        GState.Port = NetworkUtilities.GetFreePort(33673, 39999, 1);

        KState.Proxy = new Proxy();
        KState.Proxy.Start(
            GState.Port,
            GState.ReferenceManager.DivisionInfo.Divisions[divisionIndex].GatewayServers[serverIndex],
            GState.ReferenceManager.GatewayInfo.Port
        );

        GState.Started = true;
    }

    public bool InitializeArchiveFiles()
    {
        var directory = GConfig.Get<string>("UBot.SilkroadDirectory");
        var pk2Key = GConfig.Get<string>("UBot.Pk2Key", "169841");

        try
        {
            GState.MediaPk2 = new PackFileSystem(Path.Combine(directory, "media.pk2"), pk2Key);
            GState.DataPk2 = new PackFileSystem(Path.Combine(directory, "data.pk2"), pk2Key);

            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex);

            return false;
        }
    }

    public void Initialize()
    {
        ClientType = GConfig.GetEnum("UBot.Game.ClientType", GameClientType.Vietnam);
        GState.ReferenceManager = new ReferenceManager();
        GState.Party = new Party();

        SkillManager.Initialize(SR.Skill ?? new SkillService());
        RegisterSkillServiceEvents();
        ShoppingManager.Initialize();
        ClientlessManager.Initialize(SR.Clientless ?? new ClientlessService());
        RegisterClientlessServiceEvents();
        ProfileManager.Initialize(SR.Profile ?? new ProfileService());
        ClientManager.Initialize(SR.ClientLaunchPolicy ?? new ClientLaunchPolicyService());
        ScriptManager.Initialize();
    }

    public void ShowNotification(string message)
    {
        if (!GState.Ready)
            return;

        var chatPacket = new Packet(0x3026);
        chatPacket.WriteByte(ChatType.Notice);
        chatPacket.WriteConditonalString(message);

        PMgr.SendPacket(chatPacket, PacketDestination.Client);
    }

    private void RegisterSkillServiceEvents()
    {
        if (_skillEventsRegistered)
            return;

        EMgr.SubscribeEvent("OnLoadGameData", new System.Action(SkillManager.ResetBaseSkills));
        EMgr.SubscribeEvent("OnCastSkill", new System.Action<uint>(SkillManager.NotifySkillCasted));
        _skillEventsRegistered = true;
    }

    private void RegisterClientlessServiceEvents()
    {
        if (_clientlessEventsRegistered)
            return;

        EMgr.SubscribeEvent("OnAgentServerDisconnected", new System.Action(ClientlessManager.OnAgentServerDisconnected));
        EMgr.SubscribeEvent("OnAgentServerConnected", new System.Action(ClientlessManager.OnAgentServerConnected));
        _clientlessEventsRegistered = true;
    }
}
