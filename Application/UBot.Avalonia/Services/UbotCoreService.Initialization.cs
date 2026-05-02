using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UBot.FileSystem;
using UBot.NavMeshApi;
using UBot.NavMeshApi.Dungeon;
using UBot.NavMeshApi.Edges;
using UBot.NavMeshApi.Extensions;
using UBot.NavMeshApi.Terrain;
using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Extensions;
using UBot.Core.Network;
using UBot.Core.Network.Protocol;
using UBot.Core.Objects;
using UBot.Core.Objects.Party;
using UBot.Core.Objects.Spawn;
using UBot.Core.Objects.Skill;
using UBot.Core.Plugins;
using Forms = System.Windows.Forms;
using CoreRegion = UBot.Core.Objects.Region;

namespace UBot.Avalonia.Services;

internal sealed class UbotCoreLifecycleService : UbotServiceBase
{
    private static readonly object InitLock = new();
    private static bool _initialized;
    private static bool _clientInfoLoaded;
    private static bool _referenceLoading;
    private static bool _referenceLoaded;
    private static string _statusText = "Ready";
    private static bool _eventsSubscribed;

    private static event Action<string, string>? GlobalLogReceived;
    private static event Action<string, string, string>? GlobalChatMessageReceived;

    internal bool ReferenceLoading => _referenceLoading;
    internal bool ReferenceLoaded => _referenceLoaded;
    internal bool ClientInfoLoaded => _clientInfoLoaded;
    internal string StatusText => _statusText;

    internal void MarkReferenceDataDirty()
    {
        _clientInfoLoaded = false;
        _referenceLoaded = false;
    }

    internal void AddLogListener(Action<string, string> listener)
    {
        GlobalLogReceived += listener;
    }

    internal void RemoveLogListener(Action<string, string> listener)
    {
        GlobalLogReceived -= listener;
    }

    internal void AddChatListener(Action<string, string, string> listener)
    {
        GlobalChatMessageReceived += listener;
    }

    internal void RemoveChatListener(Action<string, string, string> listener)
    {
        GlobalChatMessageReceived -= listener;
    }

    internal void EnsureInitialized()
    {
        lock (InitLock)
        {
            if (_initialized)
                return;

            EnsureProfileLoaded();
            UBot.Core.RuntimeAccess.Global.Load();

            var selectedCharacter = ProfileManager.SelectedCharacter?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(selectedCharacter))
                selectedCharacter = UBot.Core.RuntimeAccess.Global.Get("UBot.General.AutoLoginCharacter", string.Empty)?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(selectedCharacter))
            {
                ProfileManager.SelectedCharacter = selectedCharacter;
                UBot.Core.RuntimeAccess.Player.Load(selectedCharacter);
            }

            var core = UBot.Core.RuntimeAccess.Core;
            var session = UBot.Core.RuntimeAccess.Session;
            if (core != null)
            {
                core.Language = UBot.Core.RuntimeAccess.Global.Get("UBot.Language", "en_US");
                core.Initialize();
            }
            session?.Initialize();

            ExtensionManager.LoadAssemblies<IPlugin>();
            ExtensionManager.LoadAssemblies<IBotbase>();

            InitializeEnabledPlugins();
            SelectConfiguredBotbase();
            CommandManager.Initialize();

            if (!_eventsSubscribed)
            {
                _eventsSubscribed = true;
                var events = UBot.Core.RuntimeAccess.Events;
                events?.SubscribeEvent("OnAddLog", new Action<string, LogLevel>(OnLog));
                events?.SubscribeEvent("OnChangeStatusText", new Action<string>(status => _statusText = status ?? string.Empty));
                events?.SubscribeEvent("OnChatMessage", new Action<string, string, ChatType>(OnChatMessage));
                events?.SubscribeEvent("OnUniqueMessage", new Action<string>(OnUniqueMessage));
            }

