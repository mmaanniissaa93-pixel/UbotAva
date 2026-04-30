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

namespace UBot.Core.Runtime;

public sealed class GameSession : IGameSession
{
    public static GameSession Shared { get; } = new();

    private bool _skillEventsRegistered;
    private bool _clientlessEventsRegistered;

    public GameClientType ClientType
    {
        get => Game.ClientType;
        set
        {
            Game.ClientType = value;
            GameClientTypeAccessor.ActiveClientType = value;
        }
    }

    public object Player => Game.Player;

    public object SelectedEntity
    {
        get => Game.SelectedEntity;
        set => Game.SelectedEntity = value as SpawnedBionic;
    }

    public object AcceptanceRequest => Game.AcceptanceRequest;
    public bool Started { get => Game.Started; set => Game.Started = value; }
    public bool Ready => Game.Ready;
    public bool Clientless { get => Game.Clientless; set => Game.Clientless = value; }
    public ushort Port => Game.Port;
    public IReferenceManager ReferenceManager => Game.ReferenceManager;

    public void Start()
    {
        Game.Started = false;

        if (Kernel.Bot?.Running == true)
            Kernel.Bot.Stop();

        Kernel.Proxy?.Shutdown();

        var divisionIndex = GlobalConfig.Get<int>("UBot.DivisionIndex");
        var serverIndex = GlobalConfig.Get<int>("UBot.GatewayIndex");

        Game.Port = NetworkUtilities.GetFreePort(33673, 39999, 1);

        Kernel.Proxy = new Proxy();
        Kernel.Proxy.Start(
            Game.Port,
            Game.ReferenceManager.DivisionInfo.Divisions[divisionIndex].GatewayServers[serverIndex],
            Game.ReferenceManager.GatewayInfo.Port
        );

        Game.Started = true;
    }

    public bool InitializeArchiveFiles()
    {
        var directory = GlobalConfig.Get<string>("UBot.SilkroadDirectory");
        var pk2Key = GlobalConfig.Get<string>("UBot.Pk2Key", "169841");

        try
        {
            Game.MediaPk2 = new PackFileSystem(Path.Combine(directory, "media.pk2"), pk2Key);
            Game.DataPk2 = new PackFileSystem(Path.Combine(directory, "data.pk2"), pk2Key);

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
        ClientType = GlobalConfig.GetEnum("UBot.Game.ClientType", GameClientType.Vietnam);
        Game.ReferenceManager = new ReferenceManager();
        Game.Party = new Party();

        SkillManager.Initialize(ServiceRuntime.Skill ?? new SkillService());
        RegisterSkillServiceEvents();
        ShoppingManager.Initialize();
        ClientlessManager.Initialize(ServiceRuntime.Clientless ?? new ClientlessService());
        RegisterClientlessServiceEvents();
        ProfileManager.Initialize(ServiceRuntime.Profile ?? new ProfileService());
        ClientManager.Initialize(ServiceRuntime.ClientLaunchPolicy ?? new ClientLaunchPolicyService());
        ScriptManager.Initialize();
    }

    public void ShowNotification(string message)
    {
        if (!Game.Ready)
            return;

        var chatPacket = new Packet(0x3026);
        chatPacket.WriteByte(ChatType.Notice);
        chatPacket.WriteConditonalString(message);

        PacketManager.SendPacket(chatPacket, PacketDestination.Client);
    }

    private void RegisterSkillServiceEvents()
    {
        if (_skillEventsRegistered)
            return;

        EventManager.SubscribeEvent("OnLoadGameData", new System.Action(SkillManager.ResetBaseSkills));
        EventManager.SubscribeEvent("OnCastSkill", new System.Action<uint>(SkillManager.NotifySkillCasted));
        _skillEventsRegistered = true;
    }

    private void RegisterClientlessServiceEvents()
    {
        if (_clientlessEventsRegistered)
            return;

        EventManager.SubscribeEvent("OnAgentServerDisconnected", new System.Action(ClientlessManager.OnAgentServerDisconnected));
        EventManager.SubscribeEvent("OnAgentServerConnected", new System.Action(ClientlessManager.OnAgentServerConnected));
        _clientlessEventsRegistered = true;
    }
}
