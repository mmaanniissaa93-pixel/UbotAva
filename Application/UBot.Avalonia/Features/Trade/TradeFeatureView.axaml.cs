using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using global::Avalonia.Layout;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.Trade;

public partial class TradeFeatureView : UserControl
{
    private sealed class RouteListModel
    {
        public string Name { get; set; } = "Default";
        public List<string> Scripts { get; set; } = new();
    }

    private PluginViewModelBase? _vm;
    private AppState? _state;
    private bool _built;
    private bool _syncing;

    private CheckBox? _useRouteScriptsCheck;
    private CheckBox? _tracePlayerCheck;
    private TextBox? _tracePlayerNameBox;
    private ComboBox? _routeListCombo;
    private ListBox? _scriptsList;
    private TextBox? _scriptInputBox;
    private TextBlock? _runtimeLabel;

    private CheckBox? _runTownScriptCheck;
    private CheckBox? _waitHunterCheck;
    private CheckBox? _attackThiefPlayersCheck;
    private CheckBox? _attackThiefNpcsCheck;
    private CheckBox? _counterAttackCheck;
    private CheckBox? _protectTransportCheck;
    private CheckBox? _castBuffsCheck;
    private CheckBox? _mountTransportCheck;
    private TextBox? _maxTransportDistanceBox;
    private CheckBox? _sellGoodsCheck;
    private CheckBox? _buyGoodsCheck;
    private TextBox? _buyGoodsQuantityBox;
    private TextBox? _recorderPathBox;

    private readonly ObservableCollection<string> _scripts = new();
    private readonly List<RouteListModel> _routeLists = new();

    public TradeFeatureView()
    {
        InitializeComponent();
    }

    public void Initialize(PluginViewModelBase vm, AppState state)
    {
        _vm = vm;
        _state = state;
        Build();
        _ = LoadFromConfigAsync();
    }

    public void UpdateFromState(JsonElement moduleState)
    {
        Build();
        var root = moduleState;
        if (moduleState.ValueKind == JsonValueKind.Object && moduleState.TryGetProperty("trade", out var tradeNode))
            root = tradeNode;
        if (root.ValueKind != JsonValueKind.Object || _runtimeLabel == null)
            return;

        var scriptRunning = root.TryGetProperty("scriptRunning", out var runningNode) && runningNode.ValueKind == JsonValueKind.True;
        var route = root.TryGetProperty("currentRouteName", out var routeNode) ? routeNode.GetString() ?? string.Empty : string.Empty;
        var hasTransport = root.TryGetProperty("hasTransport", out var transportNode) && transportNode.ValueKind == JsonValueKind.True;
        var distance = root.TryGetProperty("transportDistance", out var distanceNode) && distanceNode.TryGetDouble(out var distanceValue)
            ? distanceValue
            : -1;

        _runtimeLabel.Text = $"Script running: {(scriptRunning ? "Yes" : "No")}  |  Route: {route}  |  Transport: {(hasTransport ? $"Yes ({distance:0.0}m)" : "No")}";
    }

