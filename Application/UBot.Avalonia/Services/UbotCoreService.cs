using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Plugins;

namespace UBot.Avalonia.Services;

public sealed class UbotCoreService : UbotServiceBase, IUbotCoreService
{
    private readonly UbotCoreLifecycleService _lifecycleService;
    private readonly UbotConnectionService _connectionService;
    private readonly UbotMapService _mapService;
    private readonly UbotPluginStateService _pluginStateService;
    private readonly UbotPluginConfigService _pluginConfigService;
    private readonly UbotActionRouter _actionRouter;
    private readonly UbotIconService _iconService;
    private readonly UbotAutoLoginService _autoLoginService;
    private readonly UbotSoundNotificationService _soundNotificationService;
    private readonly UbotDialogService _dialogService;

    public event Action<string, string>? LogReceived
    {
        add
        {
            if (value != null)
                _lifecycleService.AddLogListener(value);
        }
        remove
        {
            if (value != null)
                _lifecycleService.RemoveLogListener(value);
        }
    }

    public event Action<string, string, string>? ChatMessageReceived
    {
        add
        {
            if (value != null)
                _lifecycleService.AddChatListener(value);
        }
        remove
        {
            if (value != null)
                _lifecycleService.RemoveChatListener(value);
        }
    }

    public UbotCoreService()
    {
        _lifecycleService = new UbotCoreLifecycleService();
        _connectionService = new UbotConnectionService(_lifecycleService);
        _mapService = new UbotMapService();
        _autoLoginService = new UbotAutoLoginService();
        _soundNotificationService = new UbotSoundNotificationService();
        var commandCenterService = new UbotCommandCenterService();
        var commandCenterPluginService = new UbotCommandCenterPluginService(commandCenterService);
        var generalPluginService = new UbotGeneralPluginService(_autoLoginService);
        var protectionPluginService = new UbotProtectionPluginService();
        var mapPluginService = new UbotMapPluginService();
        var skillsPluginService = new UbotSkillsPluginService();
        var itemsPluginService = new UbotItemsPluginService();
        var partyPluginService = new UbotPartyPluginService();
        var targetAssistPluginService = new UbotTargetAssistPluginService();
        var trainingBotbaseService = new UbotTrainingBotbaseService();
        var lureBotbaseService = new UbotLureBotbaseService();
        var tradeBotbaseService = new UbotTradeBotbaseService();
        var alchemyBotbaseService = new UbotAlchemyBotbaseService();
        var auxPluginStateService = new UbotPluginStateAuxService(_connectionService);

        _pluginConfigService = new UbotPluginConfigService(
            this,
            generalPluginService,
            protectionPluginService,
            mapPluginService,
            skillsPluginService,
            itemsPluginService,
            partyPluginService,
            targetAssistPluginService,
            trainingBotbaseService,
            lureBotbaseService,
            tradeBotbaseService,
            alchemyBotbaseService,
            commandCenterPluginService);
        _pluginStateService = new UbotPluginStateService(
            this,
            _connectionService,
            _mapService,
            partyPluginService,
            skillsPluginService,
            itemsPluginService,
            targetAssistPluginService,
            lureBotbaseService,
            tradeBotbaseService,
            alchemyBotbaseService,
            auxPluginStateService);
        _iconService = new UbotIconService();
        _dialogService = new UbotDialogService();

        var generalActionHandler = new UbotGeneralActionHandler(_connectionService, _mapService);
        var inventoryActionHandler = new UbotInventoryActionHandler();
        var partyActionHandler = new UbotPartyActionHandler();
        _actionRouter = new UbotActionRouter(generalActionHandler, inventoryActionHandler, partyActionHandler);

        _lifecycleService.EnsureInitialized();
    }

    public Task<RuntimeStatus> GetStatusAsync() => _connectionService.GetStatusAsync();

    public Task<ConnectionOptions> GetConnectionOptionsAsync() => _connectionService.GetConnectionOptionsAsync();

    public Task<ConnectionOptions> SetConnectionOptionsAsync(int divisionIndex, int gatewayIndex, string? mode = null, string? clientType = null)
        => _connectionService.SetConnectionOptionsAsync(divisionIndex, gatewayIndex, mode, clientType);

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
            ?? UBot.Core.RuntimeAccess.Core.Bot?.Botbase
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

    public Task<PluginStateDto> GetPluginStateAsync(string pluginId) => _pluginStateService.GetPluginStateAsync(pluginId);

