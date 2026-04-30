using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UBot.Core;

namespace UBot.Avalonia.Services;

internal sealed class UbotPluginStateService : UbotServiceBase
{
    private readonly UbotConnectionService _connectionService;
    private readonly UbotMapService _mapService;
    private readonly UbotPartyPluginService _partyPluginService;
    private readonly UbotSkillsPluginService _skillsPluginService;
    private readonly UbotItemsPluginService _itemsPluginService;
    private readonly UbotTargetAssistPluginService _targetAssistPluginService;
    private readonly UbotLureBotbaseService _lureBotbaseService;
    private readonly UbotTradeBotbaseService _tradeBotbaseService;
    private readonly UbotAlchemyBotbaseService _alchemyBotbaseService;
    private readonly UbotPluginStateAuxService _auxStateService;

    internal UbotPluginStateService(
        UbotConnectionService connectionService,
        UbotMapService mapService,
        UbotPartyPluginService partyPluginService,
        UbotSkillsPluginService skillsPluginService,
        UbotItemsPluginService itemsPluginService,
        UbotTargetAssistPluginService targetAssistPluginService,
        UbotLureBotbaseService lureBotbaseService,
        UbotTradeBotbaseService tradeBotbaseService,
        UbotAlchemyBotbaseService alchemyBotbaseService,
        UbotPluginStateAuxService auxStateService)
    {
        _connectionService = connectionService;
        _mapService = mapService;
        _partyPluginService = partyPluginService;
        _skillsPluginService = skillsPluginService;
        _itemsPluginService = itemsPluginService;
        _targetAssistPluginService = targetAssistPluginService;
        _lureBotbaseService = lureBotbaseService;
        _tradeBotbaseService = tradeBotbaseService;
        _alchemyBotbaseService = alchemyBotbaseService;
        _auxStateService = auxStateService;
    }

    internal Task<PluginStateDto> GetPluginStateAsync(string pluginId)
    {
        var statusSnapshot = _connectionService.CreateStatusSnapshot();
        var state = new Dictionary<string, object?>
        {
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["botRunning"] = UBot.Core.RuntimeAccess.Core.Bot != null && UBot.Core.RuntimeAccess.Core.Bot.Running,
            ["statusText"] = statusSnapshot.StatusText,
            ["player"] = statusSnapshot.Player
        };

        if (TryResolvePlugin(pluginId, out var plugin))
        {
            if (IsSkillsPlugin(plugin))
                state["skills"] = _skillsPluginService.BuildState();
            else if (IsInventoryPlugin(plugin))
                state["inventory"] = _itemsPluginService.BuildInventoryState();
            else if (IsMapPlugin(plugin))
                state["map"] = _mapService.BuildMapPluginStateSnapshot();
            else if (IsPartyPlugin(plugin))
                state["party"] = _partyPluginService.BuildState();
            else if (IsStatisticsPlugin(plugin))
                state["stats"] = _auxStateService.BuildStatisticsPluginState();
            else if (IsQuestPlugin(plugin))
                state["quests"] = _auxStateService.BuildQuestPluginState();
            else if (IsTargetAssistPlugin(plugin))
                state["targetAssist"] = _targetAssistPluginService.BuildState();
        }

        if (TryResolveBotbase(pluginId, out var botbase))
        {
            if (IsLureBotbase(botbase))
                state["lure"] = _lureBotbaseService.BuildState(botbase);
            else if (IsTradeBotbase(botbase))
                state["trade"] = _tradeBotbaseService.BuildState(botbase);
            else if (IsAlchemyBotbase(botbase))
                state["alchemy"] = _alchemyBotbaseService.BuildState(botbase);
        }

        var dto = new PluginStateDto
        {
            Id = pluginId ?? string.Empty,
            Enabled = ResolveEnabledState(pluginId),
            State = ToJsonElement(state)
        };

        return Task.FromResult(dto);
    }

    private static bool ResolveEnabledState(string pluginId)
    {
        if (TryResolvePlugin(pluginId, out var plugin))
            return plugin.Enabled;
        if (TryResolveBotbase(pluginId, out var botbase))
            return UBot.Core.RuntimeAccess.Core.Bot?.Botbase?.Name == botbase.Name;
        return false;
    }
}

internal sealed class UbotPluginConfigService : UbotServiceBase
{
    private readonly UbotGeneralPluginService _generalPluginService;
    private readonly UbotProtectionPluginService _protectionPluginService;
    private readonly UbotMapPluginService _mapPluginService;
    private readonly UbotSkillsPluginService _skillsPluginService;
    private readonly UbotItemsPluginService _itemsPluginService;
    private readonly UbotPartyPluginService _partyPluginService;
    private readonly UbotTargetAssistPluginService _targetAssistPluginService;
    private readonly UbotTrainingBotbaseService _trainingBotbaseService;
    private readonly UbotLureBotbaseService _lureBotbaseService;
    private readonly UbotTradeBotbaseService _tradeBotbaseService;
    private readonly UbotAlchemyBotbaseService _alchemyBotbaseService;
    private readonly UbotCommandCenterPluginService _commandCenterPluginService;

