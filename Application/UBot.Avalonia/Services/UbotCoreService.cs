using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Extensions;
using UBot.Core.Network;
using UBot.Core.Network.Protocol;
using UBot.Core.Objects;
using UBot.Core.Plugins;
using Forms = System.Windows.Forms;

namespace UBot.Avalonia.Services;

public sealed class UbotCoreService : IUbotCoreService
{
    private const string ConnectionModeKey = "UBot.Desktop.ConnectionMode";
    private const string MapPluginName = "UBot.Map";
    private const string QuestPluginAlias = "UBot.Quest";
    private const string QuestRuntimePlugin = "UBot.QuestLog";
    private const double MapClickMaxStepDistance = 145.0;

    private static readonly object InitLock = new();
    private static bool _initialized;
    private static bool _referenceLoading;
    private static bool _referenceLoaded;
    private static bool _clientVisible = true;
    private static string _statusText = "Ready";
    private static bool _eventsSubscribed;
    private static readonly JsonSerializerOptions AutoLoginReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static event Action<string, string>? GlobalLogReceived;

    public event Action<string, string>? LogReceived
    {
        add => GlobalLogReceived += value;
        remove => GlobalLogReceived -= value;
    }

    public UbotCoreService()
    {
        EnsureInitialized();
    }

    public Task<RuntimeStatus> GetStatusAsync()
    {
        return Task.FromResult(BuildStatusSnapshot());
    }

    public Task<ConnectionOptions> GetConnectionOptionsAsync()
    {
        var options = ResolveConnectionOptions();
        var normalized = NormalizeConnectionIndices(options.mode, options.divisionIndex, options.gatewayIndex);

        return Task.FromResult(new ConnectionOptions
        {
            Mode = normalized.mode,
            DivisionIndex = normalized.divisionIndex,
            GatewayIndex = normalized.gatewayIndex,
            Divisions = BuildDivisionOptions(),
            ClientType = GlobalConfig.GetEnum("UBot.Game.ClientType", Game.ClientType).ToString(),
            ClientTypes = BuildClientTypeOptions(),
            ReferenceLoading = _referenceLoading,
            ReferenceLoaded = _referenceLoaded
        });
    }

    public async Task<ConnectionOptions> SetConnectionOptionsAsync(int divisionIndex, int gatewayIndex, string? mode = null, string? clientType = null)
    {
        var options = ResolveConnectionOptions();
        var changed = false;
        var reloadReferenceData = false;

        if (!string.IsNullOrWhiteSpace(mode))
        {
            var normalizedMode = NormalizeConnectionMode(mode);
            if (!string.Equals(options.mode, normalizedMode, StringComparison.OrdinalIgnoreCase))
            {
                options.mode = normalizedMode;
                GlobalConfig.Set(ConnectionModeKey, normalizedMode);
                changed = true;
            }
        }

        if (divisionIndex < 0)
            divisionIndex = 0;
        if (gatewayIndex < 0)
            gatewayIndex = 0;

        if (divisionIndex != options.divisionIndex)
        {
            options.divisionIndex = divisionIndex;
            options.gatewayIndex = 0;
            changed = true;
        }

        if (gatewayIndex != options.gatewayIndex)
        {
            options.gatewayIndex = gatewayIndex;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(clientType)
            && Enum.TryParse<GameClientType>(clientType, true, out var requestedClientType))
        {
            var currentClientType = GlobalConfig.GetEnum("UBot.Game.ClientType", Game.ClientType);
            if (requestedClientType != currentClientType && !Game.Ready && !_referenceLoading)
            {
                GlobalConfig.Set("UBot.Game.ClientType", requestedClientType);
                Game.ClientType = requestedClientType;
                _referenceLoaded = false;
                reloadReferenceData = true;
                changed = true;
            }
        }

        var normalized = NormalizeConnectionIndices(options.mode, options.divisionIndex, options.gatewayIndex);
        GlobalConfig.Set(ConnectionModeKey, normalized.mode);
        GlobalConfig.Set("UBot.DivisionIndex", normalized.divisionIndex);
        GlobalConfig.Set("UBot.GatewayIndex", normalized.gatewayIndex);
        if (changed)
            GlobalConfig.Save();

        if (reloadReferenceData && !_referenceLoading)
            BeginReferenceDataLoad();

        return await GetConnectionOptionsAsync();
    }

    public Task<IReadOnlyList<PluginDescriptor>> GetPluginsAsync()
    {
        var modules = ExtensionManager.Plugins
            .OrderBy(plugin => plugin.Index)
            .Select(plugin => new PluginDescriptor
            {
                Id = plugin.Name,
                Title = plugin.Title,
                Enabled = plugin.Enabled,
                DisplayAsTab = plugin.DisplayAsTab,
                Index = plugin.Index,
                IconKey = ResolveModuleKey(plugin.Name)
            })
            .ToList();

        var allBotbases = ExtensionManager.Bots.ToList();
        var trainingBotbase = allBotbases.FirstOrDefault(IsTrainingBotbase)
            ?? Kernel.Bot?.Botbase
            ?? allBotbases.FirstOrDefault();

        var insertionIndex = Math.Min(1, modules.Count);
        if (trainingBotbase != null && modules.All(x => !PluginIdEquals(x.Id, trainingBotbase.Name)))
        {
            modules.Insert(insertionIndex, new PluginDescriptor
            {
                Id = trainingBotbase.Name,
                Title = trainingBotbase.Title,
                Enabled = true,
                DisplayAsTab = true,
                Index = 1,
                IconKey = ResolveModuleKey(trainingBotbase.Name)
            });
            insertionIndex++;
        }

        foreach (var botbase in allBotbases)
        {
            if (trainingBotbase != null && PluginIdEquals(trainingBotbase.Name, botbase.Name))
                continue;
            if (modules.Any(x => PluginIdEquals(x.Id, botbase.Name)))
                continue;

            modules.Insert(insertionIndex, new PluginDescriptor
            {
                Id = botbase.Name,
                Title = botbase.Title,
                Enabled = true,
                DisplayAsTab = true,
                Index = insertionIndex,
                IconKey = ResolveModuleKey(botbase.Name)
            });
            insertionIndex++;
        }

        return Task.FromResult((IReadOnlyList<PluginDescriptor>)modules);
    }

    public Task<PluginStateDto> GetPluginStateAsync(string pluginId)
    {
        var state = new Dictionary<string, object?>
        {
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["botRunning"] = Kernel.Bot != null && Kernel.Bot.Running,
            ["statusText"] = _statusText,
            ["player"] = BuildStatusSnapshot().Player
        };

        var dto = new PluginStateDto
        {
            Id = pluginId ?? string.Empty,
            Enabled = ResolveEnabledState(pluginId),
            State = ToJsonElement(state)
        };

        return Task.FromResult(dto);
    }

    public Task<Dictionary<string, object?>> GetPluginConfigAsync(string pluginId)
    {
        if (TryResolveBotbase(pluginId, out var botbase))
        {
            if (IsTrainingBotbase(botbase))
                return Task.FromResult(BuildTrainingBotbaseConfig());
        }

        if (TryResolvePlugin(pluginId, out var plugin))
        {
            if (IsGeneralPlugin(plugin))
                return Task.FromResult(BuildGeneralPluginConfig());
            if (IsProtectionPlugin(plugin))
                return Task.FromResult(BuildProtectionPluginConfig());
            if (IsMapPlugin(plugin))
                return Task.FromResult(BuildMapPluginConfig());
        }

        var loaded = LoadPluginJsonConfig(pluginId);
        return Task.FromResult(loaded);
    }