            // Keep idle footprint low: load lightweight connection metadata only.
            EnsureClientInfoLoaded();
            _initialized = true;
        }
    }


    private static void EnsureProfileLoaded()
    {
        if (!ProfileManager.Any())
            ProfileManager.Add("Default");
        if (!ProfileManager.ProfileExists(ProfileManager.SelectedProfile))
            ProfileManager.Add("Default");
    }

    private static void InitializeEnabledPlugins()
    {
        foreach (var plugin in ExtensionManager.Plugins.Where(plugin => plugin.Enabled))
        {
            try
            {
                ExtensionManager.InitializeExtension(plugin);
            }
            catch (Exception ex)
            {
                Log.Error($"Plugin [{plugin.Name ?? "unknown"}] failed to initialize", ex);
            }
        }
    }

    private static void SelectConfiguredBotbase()
    {
        var configuredName = UBot.Core.RuntimeAccess.Global.Get("UBot.BotName", "UBot.Training");
        if (TrySetBotbase(configuredName))
            return;

        var fallback = ExtensionManager.Bots.FirstOrDefault();
        if (fallback != null)
            TrySetBotbase(fallback.Name);
    }

    private static bool TrySetBotbase(string botbaseName)
    {
        var botbase = ExtensionManager.Bots.FirstOrDefault(bot => PluginIdEquals(bot.Name, botbaseName));
        if (botbase == null || UBot.Core.RuntimeAccess.Core.Bot == null)
            return false;

        UBot.Core.RuntimeAccess.Core.Bot.SetBotbase(botbase);
        UBot.Core.RuntimeAccess.Global.Set("UBot.BotName", botbase.Name);
        return true;
    }

    internal bool EnsureClientInfoLoaded()
    {
        lock (InitLock)
        {
            if (_clientInfoLoaded
                && UBot.Core.RuntimeAccess.Session.ReferenceManager?.DivisionInfo != null
                && UBot.Core.RuntimeAccess.Session.ReferenceManager.GatewayInfo != null
                && UBot.Core.RuntimeAccess.Session.ReferenceManager.VersionInfo != null)
            {
                return true;
            }

            var sroDir = UBot.Core.RuntimeAccess.Global.Get("UBot.SilkroadDirectory", string.Empty);
            if (string.IsNullOrWhiteSpace(sroDir) || !File.Exists(Path.Combine(sroDir, "media.pk2")))
                return false;

            if (!UBot.Core.RuntimeAccess.Session.InitializeArchiveFiles())
                return false;

            try
            {
                UBot.Core.RuntimeAccess.Session.ReferenceManager.LoadClientInfo();
                _clientInfoLoaded = UBot.Core.RuntimeAccess.Session.ReferenceManager?.DivisionInfo != null
                                    && UBot.Core.RuntimeAccess.Session.ReferenceManager.GatewayInfo != null
                                    && UBot.Core.RuntimeAccess.Session.ReferenceManager.VersionInfo != null;
                return _clientInfoLoaded;
            }
            catch (Exception ex)
            {
                Log.Error("Client info load failed", ex);
                _clientInfoLoaded = false;
                return false;
            }
        }
    }

    internal void BeginReferenceDataLoad()
    {
        if (_referenceLoading || _referenceLoaded)
            return;

        if (!EnsureClientInfoLoaded())
            return;

        _referenceLoading = true;
        _ = Task.Run(() =>
        {
            try
            {
                var worker = new BackgroundWorker { WorkerReportsProgress = true };
                var refManager = UBot.Core.RuntimeAccess.Session?.ReferenceManager;
                refManager?.Load(UBot.Core.RuntimeAccess.Global.Get("UBot.TranslationIndex", 9), worker);
                _referenceLoaded = refManager != null;
            }
            catch (Exception ex)
            {
                Log.Error("Reference load failed", ex);
            }
            finally
            {
                _referenceLoading = false;
            }
        });
    }

    private static void OnLog(string message, LogLevel level)
    {
        GlobalLogReceived?.Invoke(level.ToString().ToLowerInvariant(), message);
    }

    private static void OnChatMessage(string sender, string message, ChatType type)
    {
        var channel = type switch
        {
            ChatType.Private => "private",
            ChatType.Party => "party",
            ChatType.Guild => "guild",
            ChatType.Global => "global",
            ChatType.Notice => "global",
            ChatType.Stall => "stall",
            _ => "all"
        };

        GlobalChatMessageReceived?.Invoke(channel, sender ?? string.Empty, message ?? string.Empty);
    }

    private static void OnUniqueMessage(string message)
    {
        GlobalChatMessageReceived?.Invoke("unique", "Unique", message ?? string.Empty);
    }
}

