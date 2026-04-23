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
    private string _activeTab = "route";

    private CheckBox? _useRouteScriptsCheck;
    private CheckBox? _tracePlayerCheck;
    private TextBox? _tracePlayerNameBox;
    private ComboBox? _routeListCombo;
    private ListBox? _scriptsList;
    private TextBox? _scriptInputBox;
    private TextBlock? _runtimeLabel;
    private StackPanel? _routePanel;

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
    private StackPanel? _settingsPanel;

    private StackPanel? _jobOverviewPanel;
    private TextBlock? _jobAliasValue;
    private TextBlock? _jobTypeValue;
    private TextBlock? _jobLevelValue;
    private TextBlock? _jobExperienceValue;
    private TextBlock? _jobDifficultyValue;
    private TextBlock? _jobRouteValue;
    private TextBlock? _jobTransportValue;
    private readonly ObservableCollection<string> _routeOverviewRows = new();
    private ListBox? _routeOverviewList;

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
        if (root.ValueKind != JsonValueKind.Object)
            return;

        var scriptRunning = root.TryGetProperty("scriptRunning", out var runningNode) && runningNode.ValueKind == JsonValueKind.True;
        var route = root.TryGetProperty("currentRouteName", out var routeNode) ? routeNode.GetString() ?? string.Empty : string.Empty;
        var hasTransport = root.TryGetProperty("hasTransport", out var transportNode) && transportNode.ValueKind == JsonValueKind.True;
        var distance = root.TryGetProperty("transportDistance", out var distanceNode) && distanceNode.TryGetDouble(out var distanceValue)
            ? distanceValue
            : -1;
        var difficulty = 0;
        var alias = string.Empty;
        var jobType = string.Empty;
        var level = 0;
        var experience = 0L;

        if (root.TryGetProperty("jobOverview", out var jobOverview) && jobOverview.ValueKind == JsonValueKind.Object)
        {
            if (jobOverview.TryGetProperty("difficulty", out var difficultyNode) && difficultyNode.TryGetInt32(out var parsedDifficulty))
                difficulty = parsedDifficulty;
            alias = jobOverview.TryGetProperty("alias", out var aliasNode) ? aliasNode.GetString() ?? string.Empty : string.Empty;
            jobType = jobOverview.TryGetProperty("type", out var typeNode) ? typeNode.GetString() ?? string.Empty : string.Empty;
            if (jobOverview.TryGetProperty("level", out var levelNode) && levelNode.TryGetInt32(out var parsedLevel))
                level = parsedLevel;
            if (jobOverview.TryGetProperty("experience", out var expNode) && expNode.TryGetInt64(out var parsedExp))
                experience = parsedExp;
        }

        if (_runtimeLabel != null)
            _runtimeLabel.Text = $"Script running: {(scriptRunning ? "Yes" : "No")}  |  Route: {route}  |  Transport: {(hasTransport ? $"Yes ({distance:0.0}m)" : "No")}";

        if (_jobAliasValue != null)
            _jobAliasValue.Text = string.IsNullOrWhiteSpace(alias) ? "-" : alias;
        if (_jobTypeValue != null)
            _jobTypeValue.Text = string.IsNullOrWhiteSpace(jobType) ? "None" : jobType;
        if (_jobLevelValue != null)
            _jobLevelValue.Text = level.ToString(CultureInfo.InvariantCulture);
        if (_jobExperienceValue != null)
            _jobExperienceValue.Text = experience.ToString(CultureInfo.InvariantCulture);
        if (_jobDifficultyValue != null)
            _jobDifficultyValue.Text = difficulty.ToString(CultureInfo.InvariantCulture);
        if (_jobRouteValue != null)
            _jobRouteValue.Text = string.IsNullOrWhiteSpace(route) ? "-" : route;
        if (_jobTransportValue != null)
            _jobTransportValue.Text = hasTransport ? $"Yes ({distance:0.0}m)" : "No";

        _routeOverviewRows.Clear();
        if (root.TryGetProperty("routeRows", out var routeRowsNode) && routeRowsNode.ValueKind == JsonValueKind.Array)
        {
            foreach (var row in routeRowsNode.EnumerateArray())
            {
                if (row.ValueKind != JsonValueKind.Object)
                    continue;

                var name = row.TryGetProperty("name", out var nameNode) ? nameNode.GetString() ?? "(unknown)" : "(unknown)";
                var startRegion = row.TryGetProperty("startRegion", out var startNode) ? startNode.ToString() ?? "-" : "-";
                var endRegion = row.TryGetProperty("endRegion", out var endNode) ? endNode.ToString() ?? "-" : "-";
                var steps = row.TryGetProperty("numSteps", out var stepNode) && stepNode.TryGetInt32(out var parsedSteps) ? parsedSteps : 0;
                var missing = row.TryGetProperty("missing", out var missingNode) && missingNode.ValueKind == JsonValueKind.True;

                _routeOverviewRows.Add($"{name}  |  {startRegion} -> {endRegion}  |  Steps: {steps}  |  {(missing ? "Missing" : "OK")}");
            }
        }

        if (_routeOverviewRows.Count == 0)
            _routeOverviewRows.Add("No route script details available.");
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

        TabStripCtrl.SetTabs(new[] { ("route", "Route"), ("settings", "Settings"), ("joboverview", "Job Overview") });
        TabStripCtrl.ActiveTabId = _activeTab;
        TabStripCtrl.TabChanged += tab =>
        {
            _activeTab = tab;
            SyncTabVisibility();
        };
        ContentHost.Children.Clear();

        _routePanel = new StackPanel { Spacing = 10 };
        _runtimeLabel = new TextBlock { Text = "Script running: No", Classes = { "form-label" } };
        _routePanel.Children.Add(_runtimeLabel);

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

        _routePanel.Children.Add(CreateRowControl(_useRouteScriptsCheck, _tracePlayerCheck));
        _routePanel.Children.Add(CreateRow("Trace player name", _tracePlayerNameBox));
        _routePanel.Children.Add(CreateRow("Route list", _routeListCombo));
        _routePanel.Children.Add(CreateRow("Scripts", _scriptsList));
        _routePanel.Children.Add(CreateRow("Script path", _scriptInputBox));
        _routePanel.Children.Add(CreateRowControl(addScriptBtn, removeScriptBtn));
        _routePanel.Children.Add(CreateRowControl(addListBtn, removeListBtn));

        var routeSaveBtn = new Button { Content = "Save Route", Classes = { "primary" }, Width = 140 };
        routeSaveBtn.Click += SaveBtn_Click;
        _routePanel.Children.Add(routeSaveBtn);

        _settingsPanel = new StackPanel { Spacing = 10 };
        _settingsPanel.Children.Add(_runTownScriptCheck);
        _settingsPanel.Children.Add(_waitHunterCheck);
        _settingsPanel.Children.Add(_attackThiefPlayersCheck);
        _settingsPanel.Children.Add(_attackThiefNpcsCheck);
        _settingsPanel.Children.Add(_counterAttackCheck);
        _settingsPanel.Children.Add(_protectTransportCheck);
        _settingsPanel.Children.Add(_castBuffsCheck);
        _settingsPanel.Children.Add(_mountTransportCheck);
        _settingsPanel.Children.Add(CreateRow("Max transport distance", _maxTransportDistanceBox));
        _settingsPanel.Children.Add(_sellGoodsCheck);
        _settingsPanel.Children.Add(_buyGoodsCheck);
        _settingsPanel.Children.Add(CreateRow("Buy goods quantity", _buyGoodsQuantityBox));
        _settingsPanel.Children.Add(CreateRow("Recorder script path", _recorderPathBox));

        var settingsSaveBtn = new Button { Content = "Save Settings", Classes = { "primary" }, Width = 140 };
        settingsSaveBtn.Click += SaveBtn_Click;
        _settingsPanel.Children.Add(settingsSaveBtn);

        _jobOverviewPanel = new StackPanel { Spacing = 8 };
        _jobAliasValue = new TextBlock { Text = "-", Classes = { "form-label" } };
        _jobTypeValue = new TextBlock { Text = "None", Classes = { "form-label" } };
        _jobLevelValue = new TextBlock { Text = "0", Classes = { "form-label" } };
        _jobExperienceValue = new TextBlock { Text = "0", Classes = { "form-label" } };
        _jobDifficultyValue = new TextBlock { Text = "0", Classes = { "form-label" } };
        _jobRouteValue = new TextBlock { Text = "-", Classes = { "form-label" } };
        _jobTransportValue = new TextBlock { Text = "No", Classes = { "form-label" } };
        _routeOverviewList = new ListBox { Height = 220, ItemsSource = _routeOverviewRows };

        _jobOverviewPanel.Children.Add(CreateRow("Alias", _jobAliasValue));
        _jobOverviewPanel.Children.Add(CreateRow("Type", _jobTypeValue));
        _jobOverviewPanel.Children.Add(CreateRow("Level", _jobLevelValue));
        _jobOverviewPanel.Children.Add(CreateRow("Experience", _jobExperienceValue));
        _jobOverviewPanel.Children.Add(CreateRow("Difficulty", _jobDifficultyValue));
        _jobOverviewPanel.Children.Add(CreateRow("Current route", _jobRouteValue));
        _jobOverviewPanel.Children.Add(CreateRow("Transport", _jobTransportValue));
        _jobOverviewPanel.Children.Add(new TextBlock
        {
            Text = "Route Details",
            Classes = { "form-label" },
            Margin = new Thickness(0, 4, 0, 0)
        });
        _jobOverviewPanel.Children.Add(_routeOverviewList);

        ContentHost.Children.Add(_routePanel);
        ContentHost.Children.Add(_settingsPanel);
        ContentHost.Children.Add(_jobOverviewPanel);
        SyncTabVisibility();
    }

    private void SyncTabVisibility()
    {
        if (_routePanel != null)
            _routePanel.IsVisible = _activeTab == "route";
        if (_settingsPanel != null)
            _settingsPanel.IsVisible = _activeTab == "settings";
        if (_jobOverviewPanel != null)
            _jobOverviewPanel.IsVisible = _activeTab == "joboverview";
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