    public Task<Dictionary<string, object?>> GetPluginConfigAsync(string pluginId) => _pluginConfigService.GetPluginConfigAsync(pluginId);

    public Task<bool> SetPluginConfigAsync(string pluginId, Dictionary<string, object?> patch) => _pluginConfigService.SetPluginConfigAsync(pluginId, patch);

    public Task<bool> InvokePluginActionAsync(string pluginId, string action, Dictionary<string, object?>? payload = null)
        => _actionRouter.InvokePluginActionAsync(pluginId, action, payload);

    public Task<RuntimeStatus> StartBotAsync() => _connectionService.StartBotAsync();

    public Task<RuntimeStatus> StopBotAsync() => _connectionService.StopBotAsync();

    public Task<RuntimeStatus> DisconnectAsync() => _connectionService.DisconnectAsync();

    public Task SaveConfigAsync() => _connectionService.SaveConfigAsync();

    public Task<bool> StartClientAsync() => _connectionService.StartClientAsync();

    public Task<bool> GoClientlessAsync() => _connectionService.GoClientlessAsync();

    public Task<bool> ToggleClientVisibilityAsync() => _connectionService.ToggleClientVisibilityAsync();

    public Task<bool> SetSroExecutableAsync(string path) => _connectionService.SetSroExecutableAsync(path);

    public Task<string> GetSroExecutablePathAsync() => _connectionService.GetSroExecutablePathAsync();

    public Task<IReadOnlyList<AutoLoginAccountDto>> GetAutoLoginAccountsAsync() => _autoLoginService.GetAutoLoginAccountsAsync();

    public Task<bool> SaveAutoLoginAccountsAsync(IReadOnlyList<AutoLoginAccountDto> accounts) => _autoLoginService.SaveAutoLoginAccountsAsync(accounts);

    public Task<SoundNotificationSettingsDto> GetSoundNotificationSettingsAsync() => _soundNotificationService.GetSoundNotificationSettingsAsync();

    public Task<bool> SaveSoundNotificationSettingsAsync(SoundNotificationSettingsDto settings) => _soundNotificationService.SaveSoundNotificationSettingsAsync(settings);

    public Task<string> PickExecutableAsync() => _dialogService.PickExecutableAsync();

    public Task<string> PickSoundFileAsync() => _dialogService.PickSoundFileAsync();

    public Task<string> PickScriptFileAsync() => _dialogService.PickScriptFileAsync();

    public Task<byte[]?> GetSkillIconAsync(string iconFile) => _iconService.GetSkillIconAsync(iconFile);

    public Task<byte[]?> GetEmoteIconAsync(string emoteName) => _iconService.GetEmoteIconAsync(emoteName);

    public Task<IReadOnlyList<MapLocationDto>> GetMapLocationsAsync() => _mapService.GetMapLocationsAsync();

    public Task<NetworkConfig> GetNetworkConfigAsync()
    {
        var bindIp = UBot.Core.RuntimeAccess.Global.Get("UBot.Network.BindIp", "0.0.0.0");
        var proxy = UBot.Core.RuntimeAccess.Global.GetArray<string>("UBot.Network.Proxy", '|', System.StringSplitOptions.TrimEntries);

        var active   = false;
        var proxyIp   = string.Empty;
        var proxyPort = 0;
        var proxyUser = string.Empty;
        var proxyPass = string.Empty;
        var proxyType = "SOCKS5";
        var version   = 5;

        if (proxy.Length >= 6)
        {
            _ = bool.TryParse(proxy[0], out active);
            proxyIp = proxy[1];
            _ = int.TryParse(proxy[2], System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture, out proxyPort);
            proxyUser = proxy[3];
            proxyPass = proxy[4];
            proxyType = proxy[5] == "4" ? "SOCKS4" : "SOCKS5";
            version   = proxy[5] == "4" ? 4 : 5;
        }

        return Task.FromResult(new NetworkConfig
        {
            BindIp = string.IsNullOrWhiteSpace(bindIp) ? "0.0.0.0" : bindIp,
            Proxy  = new ProxyConfig
            {
                Active   = active,
                Ip       = proxyIp,
                Port     = Math.Clamp(proxyPort, 0, 65535),
                Username = proxyUser,
                Password = proxyPass,
                Type     = proxyType,
                Version  = version
            }
        });
    }