    private async System.Threading.Tasks.Task LoadFromConfigAsync()
    {
        if (_vm == null)
            return;

        await _vm.LoadConfigAsync();
        _syncing = true;
        try
        {
            _useRouteScriptsCheck!.IsChecked = _vm.BoolCfg("tradeUseRouteScripts", true);
            _tracePlayerCheck!.IsChecked = _vm.BoolCfg("tradeTracePlayer", false);
            _tracePlayerNameBox!.Text = _vm.TextCfg("tradeTracePlayerName", string.Empty);
            _runTownScriptCheck!.IsChecked = _vm.BoolCfg("tradeRunTownScript", false);
            _waitHunterCheck!.IsChecked = _vm.BoolCfg("tradeWaitForHunter", false);
            _attackThiefPlayersCheck!.IsChecked = _vm.BoolCfg("tradeAttackThiefPlayers", false);
            _attackThiefNpcsCheck!.IsChecked = _vm.BoolCfg("tradeAttackThiefNpcs", false);
            _counterAttackCheck!.IsChecked = _vm.BoolCfg("tradeCounterAttack", false);
            _protectTransportCheck!.IsChecked = _vm.BoolCfg("tradeProtectTransport", false);
            _castBuffsCheck!.IsChecked = _vm.BoolCfg("tradeCastBuffs", false);
            _mountTransportCheck!.IsChecked = _vm.BoolCfg("tradeMountTransport", false);
            _maxTransportDistanceBox!.Text = ((int)Math.Round(_vm.NumCfg("tradeMaxTransportDistance", 15))).ToString(CultureInfo.InvariantCulture);
            _sellGoodsCheck!.IsChecked = _vm.BoolCfg("tradeSellGoods", true);
            _buyGoodsCheck!.IsChecked = _vm.BoolCfg("tradeBuyGoods", true);
            _buyGoodsQuantityBox!.Text = ((int)Math.Round(_vm.NumCfg("tradeBuyGoodsQuantity", 0))).ToString(CultureInfo.InvariantCulture);
            _recorderPathBox!.Text = _vm.TextCfg("tradeRecorderScriptPath", string.Empty);

            var selectedIndex = (int)Math.Round(_vm.NumCfg("tradeSelectedRouteListIndex", 0));
            _routeLists.Clear();
            foreach (var list in ParseRouteLists(_vm.ObjCfg("tradeRouteLists")))
                _routeLists.Add(list);
            if (_routeLists.Count == 0)
                _routeLists.Add(new RouteListModel { Name = "Default", Scripts = new List<string>() });

            RefreshRouteListCombo();
            var clamped = Math.Clamp(selectedIndex, 0, _routeLists.Count - 1);
            _routeListCombo!.SelectedIndex = clamped;
            RefreshScripts();
        }
        finally
        {
            _syncing = false;
        }
    }

    private void Build()
    {
        if (_built)
            return;
        _built = true;

        TabStripCtrl.SetTabs(new[] { ("trade", "Trade") });
        TabStripCtrl.ActiveTabId = "trade";
        ContentHost.Children.Clear();

        var layout = new StackPanel { Spacing = 10 };
        _runtimeLabel = new TextBlock { Text = "Script running: No", Classes = { "form-label" } };
        layout.Children.Add(_runtimeLabel);

        _useRouteScriptsCheck = CreateCheck("Use route scripts");
        _tracePlayerCheck = CreateCheck("Trace player");
        _tracePlayerNameBox = CreateTextBox(string.Empty);
        _routeListCombo = new ComboBox { Width = 240 };
        _routeListCombo.SelectionChanged += RouteListCombo_SelectionChanged;
        _scriptsList = new ListBox { Height = 160, ItemsSource = _scripts };
        _scriptInputBox = CreateTextBox(string.Empty, 420);

        var addScriptBtn = new Button { Content = "Add Script", Width = 120 };
        addScriptBtn.Click += AddScriptBtn_Click;
        var removeScriptBtn = new Button { Content = "Remove Selected", Width = 140 };
        removeScriptBtn.Click += RemoveScriptBtn_Click;
        var addListBtn = new Button { Content = "Add Route List", Width = 120 };
        addListBtn.Click += AddListBtn_Click;
        var removeListBtn = new Button { Content = "Remove Route List", Width = 140 };
        removeListBtn.Click += RemoveListBtn_Click;

        _runTownScriptCheck = CreateCheck("Run town script");
        _waitHunterCheck = CreateCheck("Wait for hunter");
        _attackThiefPlayersCheck = CreateCheck("Attack thief players");
        _attackThiefNpcsCheck = CreateCheck("Attack thief NPCs");
        _counterAttackCheck = CreateCheck("Counter attack");
        _protectTransportCheck = CreateCheck("Protect transport");
        _castBuffsCheck = CreateCheck("Cast buffs");
        _mountTransportCheck = CreateCheck("Mount transport");
        _maxTransportDistanceBox = CreateTextBox("15", 120);
        _sellGoodsCheck = CreateCheck("Sell goods");
        _buyGoodsCheck = CreateCheck("Buy goods");
        _buyGoodsQuantityBox = CreateTextBox("0", 120);
        _recorderPathBox = CreateTextBox(string.Empty, 420);

        layout.Children.Add(CreateRowControl(_useRouteScriptsCheck, _tracePlayerCheck));
        layout.Children.Add(CreateRow("Trace player name", _tracePlayerNameBox));
        layout.Children.Add(CreateRow("Route list", _routeListCombo));
        layout.Children.Add(CreateRow("Scripts", _scriptsList));
        layout.Children.Add(CreateRow("Script path", _scriptInputBox));
        layout.Children.Add(CreateRowControl(addScriptBtn, removeScriptBtn));
        layout.Children.Add(CreateRowControl(addListBtn, removeListBtn));
        layout.Children.Add(_runTownScriptCheck);
        layout.Children.Add(_waitHunterCheck);
        layout.Children.Add(_attackThiefPlayersCheck);
        layout.Children.Add(_attackThiefNpcsCheck);
        layout.Children.Add(_counterAttackCheck);
        layout.Children.Add(_protectTransportCheck);
        layout.Children.Add(_castBuffsCheck);
        layout.Children.Add(_mountTransportCheck);
        layout.Children.Add(CreateRow("Max transport distance", _maxTransportDistanceBox));
        layout.Children.Add(_sellGoodsCheck);
        layout.Children.Add(_buyGoodsCheck);
        layout.Children.Add(CreateRow("Buy goods quantity", _buyGoodsQuantityBox));
        layout.Children.Add(CreateRow("Recorder script path", _recorderPathBox));

        var saveBtn = new Button { Content = "Save", Classes = { "primary" }, Width = 140 };
        saveBtn.Click += SaveBtn_Click;
        layout.Children.Add(saveBtn);

        ContentHost.Children.Add(layout);
    }