    public Task<bool> SetPluginConfigAsync(string pluginId, Dictionary<string, object?> patch)
    {
        if (patch == null || patch.Count == 0)
            return Task.FromResult(false);

        var changed = false;
        if (TryResolveBotbase(pluginId, out var botbase))
        {
            if (IsTrainingBotbase(botbase))
                changed = ApplyTrainingBotbasePatch(patch);
        }
        else if (TryResolvePlugin(pluginId, out var plugin))
        {
            if (IsGeneralPlugin(plugin))
                changed = ApplyGeneralPluginPatch(patch);
            else if (IsProtectionPlugin(plugin))
                changed = ApplyProtectionPluginPatch(patch);
            else if (IsMapPlugin(plugin))
                changed = ApplyMapPluginPatch(patch);
            else
                changed = ApplyGenericPluginPatch(plugin.Name, patch);
        }
        else
        {
            changed = ApplyGenericPluginPatch(pluginId, patch);
        }

        if (changed)
        {
            GlobalConfig.Save();
            PlayerConfig.Save();
        }

        return Task.FromResult(changed);
    }
    public async Task<bool> InvokePluginActionAsync(string pluginId, string action, Dictionary<string, object?>? payload = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            return false;

        payload ??= new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var normalized = action.Trim().ToLowerInvariant();

        if (normalized == "general.start-client")
            return await StartClientAsync();
        if (normalized == "general.start-clientless")
            return await GoClientlessAsync();
        if (normalized == "core.disconnect")
        {
            await DisconnectAsync();
            return true;
        }
        if (normalized == "core.save-config")
        {
            await SaveConfigAsync();
            return true;
        }

        if (normalized is "general.toggle-client-visibility")
            return await ToggleClientVisibilityAsync();

        if (normalized is "general.toggle-pending-window")
            return TogglePendingWindow();

        if (normalized is "general.open-account-setup" or "general.open-accounts-window")
            return OpenAccountsWindow();

        if (normalized is "general.open-sound-settings")
        {
            EventManager.FireEvent("OnOpenSoundSettings");
            return true;
        }

        if (normalized is "protection.apply-stat-points")
        {
            EventManager.FireEvent("OnApplyStatPoints");
            return true;
        }

        if (normalized is "statistics.reset")
        {
            EventManager.FireEvent("OnResetStatistics");
            return true;
        }

        if (normalized is "chat.send")
            return HandleChatSendAction(payload);

        if (normalized is "log.clear")
            return true;

        if (normalized is "map.walk-to" or "map.walk" or "map.goto")
            return HandleMapWalkToAction(payload);

        if (normalized is "map.reset-to-player" or "map.center-on-player" or "map.navmesh.reset-to-player")
        {
            PlayerConfig.Set("UBot.Desktop.Map.ResetToPlayerAt", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            PlayerConfig.Save();
            return true;
        }

        if (normalized is "training.set-area-current" or "training.use-current-position")
            return HandleSetTrainingAreaToCurrentPosition();

        return false;
    }

    public Task<RuntimeStatus> StartBotAsync()
    {
        if (Kernel.Bot?.Botbase == null)
            return Task.FromResult(BuildStatusSnapshot());

        if (Kernel.Proxy == null || !Kernel.Proxy.IsConnectedToAgentserver)
            return Task.FromResult(BuildStatusSnapshot());

        if (Game.Player == null)
            return Task.FromResult(BuildStatusSnapshot());

        if (!Kernel.Bot.Running)
            Kernel.Bot.Start();

        return Task.FromResult(BuildStatusSnapshot());
    }

    public Task<RuntimeStatus> StopBotAsync()
    {
        if (Kernel.Bot != null && Kernel.Bot.Running)
            Kernel.Bot.Stop();

        return Task.FromResult(BuildStatusSnapshot());
    }

    public Task<RuntimeStatus> DisconnectAsync()
    {
        if (Kernel.Bot != null && Kernel.Bot.Running)
            Kernel.Bot.Stop();

        try
        {
            Kernel.Proxy?.Shutdown();
        }
        catch
        {
            // ignored
        }

        try
        {
            ClientManager.Kill();
            _clientVisible = false;
        }
        catch
        {
            // ignored
        }

        Game.Started = false;
        return Task.FromResult(BuildStatusSnapshot());
    }

    public Task SaveConfigAsync()
    {
        GlobalConfig.Save();
        PlayerConfig.Save();
        return Task.CompletedTask;
    }

    public Task<bool> StartClientAsync()
    {
        return ConnectCoreAsync("client");
    }

    public Task<bool> GoClientlessAsync()
    {
        if (Game.Clientless)
            return Task.FromResult(true);

        if (!Game.Started || Kernel.Proxy == null || !Kernel.Proxy.IsConnectedToAgentserver)
            return Task.FromResult(false);

        GlobalConfig.Set("UBot.General.StayConnected", true);
        GlobalConfig.Save();

        ClientlessManager.GoClientless();
        try
        {
            ClientManager.Kill();
        }
        catch
        {
            // ignored
        }

        _clientVisible = false;
        GlobalConfig.Set(ConnectionModeKey, "clientless");
        GlobalConfig.Save();
        return Task.FromResult(true);
    }

    public Task<bool> ToggleClientVisibilityAsync()
    {
        _clientVisible = !_clientVisible;
        ClientManager.SetVisible(_clientVisible);
        return Task.FromResult(true);
    }

    public Task<bool> SetSroExecutableAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult(false);

        var cleanedPath = path.Trim().Trim('"');
        if (!File.Exists(cleanedPath))
            return Task.FromResult(false);

        var directory = Path.GetDirectoryName(cleanedPath);
        var executable = Path.GetFileName(cleanedPath);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(executable))
            return Task.FromResult(false);

        GlobalConfig.Set("UBot.SilkroadDirectory", directory);
        GlobalConfig.Set("UBot.SilkroadExecutable", executable);
        GlobalConfig.Save();

        _referenceLoaded = false;
        if (!_referenceLoading)
            BeginReferenceDataLoad();

