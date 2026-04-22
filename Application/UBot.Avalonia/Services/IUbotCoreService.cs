using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UBot.Avalonia.Services;

public interface IUbotCoreService
{
    event Action<string, string>? LogReceived;
    event Action<string, string, string>? ChatMessageReceived;

    Task<RuntimeStatus> GetStatusAsync();
    Task<ConnectionOptions> GetConnectionOptionsAsync();
    Task<ConnectionOptions> SetConnectionOptionsAsync(int divisionIndex, int gatewayIndex, string? mode = null, string? clientType = null);

    Task<IReadOnlyList<PluginDescriptor>> GetPluginsAsync();
    Task<PluginStateDto> GetPluginStateAsync(string pluginId);

    Task<Dictionary<string, object?>> GetPluginConfigAsync(string pluginId);
    Task<bool> SetPluginConfigAsync(string pluginId, Dictionary<string, object?> patch);
    Task<bool> InvokePluginActionAsync(string pluginId, string action, Dictionary<string, object?>? payload = null);

    Task<RuntimeStatus> StartBotAsync();
    Task<RuntimeStatus> StopBotAsync();
    Task<RuntimeStatus> DisconnectAsync();
    Task SaveConfigAsync();

    Task<bool> StartClientAsync();
    Task<bool> GoClientlessAsync();
    Task<bool> ToggleClientVisibilityAsync();

    Task<bool> SetSroExecutableAsync(string path);
    Task<string> GetSroExecutablePathAsync();
    Task<IReadOnlyList<AutoLoginAccountDto>> GetAutoLoginAccountsAsync();
    Task<bool> SaveAutoLoginAccountsAsync(IReadOnlyList<AutoLoginAccountDto> accounts);
    Task<string> PickExecutableAsync();
    Task<string> PickScriptFileAsync();
    Task<byte[]?> GetSkillIconAsync(string iconFile);
    Task<byte[]?> GetEmoteIconAsync(string emoteName);
    Task<IReadOnlyList<MapLocationDto>> GetMapLocationsAsync();
}