    private void RouteListCombo_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncing)
            return;
        RefreshScripts();
    }

    private void AddScriptBtn_Click(object? sender, RoutedEventArgs e)
    {
        var selected = GetSelectedRouteList();
        if (selected == null)
            return;

        var path = (_scriptInputBox?.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(path))
            return;
        if (selected.Scripts.Contains(path, StringComparer.OrdinalIgnoreCase))
            return;

        selected.Scripts.Add(path);
        _scriptInputBox!.Text = string.Empty;
        RefreshScripts();
    }

    private void RemoveScriptBtn_Click(object? sender, RoutedEventArgs e)
    {
        var selected = GetSelectedRouteList();
        if (selected == null || _scriptsList?.SelectedItem is not string script)
            return;

        selected.Scripts.RemoveAll(x => x.Equals(script, StringComparison.OrdinalIgnoreCase));
        RefreshScripts();
    }

    private void AddListBtn_Click(object? sender, RoutedEventArgs e)
    {
        var name = $"Route List {_routeLists.Count + 1}";
        _routeLists.Add(new RouteListModel { Name = name, Scripts = new List<string>() });
        RefreshRouteListCombo();
        _routeListCombo!.SelectedIndex = _routeLists.Count - 1;
        RefreshScripts();
    }

    private void RemoveListBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (_routeLists.Count <= 1 || _routeListCombo == null)
            return;

        var index = _routeListCombo.SelectedIndex;
        if (index < 0 || index >= _routeLists.Count)
            return;

        _routeLists.RemoveAt(index);
        RefreshRouteListCombo();
        _routeListCombo.SelectedIndex = Math.Clamp(index - 1, 0, _routeLists.Count - 1);
        RefreshScripts();
    }

    private async void SaveBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null || _syncing)
            return;

        var patch = new Dictionary<string, object?>
        {
            ["tradeUseRouteScripts"] = _useRouteScriptsCheck?.IsChecked == true,
            ["tradeTracePlayer"] = _tracePlayerCheck?.IsChecked == true,
            ["tradeTracePlayerName"] = (_tracePlayerNameBox?.Text ?? string.Empty).Trim(),
            ["tradeSelectedRouteListIndex"] = Math.Max(0, _routeListCombo?.SelectedIndex ?? 0),
            ["tradeRouteLists"] = _routeLists.Select(routeList => new Dictionary<string, object?>
            {
                ["name"] = routeList.Name,
                ["scripts"] = routeList.Scripts.Cast<object?>().ToList()
            }).Cast<object?>().ToList(),
            ["tradeRunTownScript"] = _runTownScriptCheck?.IsChecked == true,
            ["tradeWaitForHunter"] = _waitHunterCheck?.IsChecked == true,
            ["tradeAttackThiefPlayers"] = _attackThiefPlayersCheck?.IsChecked == true,
            ["tradeAttackThiefNpcs"] = _attackThiefNpcsCheck?.IsChecked == true,
            ["tradeCounterAttack"] = _counterAttackCheck?.IsChecked == true,
            ["tradeProtectTransport"] = _protectTransportCheck?.IsChecked == true,
            ["tradeCastBuffs"] = _castBuffsCheck?.IsChecked == true,
            ["tradeMountTransport"] = _mountTransportCheck?.IsChecked == true,
            ["tradeMaxTransportDistance"] = ParseInt(_maxTransportDistanceBox?.Text, 15),
            ["tradeSellGoods"] = _sellGoodsCheck?.IsChecked == true,
            ["tradeBuyGoods"] = _buyGoodsCheck?.IsChecked == true,
            ["tradeBuyGoodsQuantity"] = ParseInt(_buyGoodsQuantityBox?.Text, 0),
            ["tradeRecorderScriptPath"] = (_recorderPathBox?.Text ?? string.Empty).Trim()
        };

        await _vm.PatchConfigAsync(patch);
    }

    private RouteListModel? GetSelectedRouteList()
    {
        if (_routeListCombo == null)
            return null;
        var index = _routeListCombo.SelectedIndex;
        if (index < 0 || index >= _routeLists.Count)
            return null;
        return _routeLists[index];
    }

    private void RefreshRouteListCombo()
    {
        if (_routeListCombo == null)
            return;
        _routeListCombo.ItemsSource = _routeLists.Select(routeList => routeList.Name).ToList();
    }

    private void RefreshScripts()
    {
        _scripts.Clear();
        var selected = GetSelectedRouteList();
        if (selected == null)
            return;
        foreach (var script in selected.Scripts)
            _scripts.Add(script);
    }

    private static List<RouteListModel> ParseRouteLists(object? raw)
    {
        var result = new List<RouteListModel>();
        if (raw is not IEnumerable enumerable || raw is string)
            return result;

        foreach (var item in enumerable)
        {
            if (item is not IDictionary dictionary)
                continue;

            var name = dictionary.Contains("name") ? dictionary["name"]?.ToString() ?? string.Empty : string.Empty;
            var scripts = new List<string>();
            if (dictionary.Contains("scripts") && dictionary["scripts"] is IEnumerable scriptsEnumerable)
            {
                foreach (var script in scriptsEnumerable)
                {
                    var path = script?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(path))
                        scripts.Add(path);
                }
            }

            if (string.IsNullOrWhiteSpace(name))
                name = $"Route List {result.Count + 1}";

            result.Add(new RouteListModel
            {
                Name = name,
                Scripts = scripts.Distinct(StringComparer.OrdinalIgnoreCase).ToList()
            });
        }

        return result;
    }

    private static StackPanel CreateRow(string label, Control control)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        panel.Children.Add(new TextBlock { Text = label, Width = 220, VerticalAlignment = VerticalAlignment.Center });
        panel.Children.Add(control);
        return panel;
    }

    private static StackPanel CreateRowControl(Control left, Control right)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        panel.Children.Add(left);
        panel.Children.Add(right);
        return panel;
    }

    private static TextBox CreateTextBox(string value, double width = 260)
    {
        return new TextBox
        {
            Text = value,
            Width = width
        };
    }

    private static CheckBox CreateCheck(string label)
    {
        return new CheckBox
        {
            Content = label,
            Classes = { "check" }
        };
    }

    private static int ParseInt(string? value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }
}
