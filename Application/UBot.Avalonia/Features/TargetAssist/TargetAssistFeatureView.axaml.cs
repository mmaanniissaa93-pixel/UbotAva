using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using global::Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.TargetAssist;

public partial class TargetAssistFeatureView : UserControl
{
    private PluginViewModelBase? _vm;
    private AppState? _state;

    private CheckBox? _enabledCheck;
    private TextBox? _maxRangeBox;
    private CheckBox? _includeDeadCheck;
    private CheckBox? _ignoreSnowCheck;
    private CheckBox? _ignoreBloodyCheck;
    private CheckBox? _onlyCustomCheck;
    private ComboBox? _roleModeCombo;
    private TextBox? _cycleKeyBox;
    private TextBox? _ignoredGuildsBox;
    private TextBox? _customPlayersBox;
    private TextBlock? _runtimeLabel;
    private bool _built;
    private bool _syncing;

    public TargetAssistFeatureView()
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
        if (moduleState.ValueKind == JsonValueKind.Object && moduleState.TryGetProperty("targetAssist", out var targetAssistNode))
            root = targetAssistNode;
        if (root.ValueKind != JsonValueKind.Object)
            return;

        var candidates = root.TryGetProperty("candidateCount", out var candidatesNode) && candidatesNode.TryGetInt32(out var candidatesValue)
            ? candidatesValue
            : 0;
        var nearest = root.TryGetProperty("nearestTargetName", out var nearestNode) ? nearestNode.GetString() ?? string.Empty : string.Empty;
        var distance = root.TryGetProperty("nearestTargetDistance", out var distanceNode) && distanceNode.TryGetDouble(out var distanceValue)
            ? distanceValue
            : -1;

