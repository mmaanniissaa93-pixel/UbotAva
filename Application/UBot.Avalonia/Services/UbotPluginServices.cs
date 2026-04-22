using System.Collections.Generic;
using System.Threading.Tasks;

namespace UBot.Avalonia.Services;

internal sealed class UbotPluginStateService
{
    private readonly UbotPluginModuleService _moduleService;

    internal UbotPluginStateService(UbotPluginModuleService moduleService)
    {
        _moduleService = moduleService;
    }

    internal Task<PluginStateDto> GetPluginStateAsync(string pluginId)
    {
        return _moduleService.GetPluginStateAsync(pluginId);
    }
}

internal sealed class UbotPluginConfigService
{
    private readonly UbotPluginModuleService _moduleService;

    internal UbotPluginConfigService(UbotPluginModuleService moduleService)
    {
        _moduleService = moduleService;
    }

    internal Task<Dictionary<string, object?>> GetPluginConfigAsync(string pluginId)
    {
        return _moduleService.GetPluginConfigAsync(pluginId);
    }

    internal Task<bool> SetPluginConfigAsync(string pluginId, Dictionary<string, object?> patch)
    {
        return _moduleService.SetPluginConfigAsync(pluginId, patch);
    }

    internal static void ApplyLivePartySettingsFromConfig()
    {
        UbotPluginModuleService.ApplyLivePartySettingsFromConfig();
    }

    internal static void RefreshPartyPluginRuntime()
    {
        UbotPluginModuleService.RefreshPartyPluginRuntime();
    }
}
