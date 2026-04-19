using Avalonia.Controls;
using System.Collections.Generic;
using System.Text.Json;
using UBot.Avalonia.Features;
using UBot.Avalonia.Features.Alchemy;
using UBot.Avalonia.Features.AutoDungeon;
using UBot.Avalonia.Features.Chat;
using UBot.Avalonia.Features.General;
using UBot.Avalonia.Features.Inventory;
using UBot.Avalonia.Features.Items;
using UBot.Avalonia.Features.Logging;
using UBot.Avalonia.Features.Lure;
using UBot.Avalonia.Features.Map;
using UBot.Avalonia.Features.Party;
using UBot.Avalonia.Features.Protection;
using UBot.Avalonia.Features.Quest;
using UBot.Avalonia.Features.ServerInfo;
using UBot.Avalonia.Features.Skills;
using UBot.Avalonia.Features.Statistics;
using UBot.Avalonia.Features.TargetAssist;
using UBot.Avalonia.Features.Trade;
using UBot.Avalonia.Features.Training;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia;

/// <summary>
/// Creates and caches feature views. Call GetView(pluginId) from MainWindow navigation.
/// </summary>
public sealed class FeatureViewFactory
{
    private readonly IUbotCoreService _core;
    private readonly AppState         _state;

    private sealed class SimpleVm : PluginViewModelBase
    {
        public SimpleVm(IUbotCoreService core, AppState state) : base(core, state) { }
        protected override async void OnAttached() => await LoadConfigAsync();
    }

    private readonly Dictionary<string, (UserControl View, PluginViewModelBase Vm)> _cache = new();

    public FeatureViewFactory(IUbotCoreService core, AppState state)
    {
        _core  = core;
        _state = state;
    }

    public UserControl GetView(string pluginId)
    {
        if (_cache.TryGetValue(pluginId, out var cached)) return cached.View;

        var (view, vm) = Build(pluginId, NormKey(pluginId));
        vm.Attach(pluginId);
        _cache[pluginId] = (view, vm);
        return view;
    }

    public void UpdateState(string pluginId, JsonElement state)
    {
        if (!_cache.TryGetValue(pluginId, out var cached)) return;
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (cached.View is MapFeatureView   map)   map.UpdateFromState(state);
            if (cached.View is StatisticsFeatureView s) s.UpdateFromState(state);
            if (cached.View is SkillsFeatureView skills) skills.UpdateFromState(state);
            if (cached.View is ItemsFeatureView items) items.UpdateFromState(state);
            if (cached.View is InventoryFeatureView inv) inv.UpdateFromState(state);
        });
    }

    private (UserControl, PluginViewModelBase) Build(string pluginId, string key)
    {
        switch (key)
        {
            case "general":
            {
                var vm = new GeneralViewModel(_core, _state);
                var v  = new GeneralFeatureView();
                v.Initialize(vm);
                return (v, vm);
            }
            case "training":
            {
                var vm = new SimpleVm(_core, _state);
                var v  = new TrainingFeatureView();
                v.Initialize(vm);
                return (v, vm);
            }
            case "protection":
            {
                var vm = new SimpleVm(_core, _state);
                var v  = new ProtectionFeatureView();
                v.Initialize(vm);
                return (v, vm);
            }
            case "map":
            {
                var vm = new SimpleVm(_core, _state);
                var v  = new MapFeatureView();
                v.Initialize(vm);
                return (v, vm);
            }
            case "chat":
            {
                var vm = new SimpleVm(_core, _state);
                var v  = new ChatFeatureView();
                v.Initialize(vm, _state);
                return (v, vm);
            }
            case "log":
            {
                var vm = new SimpleVm(_core, _state);
                var v  = new LogFeatureView();
                v.Initialize(vm, _state);
                return (v, vm);
            }
            case "skills":
            {
                var vm = new SkillsViewModel(_core, _state);
                var v  = new SkillsFeatureView();
                v.Initialize(vm, _state);
                return (v, vm);
            }
            case "party":
            {
                var vm = new SimpleVm(_core, _state);
                var v  = new PartyFeatureView();
                v.Initialize(vm, _state);
                return (v, vm);
            }
            case "alchemy":
            {
                var vm = new SimpleVm(_core, _state);
                var v  = new AlchemyFeatureView();
                v.Initialize(vm, _state);
                return (v, vm);
            }
            case "trade":
            {
                var vm = new SimpleVm(_core, _state);
                var v  = new TradeFeatureView();
                v.Initialize(vm, _state);
                return (v, vm);
            }
            case "lure":
            {
                var vm = new SimpleVm(_core, _state);
                var v  = new LureFeatureView();
                v.Initialize(vm, _state);
                return (v, vm);
            }
            case "quest":
            case "quests":
            {
                var vm = new SimpleVm(_core, _state);
                var v  = new QuestFeatureView();
                v.Initialize(vm, _state);
                return (v, vm);
            }
            case "inventory":
            {
                var vm = new SimpleVm(_core, _state);
                var v  = new InventoryFeatureView();
                v.Initialize(vm, _state);
                return (v, vm);
            }
            case "items":
            {
                var vm = new SimpleVm(_core, _state);
                var v  = new ItemsFeatureView();
                v.Initialize(vm, _state);
                return (v, vm);
            }
            case "stats":
            case "statistics":
            {
                var vm = new SimpleVm(_core, _state);
                var v  = new StatisticsFeatureView();
                v.Initialize(vm, _state);
                return (v, vm);
            }
            case "targetassist":
            {
                var vm = new SimpleVm(_core, _state);
                var v  = new TargetAssistFeatureView();
                v.Initialize(vm, _state);
                return (v, vm);
            }
            case "autodungeon":
            {
                var vm = new SimpleVm(_core, _state);
                var v  = new AutoDungeonFeatureView();
                v.Initialize(vm, _state);
                return (v, vm);
            }
            case "server":
            {
                var vm = new SimpleVm(_core, _state);
                var v  = new ServerInfoFeatureView();
                v.Initialize(vm, _state);
                return (v, vm);
            }
            default:
            {
                var vm = new SimpleVm(_core, _state);
                var v  = new GenericFeatureView();
                v.Configure(pluginId, "Property", "Value", System.Array.Empty<string[]>());
                return (v, vm);
            }
        }
    }

    private static string NormKey(string id)
    {
        var s = id.ToLowerInvariant();
        if (s.Contains("general"))        return "general";
        if (s.Contains("training"))       return "training";
        if (s.Contains("skills"))         return "skills";
        if (s.Contains("protection"))     return "protection";
        if (s.Contains("inventory"))      return "inventory";
        if (s.Contains("items"))          return "items";
        if (s.Contains("map"))            return "map";
        if (s.Contains("party"))          return "party";
        if (s.Contains("statistics"))     return "stats";
        if (s.Contains("quest"))          return "quest";
        if (s.Contains("chat"))           return "chat";
        if (s.Contains("log"))            return "log";
        if (s.Contains("serverinfo"))     return "server";
        if (s.Contains("autodungeon"))    return "autodungeon";
        if (s.Contains("targetassist"))   return "targetassist";
        if (s.Contains("alchemy"))        return "alchemy";
        if (s.Contains("trade"))          return "trade";
        if (s.Contains("lure"))           return "lure";
        if (s.Contains("stats"))          return "stats";
        return s.Replace("ubot.", "");
    }
}