        if (_runtimeLabel != null)
        {
            _runtimeLabel.Text = candidates <= 0
                ? "No target candidates in range."
                : $"Candidates: {candidates}  |  Nearest: {nearest} {(distance >= 0 ? $"({distance:0.0}m)" : string.Empty)}";
        }
    }

    private async System.Threading.Tasks.Task LoadFromConfigAsync()
    {
        if (_vm == null)
            return;

        await _vm.LoadConfigAsync();
        _syncing = true;
        try
        {
            if (_enabledCheck != null)
                _enabledCheck.IsChecked = _vm.BoolCfg("enabled", false);
            if (_maxRangeBox != null)
                _maxRangeBox.Text = _vm.NumCfg("maxRange", 40).ToString("0.0");
            if (_includeDeadCheck != null)
                _includeDeadCheck.IsChecked = _vm.BoolCfg("includeDeadTargets", false);
            if (_ignoreSnowCheck != null)
                _ignoreSnowCheck.IsChecked = _vm.BoolCfg("ignoreSnowShieldTargets", true);
            if (_ignoreBloodyCheck != null)
                _ignoreBloodyCheck.IsChecked = _vm.BoolCfg("ignoreBloodyStormTargets", false);
            if (_onlyCustomCheck != null)
                _onlyCustomCheck.IsChecked = _vm.BoolCfg("onlyCustomPlayers", false);
            if (_cycleKeyBox != null)
                _cycleKeyBox.Text = _vm.TextCfg("targetCycleKey", "Oem3");
            if (_ignoredGuildsBox != null)
                _ignoredGuildsBox.Text = string.Join(", ", ToStringList(_vm.ObjCfg("ignoredGuilds")));
            if (_customPlayersBox != null)
                _customPlayersBox.Text = string.Join(", ", ToStringList(_vm.ObjCfg("customPlayers")));
            if (_roleModeCombo != null)
            {
                var role = _vm.TextCfg("roleMode", "civil").ToLowerInvariant();
                _roleModeCombo.SelectedItem = role is "thief" or "huntertrader" or "hunter_trader" or "hunter-trader"
                    ? (role.StartsWith("hunter") ? "hunterTrader" : "thief")
                    : "civil";
            }
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
        TabStripCtrl.SetTabs(new[] { ("main", "Target Assist") });
        TabStripCtrl.ActiveTabId = "main";
        ContentHost.Children.Clear();

        var layout = new StackPanel { Spacing = 10 };
        _runtimeLabel = new TextBlock { Text = "No target candidates in range.", Classes = { "form-label" } };
        layout.Children.Add(_runtimeLabel);

        _enabledCheck = CreateCheck("Enable target assist");
        _maxRangeBox = CreateTextBox("40");
        _includeDeadCheck = CreateCheck("Include dead targets");
        _ignoreSnowCheck = CreateCheck("Ignore snow shield targets");
        _ignoreBloodyCheck = CreateCheck("Ignore bloody storm targets");
        _onlyCustomCheck = CreateCheck("Only custom players");
        _cycleKeyBox = CreateTextBox("Oem3");
        _ignoredGuildsBox = CreateTextBox(string.Empty);
        _customPlayersBox = CreateTextBox(string.Empty);

        _roleModeCombo = new ComboBox
        {
            ItemsSource = new[] { "civil", "thief", "hunterTrader" },
            SelectedItem = "civil",
            Width = 220
        };

        layout.Children.Add(CreateRow("Enabled", _enabledCheck));
        layout.Children.Add(CreateRow("Max range", _maxRangeBox));
        layout.Children.Add(CreateRow("Role mode", _roleModeCombo));
        layout.Children.Add(CreateRow("Target cycle key", _cycleKeyBox));
        layout.Children.Add(CreateRow("Ignored guilds (comma separated)", _ignoredGuildsBox));
        layout.Children.Add(CreateRow("Custom players (comma separated)", _customPlayersBox));
        layout.Children.Add(_includeDeadCheck);
        layout.Children.Add(_ignoreSnowCheck);
        layout.Children.Add(_ignoreBloodyCheck);
        layout.Children.Add(_onlyCustomCheck);

        var saveBtn = new Button
        {
            Content = "Save",
            Classes = { "primary" },
            Width = 140
        };
        saveBtn.Click += SaveBtn_Click;
        layout.Children.Add(saveBtn);

        ContentHost.Children.Add(layout);
    }

    private async void SaveBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null || _syncing)
            return;

        var maxRange = 40d;
        _ = double.TryParse(_maxRangeBox?.Text, out maxRange);
        maxRange = Math.Clamp(maxRange, 5d, 400d);

        var roleMode = _roleModeCombo?.SelectedItem?.ToString() ?? "civil";
        var patch = new Dictionary<string, object?>
        {
            ["enabled"] = _enabledCheck?.IsChecked == true,
            ["maxRange"] = maxRange,
            ["includeDeadTargets"] = _includeDeadCheck?.IsChecked == true,
            ["ignoreSnowShieldTargets"] = _ignoreSnowCheck?.IsChecked == true,
            ["ignoreBloodyStormTargets"] = _ignoreBloodyCheck?.IsChecked == true,
            ["onlyCustomPlayers"] = _onlyCustomCheck?.IsChecked == true,
            ["roleMode"] = roleMode,
            ["targetCycleKey"] = (_cycleKeyBox?.Text ?? "Oem3").Trim(),
            ["ignoredGuilds"] = ParseCommaList(_ignoredGuildsBox?.Text),
            ["customPlayers"] = ParseCommaList(_customPlayersBox?.Text)
        };

        await _vm.PatchConfigAsync(patch);
    }

    private static StackPanel CreateRow(string label, Control control)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        panel.Children.Add(new TextBlock
        {
            Text = label,
            Width = 260,
            VerticalAlignment = VerticalAlignment.Center
        });
        panel.Children.Add(control);
        return panel;
    }

    private static CheckBox CreateCheck(string content)
    {
        return new CheckBox
        {
            Content = content,
            Classes = { "check" }
        };
    }

    private static TextBox CreateTextBox(string value)
    {
        return new TextBox
        {
            Text = value,
            Width = 260
        };
    }

    private static List<string> ParseCommaList(string? raw)
    {
        return (raw ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> ToStringList(object? raw)
    {
        if (raw is IEnumerable<object> objValues)
            return objValues.Select(x => x?.ToString() ?? string.Empty).Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        if (raw is IEnumerable<string> strValues)
            return strValues.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        return new List<string>();
    }
}