    public Task<bool> SaveNetworkConfigAsync(NetworkConfig config)
    {
        try
        {
            UBot.Core.RuntimeAccess.Global.Set("UBot.Network.BindIp", config.BindIp ?? "0.0.0.0");
            UBot.Core.RuntimeAccess.Global.SetArray(
                "UBot.Network.Proxy",
                new List<string>
                {
                    config.Proxy.Active.ToString(),
                    config.Proxy.Ip ?? string.Empty,
                    Math.Clamp(config.Proxy.Port, 0, 65535).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    config.Proxy.Username ?? string.Empty,
                    config.Proxy.Password ?? string.Empty,
                    config.Proxy.Version.ToString(System.Globalization.CultureInfo.InvariantCulture)
                },
                "|");
            UBot.Core.RuntimeAccess.Global.Save();
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public Task SetGlobalValueAsync<T>(string key, T value)
    {
        try
        {
            UBot.Core.RuntimeAccess.Global.Set(key, value);
            UBot.Core.RuntimeAccess.Global.Save();
        }
        catch
        {
            // Bridge hatası sessizce yutulur; UI akışı kesilmemeli.
        }
        return Task.CompletedTask;
    }

    public Task LoadGlobalConfigAsync()
    {
        try
        {
            UBot.Core.RuntimeAccess.Global.Load();
        }
        catch
        {
            // Config yükleme hatası sessizce yutulur.
        }
        return Task.CompletedTask;
    }

    public Task LoadPlayerConfigAsync(string character)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(character))
                UBot.Core.RuntimeAccess.Player.Load(character);
        }
        catch
        {
            // Player config yükleme hatası sessizce yutulur.
        }
        return Task.CompletedTask;
    }

    public Task<T> GetGlobalValueAsync<T>(string key, T defaultValue)
    {
        try
        {
            var result = UBot.Core.RuntimeAccess.Global.Get(key, defaultValue);
            return Task.FromResult(result);
        }
        catch
        {
            return Task.FromResult(defaultValue);
        }
    }

    public Task SetCoreLanguageAsync(string language)
    {
        try
        {
            UBot.Core.RuntimeAccess.Global.Set("UBot.Language", language);
            UBot.Core.RuntimeAccess.Core.Language = language;
        }
        catch
        {
            // Language set hatası sessizce yutulur.
        }
        return Task.CompletedTask;
    }

    public Task SetPlayerValueOnlyAsync<T>(string key, T value)
    {
        try
        {
            UBot.Core.RuntimeAccess.Player.Set(key, value);
        }
        catch
        {
            // Player Set-only hatası sessizce yutulur.
        }
        return Task.CompletedTask;
    }

    // ─── Lure recorder event bridge ──────────────────────────────────────────

    public Task SubscribeLureRecorderEventsAsync(Action onPlayerMove, Action<uint> onCastSkill)
    {
        try
        {
            UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnPlayerMove", onPlayerMove);
            UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnCastSkill", onCastSkill);
        }
        catch { }
        return Task.CompletedTask;
    }

    public Task UnsubscribeLureRecorderEventsAsync(Action onPlayerMove, Action<uint> onCastSkill)
    {
        try
        {
            UBot.Core.RuntimeAccess.Events.UnsubscribeEvent("OnPlayerMove", onPlayerMove);
            UBot.Core.RuntimeAccess.Events.UnsubscribeEvent("OnCastSkill", onCastSkill);
        }
        catch { }
        return Task.CompletedTask;
    }

    public Task<PlayerPositionSnapshot?> GetCurrentPlayerPositionAsync()
    {
        try
        {
            var player = UBot.Core.RuntimeAccess.Session.Player;
            if (player == null)
                return Task.FromResult<PlayerPositionSnapshot?>(null);
            var pos = player.Position;
            return Task.FromResult<PlayerPositionSnapshot?>(new PlayerPositionSnapshot
            {
                XOffset = pos.XOffset,
                YOffset = pos.YOffset,
                ZOffset = pos.ZOffset,
                XSector = pos.Region.X,
                YSector = pos.Region.Y
            });
        }
        catch
        {
            return Task.FromResult<PlayerPositionSnapshot?>(null);
        }
    }

    public Task<string?> GetSkillCodeByIdAsync(uint skillId)
    {
        try
        {
            var code = UBot.Core.RuntimeAccess.Session.Player?.Skills?.GetSkillInfoById(skillId)?.Record?.Basic_Code?.Trim();
            return Task.FromResult(string.IsNullOrWhiteSpace(code) ? null : code);
        }
        catch
        {
            return Task.FromResult<string?>(null);
        }
    }
}