    internal UbotPluginConfigService(
        UbotGeneralPluginService generalPluginService,
        UbotProtectionPluginService protectionPluginService,
        UbotMapPluginService mapPluginService,
        UbotSkillsPluginService skillsPluginService,
        UbotItemsPluginService itemsPluginService,
        UbotPartyPluginService partyPluginService,
        UbotTargetAssistPluginService targetAssistPluginService,
        UbotTrainingBotbaseService trainingBotbaseService,
        UbotLureBotbaseService lureBotbaseService,
        UbotTradeBotbaseService tradeBotbaseService,
        UbotAlchemyBotbaseService alchemyBotbaseService,
        UbotCommandCenterPluginService commandCenterPluginService)
    {
        _generalPluginService = generalPluginService;
        _protectionPluginService = protectionPluginService;
        _mapPluginService = mapPluginService;
        _skillsPluginService = skillsPluginService;
        _itemsPluginService = itemsPluginService;
        _partyPluginService = partyPluginService;
        _targetAssistPluginService = targetAssistPluginService;
        _trainingBotbaseService = trainingBotbaseService;
        _lureBotbaseService = lureBotbaseService;
        _tradeBotbaseService = tradeBotbaseService;
        _alchemyBotbaseService = alchemyBotbaseService;
        _commandCenterPluginService = commandCenterPluginService;
    }

    internal Task<Dictionary<string, object?>> GetPluginConfigAsync(string pluginId)
    {
        if (TryResolveBotbase(pluginId, out var botbase))
        {
            if (IsTrainingBotbase(botbase))
                return Task.FromResult(_trainingBotbaseService.BuildConfig());
            if (IsLureBotbase(botbase))
                return Task.FromResult(_lureBotbaseService.BuildConfig());
            if (IsTradeBotbase(botbase))
                return Task.FromResult(_tradeBotbaseService.BuildConfig());
            if (IsAlchemyBotbase(botbase))
                return Task.FromResult(_alchemyBotbaseService.BuildConfig());
        }

        if (TryResolvePlugin(pluginId, out var plugin))
        {
            if (IsGeneralPlugin(plugin))
                return Task.FromResult(_generalPluginService.BuildConfig());
            if (IsProtectionPlugin(plugin))
                return Task.FromResult(_protectionPluginService.BuildConfig());
            if (IsMapPlugin(plugin))
                return Task.FromResult(_mapPluginService.BuildConfig());
            if (IsSkillsPlugin(plugin))
                return Task.FromResult(_skillsPluginService.BuildConfig());
            if (IsItemsPlugin(plugin))
                return Task.FromResult(_itemsPluginService.BuildConfig());
            if (IsPartyPlugin(plugin))
                return Task.FromResult(_partyPluginService.BuildConfig());
            if (IsTargetAssistPlugin(plugin))
                return Task.FromResult(_targetAssistPluginService.BuildConfig());
            if (IsCommandCenterPlugin(plugin))
                return Task.FromResult(_commandCenterPluginService.BuildConfig());
        }

        return Task.FromResult(UbotPluginConfigHelpers.LoadRawConfig(pluginId));
    }

    internal Task<bool> SetPluginConfigAsync(string pluginId, Dictionary<string, object?> patch)
    {
        if (patch == null || patch.Count == 0)
            return Task.FromResult(false);

        var changed = false;

        if (TryResolveBotbase(pluginId, out var botbase))
        {
            if (IsTrainingBotbase(botbase))
                changed = _trainingBotbaseService.ApplyPatch(patch);
            else if (IsLureBotbase(botbase))
                changed = _lureBotbaseService.ApplyPatch(botbase, patch);
            else if (IsTradeBotbase(botbase))
                changed = _tradeBotbaseService.ApplyPatch(botbase, patch);
            else if (IsAlchemyBotbase(botbase))
                changed = _alchemyBotbaseService.ApplyPatch(botbase, patch);
        }
        else if (TryResolvePlugin(pluginId, out var plugin))
        {
            if (IsGeneralPlugin(plugin))
                changed = _generalPluginService.ApplyPatch(patch);
            else if (IsProtectionPlugin(plugin))
                changed = _protectionPluginService.ApplyPatch(patch);
            else if (IsMapPlugin(plugin))
                changed = _mapPluginService.ApplyPatch(patch);
            else if (IsSkillsPlugin(plugin))
                changed = _skillsPluginService.ApplyPatch(patch);
            else if (IsItemsPlugin(plugin))
                changed = _itemsPluginService.ApplyPatch(patch);
            else if (IsPartyPlugin(plugin))
                changed = _partyPluginService.ApplyPatch(patch);
            else if (IsTargetAssistPlugin(plugin))
                changed = _targetAssistPluginService.ApplyPatch(patch);
            else if (IsCommandCenterPlugin(plugin))
                changed = _commandCenterPluginService.ApplyPatch(patch);
            else
                changed = UbotPluginConfigHelpers.ApplyGenericPatch(plugin.Name, patch);
        }
        else
        {
            changed = UbotPluginConfigHelpers.ApplyGenericPatch(pluginId, patch);
        }

        if (changed)
        {
            UBot.Core.RuntimeAccess.Global.Save();
            UBot.Core.RuntimeAccess.Player.Save();
        }

        return Task.FromResult(changed);
    }

    internal static void ApplyLivePartySettingsFromConfig()
    {
        UbotPartyPluginService.ApplyLivePartySettingsFromConfig();
    }

    internal static void RefreshPartyPluginRuntime()
    {
        UbotPartyPluginService.RefreshPartyPluginRuntime();
    }
}