        return Task.FromResult(true);
    }

    public Task<string> GetSroExecutablePathAsync()
    {
        var directory = GlobalConfig.Get("UBot.SilkroadDirectory", string.Empty) ?? string.Empty;
        var executable = GlobalConfig.Get("UBot.SilkroadExecutable", string.Empty) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(executable))
            return Task.FromResult(string.Empty);

        return Task.FromResult(Path.Combine(directory, executable));
    }

    public Task<IReadOnlyList<AutoLoginAccountDto>> GetAutoLoginAccountsAsync()
    {
        var accounts = LoadAutoLoginAccountsFromFile()
            .OrderBy(x => x.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult((IReadOnlyList<AutoLoginAccountDto>)accounts);
    }

    public Task<bool> SaveAutoLoginAccountsAsync(IReadOnlyList<AutoLoginAccountDto> accounts)
    {
        try
        {
            var sanitized = (accounts ?? Array.Empty<AutoLoginAccountDto>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Username))
                .GroupBy(x => x.Username.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var item = group.Last();
                    return new AutoLoginAccountDto
                    {
                        Username = item.Username.Trim(),
                        Password = item.Password ?? string.Empty,
                        SecondaryPassword = item.SecondaryPassword ?? string.Empty,
                        Channel = item.Channel == 0 ? (byte)1 : item.Channel,
                        Type = string.IsNullOrWhiteSpace(item.Type) ? "Joymax" : item.Type,
                        ServerName = item.ServerName?.Trim() ?? string.Empty,
                        SelectedCharacter = item.SelectedCharacter?.Trim() ?? string.Empty,
                        Characters = (item.Characters ?? new List<string>())
                            .Where(name => !string.IsNullOrWhiteSpace(name))
                            .Select(name => name.Trim())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList()
                    };
                })
                .OrderBy(x => x.Username, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!WriteAutoLoginAccountsToFile(sanitized))
                return Task.FromResult(false);

            ReloadGeneralAccountsRuntime();
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task<string> PickExecutableAsync()
    {
        using var dialog = new Forms.OpenFileDialog
        {
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            Title = "Select Silkroad executable"
        };

        var result = dialog.ShowDialog();
        return Task.FromResult(result == Forms.DialogResult.OK ? dialog.FileName : string.Empty);
    }

    public Task<string> PickScriptFileAsync()
    {
        using var dialog = new Forms.OpenFileDialog
        {
            Filter = "Script files (*.txt;*.script)|*.txt;*.script|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            Title = "Select script file"
        };

        var result = dialog.ShowDialog();
        return Task.FromResult(result == Forms.DialogResult.OK ? dialog.FileName : string.Empty);
    }

    private static void EnsureInitialized()
    {
        lock (InitLock)
        {
            if (_initialized)
                return;

            EnsureProfileLoaded();
            GlobalConfig.Load();

            if (!string.IsNullOrWhiteSpace(ProfileManager.SelectedCharacter))
                PlayerConfig.Load(ProfileManager.SelectedCharacter);

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
            }

            BeginReferenceDataLoad();
            _initialized = true;
        }
    }

    private static async Task<bool> ConnectCoreAsync(string mode)
    {
        var requestedMode = NormalizeConnectionMode(mode);
        GlobalConfig.Set(ConnectionModeKey, requestedMode);

        var options = ResolveConnectionOptions();
        var normalized = NormalizeConnectionIndices(requestedMode, options.divisionIndex, options.gatewayIndex);

        GlobalConfig.Set("UBot.DivisionIndex", normalized.divisionIndex);
        GlobalConfig.Set("UBot.GatewayIndex", normalized.gatewayIndex);
        GlobalConfig.Save();

        if (Kernel.Bot != null && Kernel.Bot.Running)
            Kernel.Bot.Stop();

        Kernel.Proxy?.Shutdown();
        Game.Clientless = requestedMode == "clientless";

        if (!_referenceLoaded)
        {
            if (!_referenceLoading)
                BeginReferenceDataLoad();
            return false;
        }

        if (Game.ReferenceManager?.DivisionInfo?.Divisions == null
            || Game.ReferenceManager.DivisionInfo.Divisions.Count == 0
            || Game.ReferenceManager.GatewayInfo == null)
        {
            return false;
        }

        try
        {
            Game.Start();
            if (!Game.Clientless)
            {
                return await ClientManager.Start();
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(1200).ConfigureAwait(false);
                ClientlessManager.RequestServerList();
            });
            return true;
        }
        catch
        {
            return false;
        }
    }
    private static RuntimeStatus BuildStatusSnapshot()
    {
        var normalized = NormalizeConnectionIndices(
            NormalizeConnectionMode(GlobalConfig.Get(ConnectionModeKey, "clientless")),
            GlobalConfig.Get("UBot.DivisionIndex", 0),
            GlobalConfig.Get("UBot.GatewayIndex", 0));

        return new RuntimeStatus
        {
            BotRunning = Kernel.Bot != null && Kernel.Bot.Running,
            Profile = ProfileManager.SelectedProfile,
            Server = ResolveServerName(normalized.divisionIndex, normalized.gatewayIndex),
            Character = ResolveCharacterName(),
            StatusText = _statusText,
            ClientReady = Game.Ready,
            ClientStarted = Game.Started,
            ClientConnected = Kernel.Proxy != null && Kernel.Proxy.ClientConnected,
            GatewayConnected = Kernel.Proxy != null && Kernel.Proxy.IsConnectedToGatewayserver,
            AgentConnected = Kernel.Proxy != null && Kernel.Proxy.IsConnectedToAgentserver,
            ReferenceLoading = _referenceLoading,
            ReferenceLoaded = _referenceLoaded,
            SelectedBotbase = Kernel.Bot?.Botbase?.Name,
            ConnectionMode = normalized.mode,
            DivisionIndex = normalized.divisionIndex,
            GatewayIndex = normalized.gatewayIndex,
            Player = BuildPlayerSummary()
        };
    }

    private static PlayerStats BuildPlayerSummary()
    {
        var player = Game.Player;
        if (player == null)
            return new PlayerStats();

        var maxHealth = Math.Max(0, player.MaximumHealth);
        var maxMana = Math.Max(0, player.MaximumMana);
        var healthPercent = maxHealth > 0 ? Math.Clamp(player.Health * 100.0 / maxHealth, 0, 100) : 0;
        var manaPercent = maxMana > 0 ? Math.Clamp(player.Mana * 100.0 / maxMana, 0, 100) : 0;

        var expPercent = 0d;
        var refLevel = Game.ReferenceManager?.GetRefLevel(player.Level);
        if (refLevel != null && refLevel.Exp_C > 0)
            expPercent = Math.Clamp(player.Experience * 100.0 / refLevel.Exp_C, 0, 100);

        return new PlayerStats
        {
            Name = player.Name,
            Level = player.Level,
            Health = player.Health,
            MaxHealth = maxHealth,
            HealthPercent = Math.Round(healthPercent, 2),
            Mana = player.Mana,
            MaxMana = maxMana,
            ManaPercent = Math.Round(manaPercent, 2),
            ExperiencePercent = Math.Round(expPercent, 2),
            Gold = (long)player.Gold,
            SkillPoints = (int)player.SkillPoints,
            StatPoints = (int)player.StatPoints,
            InCombat = player.InCombat,
            XOffset = Math.Round(player.Position.XOffset, 2),
            YOffset = Math.Round(player.Position.YOffset, 2)
        };
    }

    private static List<ConnectionDivisionDto> BuildDivisionOptions()
    {
        var divisionInfo = Game.ReferenceManager?.DivisionInfo;
        if (divisionInfo?.Divisions == null)
            return new List<ConnectionDivisionDto>();

        var result = new List<ConnectionDivisionDto>(divisionInfo.Divisions.Count);
        for (var i = 0; i < divisionInfo.Divisions.Count; i++)
        {
            var division = divisionInfo.Divisions[i];
            var dto = new ConnectionDivisionDto { Index = i, Name = division.Name };
            for (var j = 0; j < (division.GatewayServers?.Count ?? 0); j++)
            {
                dto.Servers.Add(new ConnectionServerDto
                {
                    Index = j,
                    Name = division.GatewayServers[j]
                });
            }

            result.Add(dto);
        }

        return result;
    }

    private static List<ConnectionClientTypeDto> BuildClientTypeOptions()
    {
        return Enum.GetValues<GameClientType>()
            .Select(value => new ConnectionClientTypeDto
            {
                Id = value.ToString(),
                Name = value.ToString(),
                Value = (int)value
            })
            .ToList();
    }

    private static (string mode, int divisionIndex, int gatewayIndex) ResolveConnectionOptions()
    {
        var mode = NormalizeConnectionMode(GlobalConfig.Get(ConnectionModeKey, "clientless"));
        var divisionIndex = GlobalConfig.Get("UBot.DivisionIndex", 0);
        var gatewayIndex = GlobalConfig.Get("UBot.GatewayIndex", 0);
        return (mode, divisionIndex, gatewayIndex);
    }

    private static (string mode, int divisionIndex, int gatewayIndex) NormalizeConnectionIndices(string mode, int divisionIndex, int gatewayIndex)
    {
        var normalizedMode = NormalizeConnectionMode(mode);
        var divisions = Game.ReferenceManager?.DivisionInfo?.Divisions;
        if (divisions == null || divisions.Count == 0)
            return (normalizedMode, Math.Max(divisionIndex, 0), Math.Max(gatewayIndex, 0));

        if (divisionIndex < 0)
            divisionIndex = 0;
        if (divisionIndex >= divisions.Count)
            divisionIndex = divisions.Count - 1;

        var servers = divisions[divisionIndex].GatewayServers;
        if (servers == null || servers.Count == 0)
            return (normalizedMode, divisionIndex, 0);

        if (gatewayIndex < 0)
            gatewayIndex = 0;
        if (gatewayIndex >= servers.Count)
            gatewayIndex = servers.Count - 1;

        return (normalizedMode, divisionIndex, gatewayIndex);
    }

    private static string NormalizeConnectionMode(string? mode)
    {
        if (string.Equals(mode, "client", StringComparison.OrdinalIgnoreCase))
            return "client";
        return "clientless";
    }

    private static string ResolveCharacterName()
    {
        if (Game.Player != null && !string.IsNullOrWhiteSpace(Game.Player.Name))
            return Game.Player.Name;
        if (!string.IsNullOrWhiteSpace(ProfileManager.SelectedCharacter))
            return ProfileManager.SelectedCharacter;
        return "-";
    }

    private static string ResolveServerName(int divisionIndex, int gatewayIndex)
    {
        var divisions = Game.ReferenceManager?.DivisionInfo?.Divisions;
        if (divisions == null || divisions.Count == 0 || divisionIndex < 0 || divisionIndex >= divisions.Count)
            return "Unknown";

        var division = divisions[divisionIndex];
        if (division.GatewayServers == null || division.GatewayServers.Count == 0)
            return division.Name;
        if (gatewayIndex >= 0 && gatewayIndex < division.GatewayServers.Count)
            return division.GatewayServers[gatewayIndex];
        return division.GatewayServers[0];
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

    private static void BeginReferenceDataLoad()
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

    private static bool ResolveEnabledState(string pluginId)
    {
        if (TryResolvePlugin(pluginId, out var plugin))
            return plugin.Enabled;
        if (TryResolveBotbase(pluginId, out var botbase))
            return Kernel.Bot?.Botbase?.Name == botbase.Name;
        return false;
    }

    private static bool TryResolvePlugin(string pluginId, out IPlugin plugin)
    {
        plugin = ExtensionManager.Plugins.FirstOrDefault(item =>
            PluginIdEquals(item.Name, pluginId)
            || (PluginIdEquals(pluginId, QuestPluginAlias) && PluginIdEquals(item.Name, QuestRuntimePlugin)));
        return plugin != null;
    }

    private static bool TryResolveBotbase(string pluginId, out IBotbase botbase)
    {
        botbase = ExtensionManager.Bots.FirstOrDefault(item => PluginIdEquals(item.Name, pluginId));
        return botbase != null;
    }

    private static bool PluginIdEquals(string? left, string? right)
    {
        return string.Equals(left?.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTrainingBotbase(IBotbase botbase) => ResolveModuleKey(botbase.Name) == "training";
    private static bool IsGeneralPlugin(IPlugin plugin) => ResolveModuleKey(plugin.Name) == "general";
    private static bool IsProtectionPlugin(IPlugin plugin) => ResolveModuleKey(plugin.Name) == "protection";
    private static bool IsMapPlugin(IPlugin plugin) => ResolveModuleKey(plugin.Name) == "map";

    private static string ResolveModuleKey(string id)
    {
        var value = (id ?? string.Empty).ToLowerInvariant();
        if (value.Contains("general")) return "general";
        if (value.Contains("training")) return "training";
        if (value.Contains("protection")) return "protection";
        if (value.Contains("inventory")) return "inventory";
        if (value.Contains("items")) return "items";
        if (value.Contains("map")) return "map";
        if (value.Contains("party")) return "party";
        if (value.Contains("statistics")) return "stats";
        if (value.Contains("quest")) return "quest";
        if (value.Contains("chat")) return "chat";
        if (value.Contains("log")) return "log";
        if (value.Contains("serverinfo")) return "server";
        if (value.Contains("autodungeon")) return "autodungeon";
        if (value.Contains("targetassist")) return "targetassist";
        if (value.Contains("alchemy")) return "alchemy";
        if (value.Contains("trade")) return "trade";
        if (value.Contains("lure")) return "lure";
        return value.Replace("ubot.", "");
    }
    private static Dictionary<string, object?> BuildGeneralPluginConfig()
    {
        var config = LoadPluginJsonConfig("UBot.General");
        var savedAccounts = LoadAutoLoginAccountsFromFile();
        config["enableAutomatedLogin"] = GlobalConfig.Get("UBot.General.EnableAutomatedLogin", false);
        config["autoLoginAccount"] = GlobalConfig.Get("UBot.General.AutoLoginAccountUsername", string.Empty);
        config["selectedCharacter"] = GlobalConfig.Get("UBot.General.AutoLoginCharacter", string.Empty);
        config["autoCharSelect"] = GlobalConfig.Get("UBot.General.CharacterAutoSelect", false);
        config["enableLoginDelay"] = GlobalConfig.Get("UBot.General.EnableLoginDelay", false);
        config["loginDelay"] = GlobalConfig.Get("UBot.General.LoginDelay", 10);
        config["enableWaitAfterDc"] = GlobalConfig.Get("UBot.General.EnableWaitAfterDC", false);
        config["waitAfterDc"] = GlobalConfig.Get("UBot.General.WaitAfterDC", 3);
        config["enableStaticCaptcha"] = GlobalConfig.Get("UBot.General.EnableStaticCaptcha", false);
        config["staticCaptcha"] = GlobalConfig.Get("UBot.General.StaticCaptcha", string.Empty);
        config["autoStartBot"] = GlobalConfig.Get("UBot.General.StartBot", false);
        config["useReturnScroll"] = GlobalConfig.Get("UBot.General.UseReturnScroll", false);
        config["autoHideClient"] = GlobalConfig.Get("UBot.General.HideOnStartClient", false);
        config["characterAutoSelectFirst"] = GlobalConfig.Get("UBot.General.CharacterAutoSelectFirst", false);
        config["characterAutoSelectHigher"] = GlobalConfig.Get("UBot.General.CharacterAutoSelectHigher", false);
        config["stayConnectedAfterClientExit"] = GlobalConfig.Get("UBot.General.StayConnected", false);
        config["moveToTrayOnMinimize"] = GlobalConfig.Get("UBot.General.TrayWhenMinimize", false);
        config["autoHidePendingWindow"] = GlobalConfig.Get("UBot.General.AutoHidePendingWindow", false);
        config["enablePendingQueueLogs"] = GlobalConfig.Get("UBot.General.PendingEnableQueueLogs", false);
        config["enableQueueNotification"] = GlobalConfig.Get("UBot.General.EnableQueueNotification", false);
        config["queuePeopleLeft"] = GlobalConfig.Get("UBot.General.QueueLeft", 30);
        config["sroExecutable"] = Path.Combine(
            GlobalConfig.Get("UBot.SilkroadDirectory", string.Empty),
            GlobalConfig.Get("UBot.SilkroadExecutable", string.Empty));

        var accounts = savedAccounts.Select(x => x.Username).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var characterMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var account in savedAccounts)
        {
            if (string.IsNullOrWhiteSpace(account.Username))
                continue;

            characterMap[account.Username] = account.Characters
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var selectedAccount = GlobalConfig.Get("UBot.General.AutoLoginAccountUsername", string.Empty);
        characterMap.TryGetValue(selectedAccount, out var selectedCharacters);
        var selectedCharacter = GlobalConfig.Get("UBot.General.AutoLoginCharacter", string.Empty);

        if (string.IsNullOrWhiteSpace(selectedCharacter))
        {
            var preferredCharacter = savedAccounts
                .FirstOrDefault(x => string.Equals(x.Username, selectedAccount, StringComparison.OrdinalIgnoreCase))
                ?.SelectedCharacter;
            if (!string.IsNullOrWhiteSpace(preferredCharacter))
                selectedCharacter = preferredCharacter;
        }

        config["autoLoginAccounts"] = accounts;
        config["autoLoginCharacters"] = selectedCharacters ?? new List<string>();
        config["autoLoginCharacterMap"] = characterMap;
        config["selectedCharacter"] = selectedCharacter;
        return config;
    }

    private static Dictionary<string, object?> BuildTrainingBotbaseConfig()
    {
        return new Dictionary<string, object?>
        {
            ["areaRegion"] = (int)PlayerConfig.Get<ushort>("UBot.Area.Region"),
            ["areaX"] = PlayerConfig.Get("UBot.Area.X", 0f),
            ["areaY"] = PlayerConfig.Get("UBot.Area.Y", 0f),
            ["areaZ"] = PlayerConfig.Get("UBot.Area.Z", 0f),
            ["areaRadius"] = Math.Clamp(PlayerConfig.Get("UBot.Area.Radius", 50), 5, 100),
            ["walkScript"] = PlayerConfig.Get("UBot.Walkback.File", string.Empty),
            ["useMount"] = PlayerConfig.Get("UBot.Training.checkUseMount", true),
            ["castBuffs"] = PlayerConfig.Get("UBot.Training.checkCastBuffs", true),
            ["useSpeedDrug"] = PlayerConfig.Get("UBot.Training.checkUseSpeedDrug", true),
            ["useReverse"] = PlayerConfig.Get("UBot.Training.checkBoxUseReverse", false),
            ["berserkWhenFull"] = PlayerConfig.Get("UBot.Training.checkBerzerkWhenFull", false),
            ["berserkByMonsterAmount"] = PlayerConfig.Get("UBot.Training.checkBerzerkMonsterAmount", false),
            ["berserkMonsterAmount"] = PlayerConfig.Get("UBot.Training.numBerzerkMonsterAmount", 5),
            ["berserkByAvoidance"] = PlayerConfig.Get("UBot.Training.checkBerzerkAvoidance", false),
            ["berserkByMonsterRarity"] = PlayerConfig.Get("UBot.Training.checkBerserkOnMonsterRarity", false),
            ["ignoreDimensionPillar"] = PlayerConfig.Get("UBot.Training.checkBoxDimensionPillar", false),
            ["attackWeakerFirst"] = PlayerConfig.Get("UBot.Training.checkAttackWeakerFirst", false),
            ["dontFollowMobs"] = PlayerConfig.Get("UBot.Training.checkBoxDontFollowMobs", false),
            ["avoidanceList"] = PlayerConfig.GetArray<string>("UBot.Avoidance.Avoid").ToList(),
            ["preferList"] = PlayerConfig.GetArray<string>("UBot.Avoidance.Prefer").ToList(),
            ["berserkList"] = PlayerConfig.GetArray<string>("UBot.Avoidance.Berserk").ToList()
        };
    }

    private static Dictionary<string, object?> BuildMapPluginConfig()
    {
        var config = LoadPluginJsonConfig(MapPluginName);
        var showFilter = PlayerConfig.Get("UBot.Desktop.Map.ShowFilter", string.Empty);
        if (string.IsNullOrWhiteSpace(showFilter))
            showFilter = PlayerConfig.Get("UBot.Desktop.Map.EntityFilter", "All");

        var collisionDetection = GlobalConfig.Get("UBot.EnableCollisionDetection",
            PlayerConfig.Get("UBot.Desktop.Map.CollisionDetection", false));
        var autoSelectUniques = PlayerConfig.Get("UBot.Map.AutoSelectUnique",
            PlayerConfig.Get("UBot.Desktop.Map.AutoSelectUniques", false));

        config["showFilter"] = showFilter;
        config["entityFilter"] = showFilter;
        config["collisionDetection"] = collisionDetection;
        config["autoSelectUniques"] = autoSelectUniques;
        config["autoSelectUnique"] = autoSelectUniques;
        config["resetToPlayerAt"] = PlayerConfig.Get("UBot.Desktop.Map.ResetToPlayerAt", 0L);
        return config;
    }

    private static Dictionary<string, object?> BuildProtectionPluginConfig()
    {
        return new Dictionary<string, object?>
        {
            ["hpPotionEnabled"] = PlayerConfig.Get("UBot.Protection.checkUseHPPotionsPlayer", true),
            ["hpPotionThreshold"] = Math.Clamp(PlayerConfig.Get("UBot.Protection.numPlayerHPPotionMin", 75), 0, 100),
            ["mpPotionEnabled"] = PlayerConfig.Get("UBot.Protection.checkUseMPPotionsPlayer", true),
            ["mpPotionThreshold"] = Math.Clamp(PlayerConfig.Get("UBot.Protection.numPlayerMPPotionMin", 75), 0, 100),
            ["vigorHpEnabled"] = PlayerConfig.Get("UBot.Protection.checkUseVigorHP", false),
            ["vigorHpThreshold"] = Math.Clamp(PlayerConfig.Get("UBot.Protection.numPlayerHPVigorPotionMin", 50), 0, 100),
            ["vigorMpEnabled"] = PlayerConfig.Get("UBot.Protection.checkUseVigorMP", false),
            ["vigorMpThreshold"] = Math.Clamp(PlayerConfig.Get("UBot.Protection.numPlayerMPVigorPotionMin", 50), 0, 100),
            ["skillHpEnabled"] = PlayerConfig.Get("UBot.Protection.checkUseSkillHP", false),
            ["skillHpThreshold"] = Math.Clamp(PlayerConfig.Get("UBot.Protection.numPlayerSkillHPMin", 50), 0, 100),
            ["mpSkillEnabled"] = PlayerConfig.Get("UBot.Protection.checkUseSkillMP", false),
            ["mpSkillThreshold"] = Math.Clamp(PlayerConfig.Get("UBot.Protection.numPlayerSkillMPMin", 50), 0, 100),
            ["deadDelayEnabled"] = PlayerConfig.Get("UBot.Protection.checkDead", false),
            ["stopInTown"] = PlayerConfig.Get("UBot.Protection.checkStopBotOnReturnToTown", false),
            ["noArrows"] = PlayerConfig.Get("UBot.Protection.checkNoArrows", false),
            ["fullInventory"] = PlayerConfig.Get("UBot.Protection.checkInventory", false),
            ["fullPetInventory"] = PlayerConfig.Get("UBot.Protection.checkFullPetInventory", false),
            ["hpPotionsLow"] = PlayerConfig.Get("UBot.Protection.checkNoHPPotions", false),
            ["mpPotionsLow"] = PlayerConfig.Get("UBot.Protection.checkNoMPPotions", false),
            ["lowDurability"] = PlayerConfig.Get("UBot.Protection.checkDurability", false),
            ["levelUp"] = PlayerConfig.Get("UBot.Protection.checkLevelUp", false),
            ["shardFatigue"] = PlayerConfig.Get("UBot.Protection.checkShardFatigue", false),
            ["useUniversalPills"] = PlayerConfig.Get("UBot.Protection.checkUseUniversalPills", true),
            ["useBadStatusSkill"] = PlayerConfig.Get("UBot.Protection.checkUseBadStatusSkill", false),
            ["increaseInt"] = Math.Clamp(PlayerConfig.Get("UBot.Protection.numIncInt", 0), 0, 3),
            ["increaseStr"] = Math.Clamp(PlayerConfig.Get("UBot.Protection.numIncStr", 0), 0, 3),
            ["petHpPotionEnabled"] = PlayerConfig.Get("UBot.Protection.checkUsePetHP", false),
            ["petHgpPotionEnabled"] = PlayerConfig.Get("UBot.Protection.checkUseHGP", false),
            ["petAbnormalRecoveryEnabled"] = PlayerConfig.Get("UBot.Protection.checkUseAbnormalStatePotion", true),
            ["reviveGrowthFellow"] = PlayerConfig.Get("UBot.Protection.checkReviveAttackPet", false),
            ["autoSummonGrowthFellow"] = PlayerConfig.Get("UBot.Protection.checkAutoSummonAttackPet", false),
            ["hpSkillId"] = PlayerConfig.Get("UBot.Protection.HpSkill", 0U).ToString(),
            ["mpSkillId"] = PlayerConfig.Get("UBot.Protection.MpSkill", 0U).ToString(),
            ["badStatusSkillId"] = PlayerConfig.Get("UBot.Protection.BadStatusSkill", 0U).ToString()
        };
    }
    private static bool ApplyGeneralPluginPatch(Dictionary<string, object?> patch)
    {
        var changed = false;
        var selectedCharacterPatched = false;
        string? selectedCharacterValue = null;
        string? selectedAccountValue = null;
        foreach (var kv in patch)
        {
            switch (kv.Key)
            {
                case "sroExecutable":
                    if (kv.Value is string sroPath && !string.IsNullOrWhiteSpace(sroPath))
                    {
                        var path = sroPath.Trim().Trim('"');
                        if (File.Exists(path))
                        {
                            GlobalConfig.Set("UBot.SilkroadDirectory", Path.GetDirectoryName(path) ?? string.Empty);
                            GlobalConfig.Set("UBot.SilkroadExecutable", Path.GetFileName(path));
                            changed = true;
                        }
                    }
                    break;
                case "enableAutomatedLogin":
                    changed |= SetGlobalBool("UBot.General.EnableAutomatedLogin", kv.Value);
                    break;
                case "autoLoginAccount":
                    changed |= SetGlobalString("UBot.General.AutoLoginAccountUsername", kv.Value);
                    selectedAccountValue = kv.Value?.ToString()?.Trim();
                    break;
                case "selectedCharacter":
                    changed |= SetGlobalString("UBot.General.AutoLoginCharacter", kv.Value);
                    selectedCharacterPatched = true;
                    selectedCharacterValue = kv.Value?.ToString()?.Trim() ?? string.Empty;
                    break;
                case "autoCharSelect":
                    changed |= SetGlobalBool("UBot.General.CharacterAutoSelect", kv.Value);
                    break;
                case "enableLoginDelay":
                    changed |= SetGlobalBool("UBot.General.EnableLoginDelay", kv.Value);
                    break;
                case "loginDelay":
                    changed |= SetGlobalInt("UBot.General.LoginDelay", kv.Value, 0, 3600);
                    break;
                case "enableWaitAfterDc":
                    changed |= SetGlobalBool("UBot.General.EnableWaitAfterDC", kv.Value);
                    break;
                case "waitAfterDc":
                    changed |= SetGlobalInt("UBot.General.WaitAfterDC", kv.Value, 0, 3600);
                    break;
                case "enableStaticCaptcha":
                    changed |= SetGlobalBool("UBot.General.EnableStaticCaptcha", kv.Value);
                    break;
                case "staticCaptcha":
                    changed |= SetGlobalString("UBot.General.StaticCaptcha", kv.Value);
                    break;
                case "autoStartBot":
                    changed |= SetGlobalBool("UBot.General.StartBot", kv.Value);
                    break;
                case "useReturnScroll":
                    changed |= SetGlobalBool("UBot.General.UseReturnScroll", kv.Value);
                    break;
                case "autoHideClient":
                    changed |= SetGlobalBool("UBot.General.HideOnStartClient", kv.Value);
                    break;
                case "characterAutoSelectFirst":
                    changed |= SetGlobalBool("UBot.General.CharacterAutoSelectFirst", kv.Value);
                    break;
                case "characterAutoSelectHigher":
                    changed |= SetGlobalBool("UBot.General.CharacterAutoSelectHigher", kv.Value);
                    break;
                case "stayConnectedAfterClientExit":
                    changed |= SetGlobalBool("UBot.General.StayConnected", kv.Value);
                    break;
                case "moveToTrayOnMinimize":
                    changed |= SetGlobalBool("UBot.General.TrayWhenMinimize", kv.Value);
                    break;
                case "autoHidePendingWindow":
                    changed |= SetGlobalBool("UBot.General.AutoHidePendingWindow", kv.Value);
                    break;
                case "enablePendingQueueLogs":
                    changed |= SetGlobalBool("UBot.General.PendingEnableQueueLogs", kv.Value);
                    break;
                case "enableQueueNotification":
                    changed |= SetGlobalBool("UBot.General.EnableQueueNotification", kv.Value);
                    break;
                case "queuePeopleLeft":
                    changed |= SetGlobalInt("UBot.General.QueueLeft", kv.Value, 0, 999);
                    break;
                default:
                    break;
            }
        }

        if (selectedCharacterPatched)
        {
            if (string.IsNullOrWhiteSpace(selectedAccountValue))
                selectedAccountValue = GlobalConfig.Get("UBot.General.AutoLoginAccountUsername", string.Empty);

            changed |= UpdateSelectedCharacterForAccount(selectedAccountValue, selectedCharacterValue);
        }

        return changed;
    }

    private static bool ApplyTrainingBotbasePatch(Dictionary<string, object?> patch)
    {
        var changed = false;
        changed |= SetPlayerInt("UBot.Area.Region", patch, "areaRegion", 0, ushort.MaxValue);
        changed |= SetPlayerFloat("UBot.Area.X", patch, "areaX");
        changed |= SetPlayerFloat("UBot.Area.Y", patch, "areaY");
        changed |= SetPlayerFloat("UBot.Area.Z", patch, "areaZ");
        changed |= SetPlayerInt("UBot.Area.Radius", patch, "areaRadius", 5, 100);
        changed |= SetPlayerString("UBot.Walkback.File", patch, "walkScript");
        changed |= SetPlayerBool("UBot.Training.checkUseMount", patch, "useMount");
        changed |= SetPlayerBool("UBot.Training.checkCastBuffs", patch, "castBuffs");
        changed |= SetPlayerBool("UBot.Training.checkUseSpeedDrug", patch, "useSpeedDrug");
        changed |= SetPlayerBool("UBot.Training.checkBoxUseReverse", patch, "useReverse");
        changed |= SetPlayerBool("UBot.Training.checkBerzerkWhenFull", patch, "berserkWhenFull");
        changed |= SetPlayerBool("UBot.Training.checkBerzerkMonsterAmount", patch, "berserkByMonsterAmount");
        changed |= SetPlayerInt("UBot.Training.numBerzerkMonsterAmount", patch, "berserkMonsterAmount", 1, 20);
        changed |= SetPlayerBool("UBot.Training.checkBerzerkAvoidance", patch, "berserkByAvoidance");
        changed |= SetPlayerBool("UBot.Training.checkBerserkOnMonsterRarity", patch, "berserkByMonsterRarity");
        changed |= SetPlayerBool("UBot.Training.checkBoxDimensionPillar", patch, "ignoreDimensionPillar");
        changed |= SetPlayerBool("UBot.Training.checkAttackWeakerFirst", patch, "attackWeakerFirst");
        changed |= SetPlayerBool("UBot.Training.checkBoxDontFollowMobs", patch, "dontFollowMobs");

        if (TryGetStringListValue(patch, "avoidanceList", out var avoidance))
        {
            PlayerConfig.SetArray("UBot.Avoidance.Avoid", avoidance.ToArray());
            changed = true;
        }

        if (TryGetStringListValue(patch, "preferList", out var prefer))
        {
            PlayerConfig.SetArray("UBot.Avoidance.Prefer", prefer.ToArray());
            changed = true;
        }

        if (TryGetStringListValue(patch, "berserkList", out var berserk))
        {
            PlayerConfig.SetArray("UBot.Avoidance.Berserk", berserk.ToArray());
            changed = true;
        }

        if (changed)
            EventManager.FireEvent("OnSavePlayerConfig");

        return changed;
    }

    private static bool ApplyMapPluginPatch(Dictionary<string, object?> patch)
    {
        var changed = false;
        if (TryGetStringValue(patch, "showFilter", out var showFilter) || TryGetStringValue(patch, "entityFilter", out showFilter))
        {
            var normalized = string.IsNullOrWhiteSpace(showFilter) ? "All" : showFilter.Trim();
            PlayerConfig.Set("UBot.Desktop.Map.ShowFilter", normalized);
            PlayerConfig.Set("UBot.Desktop.Map.EntityFilter", normalized);
            changed = true;
        }

        if (TryGetBoolValue(patch, "collisionDetection", out var collision))
        {
            GlobalConfig.Set("UBot.EnableCollisionDetection", collision);
            PlayerConfig.Set("UBot.Desktop.Map.CollisionDetection", collision);
            changed = true;
        }

        if (TryGetBoolValue(patch, "autoSelectUniques", out var autoSelect) || TryGetBoolValue(patch, "autoSelectUnique", out autoSelect))
        {
            PlayerConfig.Set("UBot.Map.AutoSelectUnique", autoSelect);
            PlayerConfig.Set("UBot.Desktop.Map.AutoSelectUniques", autoSelect);
            changed = true;
        }

        if (changed)
            EventManager.FireEvent("OnSavePlayerConfig");
        return changed;
    }

    private static bool ApplyProtectionPluginPatch(Dictionary<string, object?> patch)
    {
        var changed = false;
        changed |= SetPlayerBool("UBot.Protection.checkUseHPPotionsPlayer", patch, "hpPotionEnabled");
        changed |= SetPlayerInt("UBot.Protection.numPlayerHPPotionMin", patch, "hpPotionThreshold", 0, 100);
        changed |= SetPlayerBool("UBot.Protection.checkUseMPPotionsPlayer", patch, "mpPotionEnabled");
        changed |= SetPlayerInt("UBot.Protection.numPlayerMPPotionMin", patch, "mpPotionThreshold", 0, 100);
        changed |= SetPlayerBool("UBot.Protection.checkUseVigorHP", patch, "vigorHpEnabled");
        changed |= SetPlayerInt("UBot.Protection.numPlayerHPVigorPotionMin", patch, "vigorHpThreshold", 0, 100);
        changed |= SetPlayerBool("UBot.Protection.checkUseVigorMP", patch, "vigorMpEnabled");
        changed |= SetPlayerInt("UBot.Protection.numPlayerMPVigorPotionMin", patch, "vigorMpThreshold", 0, 100);
        changed |= SetPlayerBool("UBot.Protection.checkUseSkillHP", patch, "skillHpEnabled");
        changed |= SetPlayerInt("UBot.Protection.numPlayerSkillHPMin", patch, "skillHpThreshold", 0, 100);
        changed |= SetPlayerBool("UBot.Protection.checkUseSkillMP", patch, "mpSkillEnabled");
        changed |= SetPlayerInt("UBot.Protection.numPlayerSkillMPMin", patch, "mpSkillThreshold", 0, 100);

        changed |= SetPlayerBool("UBot.Protection.checkDead", patch, "deadDelayEnabled");
        changed |= SetPlayerBool("UBot.Protection.checkStopBotOnReturnToTown", patch, "stopInTown");
        changed |= SetPlayerBool("UBot.Protection.checkNoArrows", patch, "noArrows");
        changed |= SetPlayerBool("UBot.Protection.checkInventory", patch, "fullInventory");
        changed |= SetPlayerBool("UBot.Protection.checkFullPetInventory", patch, "fullPetInventory");
        changed |= SetPlayerBool("UBot.Protection.checkNoHPPotions", patch, "hpPotionsLow");
        changed |= SetPlayerBool("UBot.Protection.checkNoMPPotions", patch, "mpPotionsLow");
        changed |= SetPlayerBool("UBot.Protection.checkDurability", patch, "lowDurability");
        changed |= SetPlayerBool("UBot.Protection.checkLevelUp", patch, "levelUp");
        changed |= SetPlayerBool("UBot.Protection.checkShardFatigue", patch, "shardFatigue");
        changed |= SetPlayerBool("UBot.Protection.checkUseUniversalPills", patch, "useUniversalPills");
        changed |= SetPlayerBool("UBot.Protection.checkUseBadStatusSkill", patch, "useBadStatusSkill");
        changed |= SetPlayerInt("UBot.Protection.numIncInt", patch, "increaseInt", 0, 3);
        changed |= SetPlayerInt("UBot.Protection.numIncStr", patch, "increaseStr", 0, 3);
        changed |= SetPlayerBool("UBot.Protection.checkUsePetHP", patch, "petHpPotionEnabled");
        changed |= SetPlayerBool("UBot.Protection.checkUseHGP", patch, "petHgpPotionEnabled");
        changed |= SetPlayerBool("UBot.Protection.checkUseAbnormalStatePotion", patch, "petAbnormalRecoveryEnabled");
        changed |= SetPlayerBool("UBot.Protection.checkReviveAttackPet", patch, "reviveGrowthFellow");
        changed |= SetPlayerBool("UBot.Protection.checkAutoSummonAttackPet", patch, "autoSummonGrowthFellow");

        if (TryGetUIntValue(patch, "hpSkillId", out var hpSkill))
        {
            PlayerConfig.Set("UBot.Protection.HpSkill", hpSkill);
            changed = true;
        }
        if (TryGetUIntValue(patch, "mpSkillId", out var mpSkill))
        {
            PlayerConfig.Set("UBot.Protection.MpSkill", mpSkill);
            changed = true;
        }
        if (TryGetUIntValue(patch, "badStatusSkillId", out var badStatusSkill))
        {
            PlayerConfig.Set("UBot.Protection.BadStatusSkill", badStatusSkill);
            changed = true;
        }

        return changed;
    }

    private static bool ApplyGenericPluginPatch(string pluginId, Dictionary<string, object?> patch)
    {
        var current = LoadPluginJsonConfig(pluginId);
        foreach (var kv in patch)
            current[kv.Key] = kv.Value;

        SavePluginJsonConfig(pluginId, current);
        return true;
    }

    private static bool HandleSetTrainingAreaToCurrentPosition()
    {
        if (Game.Player == null)
            return false;

        var p = Game.Player.Position;
        PlayerConfig.Set("UBot.Area.Region", (ushort)p.Region);
        PlayerConfig.Set("UBot.Area.X", p.XOffset);
        PlayerConfig.Set("UBot.Area.Y", p.YOffset);
        PlayerConfig.Set("UBot.Area.Z", p.ZOffset);
        EventManager.FireEvent("OnSetTrainingArea");
        PlayerConfig.Save();
        return true;
    }

    private static bool HandleMapWalkToAction(Dictionary<string, object?> payload)
    {
        if (Game.Player == null)
            return false;

        if (!TryGetDoubleValue(payload, "mapX", out var mapX))
            return false;
        if (!TryGetDoubleValue(payload, "mapY", out var mapY))
            return false;

        var source = Game.Player.Position;
        var destination = new Position((float)mapX, (float)mapY, source.Region) { ZOffset = source.ZOffset };
        var movementTarget = ClampMapWalkStep(source, destination, MapClickMaxStepDistance);
        return TrySendPlayerMovePacket(movementTarget);
    }

    private static Position ClampMapWalkStep(Position source, Position destination, double maxDistance)
    {
        var distance = source.DistanceTo(destination);
        if (distance <= maxDistance || distance <= 0.01)
            return destination;

        var ratio = (float)(maxDistance / distance);
        var worldX = source.X + (destination.X - source.X) * ratio;
        var worldY = source.Y + (destination.Y - source.Y) * ratio;
        return new Position(worldX, worldY, source.Region) { ZOffset = source.ZOffset };
    }

    private static bool TrySendPlayerMovePacket(Position destination)
    {
        if (Game.Player == null)
            return false;

        var packet = new Packet(0x7021);
        packet.WriteByte(1);

        if (!Game.Player.IsInDungeon)
        {
            packet.WriteUShort(destination.Region);
            packet.WriteShort((short)Math.Clamp(Math.Round(destination.XOffset), short.MinValue, short.MaxValue));
            packet.WriteShort((short)Math.Clamp(Math.Round(destination.ZOffset), short.MinValue, short.MaxValue));
            packet.WriteShort((short)Math.Clamp(Math.Round(destination.YOffset), short.MinValue, short.MaxValue));
        }
        else
        {
            packet.WriteUShort(Game.Player.Position.Region);
            packet.WriteInt((int)Math.Round(destination.XOffset));
            packet.WriteInt((int)Math.Round(destination.ZOffset));
            packet.WriteInt((int)Math.Round(destination.YOffset));
        }

        PacketManager.SendPacket(packet, PacketDestination.Server);
        return true;
    }

    private static bool HandleChatSendAction(Dictionary<string, object?> payload)
    {
        if (!TryGetStringValue(payload, "message", out var message) || string.IsNullOrWhiteSpace(message))
            return false;

        var channel = TryGetStringValue(payload, "channel", out var parsedChannel)
            ? parsedChannel.Trim().ToLowerInvariant()
            : "all";

        if (channel == "global")
        {
            SendGlobalChatPacket(message.Trim());
            return true;
        }

        var chatType = channel switch
        {
            "private" => ChatType.Private,
            "party" => ChatType.Party,
            "guild" => ChatType.Guild,
            "union" => ChatType.Union,
            "academy" => ChatType.Academy,
            "stall" => ChatType.Stall,
            _ => ChatType.All
        };

        string? receiver = null;
        if (chatType == ChatType.Private)
        {
            if (!TryGetStringValue(payload, "target", out var target))
                return false;
            receiver = target?.Trim();
            if (string.IsNullOrWhiteSpace(receiver))
                return false;
        }

        SendChatPacket(chatType, message.Trim(), receiver);
        return true;
    }

    private static void SendChatPacket(ChatType type, string message, string? receiver = null)
    {
        var packet = new Packet(0x7025);
        packet.WriteByte(type);
        packet.WriteByte(1);

        if (Game.ClientType > GameClientType.Vietnam)
            packet.WriteByte(0);
        if (Game.ClientType >= GameClientType.Chinese_Old)
            packet.WriteByte(0);

        if (type == ChatType.Private)
            packet.WriteString(receiver ?? string.Empty);

        packet.WriteConditonalString(message);
        PacketManager.SendPacket(packet, PacketDestination.Server);
    }

    private static void SendGlobalChatPacket(string message)
    {
        var item = Game.Player?.Inventory?.GetItem(new TypeIdFilter(3, 3, 3, 5));
        if (item == null)
            return;

        var packet = new Packet(0x704C);
        packet.WriteByte(item.Slot);
        if (Game.ClientType > GameClientType.Vietnam)
        {
            packet.WriteInt(item.Record.Tid);
            packet.WriteByte(0);
        }
        else
        {
            packet.WriteUShort((ushort)item.Record.Tid);
        }

        packet.WriteConditonalString(message);
        PacketManager.SendPacket(packet, PacketDestination.Server);
    }

    private static bool TogglePendingWindow()
    {
        try
        {
            var pendingWindowType = Type.GetType("UBot.General.Views.PendingWindow, UBot.General", false);
            if (pendingWindowType == null)
                return false;

            var instanceProperty = pendingWindowType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var form = instanceProperty?.GetValue(null) as Forms.Form;
            if (form == null)
                return false;

            if (form.Visible)
                form.Hide();
            else
                form.Show();

            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool OpenAccountsWindow()
    {
        try
        {
            var viewType = Type.GetType("UBot.General.Views.View, UBot.General", false);
            if (viewType == null)
                return false;

            var accountsWindowProperty = viewType.GetProperty("AccountsWindow", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var mainViewProperty = viewType.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var form = accountsWindowProperty?.GetValue(null) as Forms.Form;
            if (form == null)
                return false;

            if (form.Visible)
            {
                form.BringToFront();
                form.Focus();
                return true;
            }

            var owner = mainViewProperty?.GetValue(null) as Forms.Control;
            if (owner != null)
                form.Show(owner);
            else
                form.Show();

            form.BringToFront();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static List<AutoLoginAccountDto> LoadAutoLoginAccountsFromFile()
    {
        try
        {
            var filePath = GetAutoLoginDataFilePath();
            if (!File.Exists(filePath))
                return new List<AutoLoginAccountDto>();

            var encoded = File.ReadAllBytes(filePath);
            if (encoded.Length == 0)
                return new List<AutoLoginAccountDto>();

            var blowfish = new Blowfish();
            var decoded = blowfish.Decode(encoded);
            if (decoded == null || decoded.Length == 0)
                return new List<AutoLoginAccountDto>();

            var json = Encoding.UTF8.GetString(decoded).Trim('\0').Trim();
            if (string.IsNullOrWhiteSpace(json))
                return new List<AutoLoginAccountDto>();

            var accounts = JsonSerializer.Deserialize<List<AutoLoginAccountDto>>(json, AutoLoginReadOptions);
            if (accounts == null)
                return new List<AutoLoginAccountDto>();

            foreach (var account in accounts)
            {
                account.Username = account.Username?.Trim() ?? string.Empty;
                account.Password ??= string.Empty;
                account.SecondaryPassword ??= string.Empty;
                account.Type = string.IsNullOrWhiteSpace(account.Type) ? "Joymax" : account.Type;
                account.ServerName = account.ServerName?.Trim() ?? string.Empty;
                account.SelectedCharacter = account.SelectedCharacter?.Trim() ?? string.Empty;
                account.Characters = (account.Characters ?? new List<string>())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (account.Channel == 0)
                    account.Channel = 1;
            }

            var result = accounts.Where(x => !string.IsNullOrWhiteSpace(x.Username)).ToList();

            // Compatibility migration:
            // Earlier Avalonia builds wrote camelCase keys (username/serverName/...).
            // UBot.General deserializes Account with PascalCase property names, so auto-login
            // can silently fail if file shape is not normalized.
            if (RequiresLegacyAutoLoginMigration(json))
            {
                if (WriteAutoLoginAccountsToFile(result))
                    ReloadGeneralAccountsRuntime();
            }

            return result;
        }
        catch
        {
            return new List<AutoLoginAccountDto>();
        }
    }

    private static bool WriteAutoLoginAccountsToFile(IReadOnlyList<AutoLoginAccountDto> accounts)
    {
        try
        {
            var json = JsonSerializer.Serialize(accounts ?? Array.Empty<AutoLoginAccountDto>());
            var data = Encoding.UTF8.GetBytes(json);
            var blowfish = new Blowfish();
            var encoded = blowfish.Encode(data);
            if (encoded == null)
                return false;

            var filePath = GetAutoLoginDataFilePath();
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllBytes(filePath, encoded);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool RequiresLegacyAutoLoginMigration(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        return json.Contains("\"username\"", StringComparison.Ordinal)
               || json.Contains("\"password\"", StringComparison.Ordinal)
               || json.Contains("\"secondaryPassword\"", StringComparison.Ordinal)
               || json.Contains("\"serverName\"", StringComparison.Ordinal)
               || json.Contains("\"selectedCharacter\"", StringComparison.Ordinal)
               || json.Contains("\"characters\"", StringComparison.Ordinal);
    }

    private static bool UpdateSelectedCharacterForAccount(string? username, string? selectedCharacter)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        var accounts = LoadAutoLoginAccountsFromFile();
        if (accounts.Count == 0)
            return false;

        var account = accounts.FirstOrDefault(x =>
            string.Equals(x.Username, username.Trim(), StringComparison.OrdinalIgnoreCase));
        if (account == null)
            return false;

        var normalizedCharacter = selectedCharacter?.Trim() ?? string.Empty;
        if (string.Equals(account.SelectedCharacter ?? string.Empty, normalizedCharacter, StringComparison.Ordinal))
            return false;

        account.SelectedCharacter = normalizedCharacter;
        if (!WriteAutoLoginAccountsToFile(accounts))
            return false;

        ReloadGeneralAccountsRuntime();
        return true;
    }

    private static void ReloadGeneralAccountsRuntime()
    {
        try
        {
            var accountsType = Type.GetType("UBot.General.Components.Accounts, UBot.General", false);
            var loadMethod = accountsType?.GetMethod("Load", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            loadMethod?.Invoke(null, null);
        }
        catch
        {
            // ignored
        }
    }

    private static string GetAutoLoginDataFilePath()
    {
        return Path.Combine(Kernel.BasePath, "User", ProfileManager.SelectedProfile, "autologin.data");
    }

    private static string GetPluginConfigKey(string pluginName) => $"UBot.Desktop.PluginConfig.{pluginName}";

    private static Dictionary<string, object?> LoadPluginJsonConfig(string pluginName)
    {
        var key = GetPluginConfigKey(pluginName);
        var raw = GlobalConfig.Get(key, "{}");
        try
        {
            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.ValueKind == JsonValueKind.Object)
                return JsonObjectToDictionary(document.RootElement);
        }
        catch
        {
            // ignored
        }

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
    }

    private static void SavePluginJsonConfig(string pluginName, Dictionary<string, object?> config)
    {
        var key = GetPluginConfigKey(pluginName);
        GlobalConfig.Set(key, JsonSerializer.Serialize(config));
    }

    private static Dictionary<string, object?> JsonObjectToDictionary(JsonElement element)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var prop in element.EnumerateObject())
            result[prop.Name] = JsonElementToObject(prop.Value);
        return result;
    }

    private static object? JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
            JsonValueKind.Object => JsonObjectToDictionary(element),
            _ => null
        };
    }

    private static JsonElement ToJsonElement(object value)
    {
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(value));
        return document.RootElement.Clone();
    }

    private static bool SetGlobalBool(string key, object? value)
    {
        if (!TryConvertBool(value, out var parsed))
            return false;
        GlobalConfig.Set(key, parsed);
        return true;
    }

    private static bool SetGlobalInt(string key, object? value, int min, int max)
    {
        if (!TryConvertInt(value, out var parsed))
            return false;
        GlobalConfig.Set(key, Math.Clamp(parsed, min, max));
        return true;
    }

    private static bool SetGlobalString(string key, object? value)
    {
        if (value == null)
            return false;
        GlobalConfig.Set(key, value.ToString() ?? string.Empty);
        return true;
    }

    private static bool SetPlayerBool(string targetKey, IDictionary<string, object?> patch, string patchKey)
    {
        if (!patch.TryGetValue(patchKey, out var value) || !TryConvertBool(value, out var parsed))
            return false;
        PlayerConfig.Set(targetKey, parsed);
        return true;
    }

    private static bool SetPlayerInt(string targetKey, IDictionary<string, object?> patch, string patchKey, int min, int max)
    {
        if (!patch.TryGetValue(patchKey, out var value) || !TryConvertInt(value, out var parsed))
            return false;
        PlayerConfig.Set(targetKey, Math.Clamp(parsed, min, max));
        return true;
    }

    private static bool SetPlayerFloat(string targetKey, IDictionary<string, object?> patch, string patchKey)
    {
        if (!patch.TryGetValue(patchKey, out var value) || !TryConvertDouble(value, out var parsed))
            return false;
        PlayerConfig.Set(targetKey, (float)parsed);
        return true;
    }

    private static bool SetPlayerString(string targetKey, IDictionary<string, object?> patch, string patchKey)
    {
        if (!patch.TryGetValue(patchKey, out var value))
            return false;
        PlayerConfig.Set(targetKey, value?.ToString() ?? string.Empty);
        return true;
    }

    private static bool TryGetStringValue(IDictionary<string, object?> payload, string key, out string value)
    {
        value = string.Empty;
        if (!payload.TryGetValue(key, out var raw) || raw == null)
            return false;

        value = raw.ToString() ?? string.Empty;
        return true;
    }

    private static bool TryGetBoolValue(IDictionary<string, object?> payload, string key, out bool value)
    {
        value = false;
        return payload.TryGetValue(key, out var raw) && TryConvertBool(raw, out value);
    }

    private static bool TryGetDoubleValue(IDictionary<string, object?> payload, string key, out double value)
    {
        value = 0;
        return payload.TryGetValue(key, out var raw) && TryConvertDouble(raw, out value);
    }

    private static bool TryGetUIntValue(IDictionary<string, object?> payload, string key, out uint value)
    {
        value = 0;
        if (!payload.TryGetValue(key, out var raw) || raw == null)
            return false;

        if (raw is uint direct)
        {
            value = direct;
            return true;
        }

        if (raw is int intValue && intValue >= 0)
        {
            value = (uint)intValue;
            return true;
        }

        if (uint.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static bool TryGetStringListValue(IDictionary<string, object?> payload, string key, out List<string> values)
    {
        values = new List<string>();
        if (!payload.TryGetValue(key, out var raw) || raw == null)
            return false;

        if (raw is string single)
        {
            if (!string.IsNullOrWhiteSpace(single))
                values.Add(single.Trim());
            return true;
        }

        if (raw is IEnumerable<string> stringEnum)
        {
            values = stringEnum.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return true;
        }

        if (raw is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                var text = item?.ToString();
                if (!string.IsNullOrWhiteSpace(text))
                    values.Add(text.Trim());
            }
            values = values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            return true;
        }

        return false;
    }

    private static bool TryConvertBool(object? value, out bool parsed)
    {
        parsed = false;
        if (value == null)
            return false;
        if (value is bool boolValue)
        {
            parsed = boolValue;
            return true;
        }
        if (value is string text && bool.TryParse(text, out var boolParsed))
        {
            parsed = boolParsed;
            return true;
        }
        return false;
    }

    private static bool TryConvertInt(object? value, out int parsed)
    {
        parsed = 0;
        if (value == null)
            return false;
        if (value is int intValue)
        {
            parsed = intValue;
            return true;
        }
        if (value is long longValue && longValue >= int.MinValue && longValue <= int.MaxValue)
        {
            parsed = (int)longValue;
            return true;
        }
        if (value is double doubleValue)
        {
            parsed = (int)Math.Round(doubleValue);
            return true;
        }
        if (int.TryParse(value.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedInt))
        {
            parsed = parsedInt;
            return true;
        }
        return false;
    }

    private static bool TryConvertDouble(object? value, out double parsed)
    {
        parsed = 0;
        if (value == null)
            return false;
        if (value is double doubleValue)
        {
            parsed = doubleValue;
            return true;
        }
        if (value is float floatValue)
        {
            parsed = floatValue;
            return true;
        }
        if (value is int intValue)
        {
            parsed = intValue;
            return true;
        }
        if (value is long longValue)
        {
            parsed = longValue;
            return true;
        }
        if (double.TryParse(value.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedDouble))
        {
            parsed = parsedDouble;
            return true;
        }
        return false;
    }
}
