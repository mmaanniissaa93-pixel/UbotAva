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
    private static bool _referenceLoading;
    private static bool _referenceLoaded;
    private static string _statusText = "Ready";
    private static bool _eventsSubscribed;

    private static event Action<string, string>? GlobalLogReceived;
    private static event Action<string, string, string>? GlobalChatMessageReceived;

    internal bool ReferenceLoading => _referenceLoading;
    internal bool ReferenceLoaded => _referenceLoaded;
    internal string StatusText => _statusText;

    internal void MarkReferenceDataDirty()
    {
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
            GlobalConfig.Load();

            var selectedCharacter = ProfileManager.SelectedCharacter?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(selectedCharacter))
                selectedCharacter = GlobalConfig.Get("UBot.General.AutoLoginCharacter", string.Empty)?.Trim() ?? string.Empty;

            if (!string.IsNullOrWhiteSpace(selectedCharacter))
            {
                ProfileManager.SelectedCharacter = selectedCharacter;
                PlayerConfig.Load(selectedCharacter);
            }

            Kernel.Language = GlobalConfig.Get("UBot.Language", "en_US");
            Kernel.Initialize();
            Game.Initialize();

            ExtensionManager.LoadAssemblies<IPlugin>();
            ExtensionManager.LoadAssemblies<IBotbase>();

            InitializeEnabledPlugins();
            SelectConfiguredBotbase();
            CommandManager.Initialize();

            if (!_eventsSubscribed)
            {
                _eventsSubscribed = true;
                EventManager.SubscribeEvent("OnAddLog", new Action<string, LogLevel>(OnLog));
                EventManager.SubscribeEvent("OnChangeStatusText", new Action<string>(status => _statusText = status ?? string.Empty));
                EventManager.SubscribeEvent("OnChatMessage", new Action<string, string, ChatType>(OnChatMessage));
                EventManager.SubscribeEvent("OnUniqueMessage", new Action<string>(OnUniqueMessage));
            }

            BeginReferenceDataLoad();
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
                Log.Error($"Plugin [{plugin.Name}] failed to initialize: {ex.Message}");
            }
        }
    }

    private static void SelectConfiguredBotbase()
    {
        var configuredName = GlobalConfig.Get("UBot.BotName", "UBot.Training");
        if (TrySetBotbase(configuredName))
            return;

        var fallback = ExtensionManager.Bots.FirstOrDefault();
        if (fallback != null)
            TrySetBotbase(fallback.Name);
    }

    private static bool TrySetBotbase(string botbaseName)
    {
        var botbase = ExtensionManager.Bots.FirstOrDefault(bot => PluginIdEquals(bot.Name, botbaseName));
        if (botbase == null || Kernel.Bot == null)
            return false;

        Kernel.Bot.SetBotbase(botbase);
        GlobalConfig.Set("UBot.BotName", botbase.Name);
        return true;
    }

    internal void BeginReferenceDataLoad()
    {
        if (_referenceLoading || _referenceLoaded)
            return;

        var sroDir = GlobalConfig.Get("UBot.SilkroadDirectory", string.Empty);
        if (string.IsNullOrWhiteSpace(sroDir) || !File.Exists(Path.Combine(sroDir, "media.pk2")))
            return;
        if (!Game.InitializeArchiveFiles())
            return;

        _referenceLoading = true;
        _ = Task.Run(() =>
        {
            try
            {
                var worker = new BackgroundWorker { WorkerReportsProgress = true };
                Game.ReferenceManager.Load(GlobalConfig.Get("UBot.TranslationIndex", 9), worker);
                _referenceLoaded = true;
            }
            catch (Exception ex)
            {
                Log.Error($"Reference load failed: {ex.Message}");
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

