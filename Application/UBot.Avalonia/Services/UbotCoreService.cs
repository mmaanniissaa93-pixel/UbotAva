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
        var commandCenterService = new UbotCommandCenterService();
        var pluginModuleService = new UbotPluginModuleService(
            _connectionService,
            _mapService,
            commandCenterService,
            _autoLoginService);
        _pluginConfigService = new UbotPluginConfigService(pluginModuleService);
        _pluginStateService = new UbotPluginStateService(pluginModuleService);
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

    public Task<string> PickExecutableAsync() => _dialogService.PickExecutableAsync();

    public Task<string> PickScriptFileAsync() => _dialogService.PickScriptFileAsync();

    public Task<byte[]?> GetSkillIconAsync(string iconFile) => _iconService.GetSkillIconAsync(iconFile);

    public Task<byte[]?> GetEmoteIconAsync(string emoteName) => _iconService.GetEmoteIconAsync(emoteName);

    public Task<IReadOnlyList<MapLocationDto>> GetMapLocationsAsync() => _mapService.GetMapLocationsAsync();
}
