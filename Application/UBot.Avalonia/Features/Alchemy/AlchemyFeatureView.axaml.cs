using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using global::Avalonia.Layout;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.Alchemy;

public partial class AlchemyFeatureView : UserControl
{
    private sealed class AlchemyItemOption
    {
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public override string ToString() => Label;
    }

    private static readonly string[] BlueKeys =
    {
        "str", "int", "immortal", "steady", "lucky", "durability", "attackRate", "blockRate", "availableSlots"
    };

    private static readonly string[] StatKeys =
    {
        "critical", "magAtk", "phyAtk", "attackRate", "magReinforce", "phyReinforce", "durability"
    };

    private PluginViewModelBase? _vm;
    private AppState? _state;
    private bool _built;
    private bool _syncing;

    private ComboBox? _modeCombo;
    private ComboBox? _itemCombo;
    private TextBox? _maxEnhancementBox;
    private ComboBox? _elixirTypeCombo;
    private CheckBox? _stopNoPowderCheck;
    private CheckBox? _luckyStoneCheck;
    private CheckBox? _immortalStoneCheck;
    private CheckBox? _astralStoneCheck;
    private CheckBox? _steadyStoneCheck;
    private TextBlock? _runtimeLabel;
    private ListBox? _bluesList;
    private ListBox? _statsList;

    private readonly Dictionary<string, CheckBox> _blueEnabledChecks = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TextBox> _blueMaxBoxes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CheckBox> _statEnabledChecks = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ComboBox> _statTargetCombos = new(StringComparer.OrdinalIgnoreCase);

    private readonly List<AlchemyItemOption> _itemOptions = new();

    public AlchemyFeatureView()
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
        if (moduleState.ValueKind == JsonValueKind.Object && moduleState.TryGetProperty("alchemy", out var alchemyNode))
            root = alchemyNode;
        if (root.ValueKind != JsonValueKind.Object)
            return;

        if (_runtimeLabel != null)
        {
            var powder = ReadInt(root, "luckyPowderCount");
            var luckyStone = ReadInt(root, "luckyStoneCount");
            var immortal = ReadInt(root, "immortalStoneCount");
            var astral = ReadInt(root, "astralStoneCount");
            var steady = ReadInt(root, "steadyStoneCount");
            _runtimeLabel.Text = $"Powder:{powder}  Lucky:{luckyStone}  Immortal:{immortal}  Astral:{astral}  Steady:{steady}";
        }

        if (root.TryGetProperty("itemsCatalog", out var itemsCatalog) && itemsCatalog.ValueKind == JsonValueKind.Array)
        {
            var selectedCode = (_itemCombo?.SelectedItem as AlchemyItemOption)?.Code
                ?? _vm?.TextCfg("alchemyItemCode", string.Empty)
                ?? string.Empty;
            _itemOptions.Clear();
            foreach (var entry in itemsCatalog.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                    continue;
                var code = entry.TryGetProperty("codeName", out var codeNode) ? codeNode.GetString() ?? string.Empty : string.Empty;
                var name = entry.TryGetProperty("name", out var nameNode) ? nameNode.GetString() ?? code : code;
                if (string.IsNullOrWhiteSpace(code))
                    continue;
                _itemOptions.Add(new AlchemyItemOption { Code = code, Label = name });
            }

            if (_itemCombo != null)
            {
                _itemCombo.ItemsSource = null;
                _itemCombo.ItemsSource = _itemOptions;
                _itemCombo.SelectedItem = _itemOptions.FirstOrDefault(x => x.Code.Equals(selectedCode, StringComparison.OrdinalIgnoreCase))
                    ?? _itemOptions.FirstOrDefault();
            }
        }

        if (_bluesList != null)
            _bluesList.ItemsSource = ParseRows(root, "alchemyBlues");
        if (_statsList != null)
            _statsList.ItemsSource = ParseRows(root, "alchemyStats");
    }

    private async System.Threading.Tasks.Task LoadFromConfigAsync()
    {
        if (_vm == null)
            return;

        await _vm.LoadConfigAsync();
        _syncing = true;
        try
        {
            _modeCombo!.SelectedItem = _vm.TextCfg("alchemyMode", "enhance");
            _maxEnhancementBox!.Text = ((int)Math.Round(_vm.NumCfg("alchemyMaxEnhancement", 0))).ToString(CultureInfo.InvariantCulture);
            _elixirTypeCombo!.SelectedItem = _vm.TextCfg("alchemyElixirType", "weapon");
            _stopNoPowderCheck!.IsChecked = _vm.BoolCfg("stopAtNoPowder", true);
            _luckyStoneCheck!.IsChecked = _vm.BoolCfg("useLuckyStone", false);
            _immortalStoneCheck!.IsChecked = _vm.BoolCfg("useImmortalStone", false);
            _astralStoneCheck!.IsChecked = _vm.BoolCfg("useAstralStone", false);
            _steadyStoneCheck!.IsChecked = _vm.BoolCfg("useSteadyStone", false);

            foreach (var key in BlueKeys)
            {
                if (_blueEnabledChecks.TryGetValue(key, out var enabledCheck))
                    enabledCheck.IsChecked = _vm.BoolCfg($"alchemyBlueEnabled_{key}", false);
                if (_blueMaxBoxes.TryGetValue(key, out var maxBox))
                    maxBox.Text = ((int)Math.Round(_vm.NumCfg($"alchemyBlueMax_{key}", 0))).ToString(CultureInfo.InvariantCulture);
            }

            foreach (var key in StatKeys)
            {
                if (_statEnabledChecks.TryGetValue(key, out var enabledCheck))
                    enabledCheck.IsChecked = _vm.BoolCfg($"alchemyStatEnabled_{key}", false);
                if (_statTargetCombos.TryGetValue(key, out var targetCombo))
                    targetCombo.SelectedItem = _vm.TextCfg($"alchemyStatTarget_{key}", "off");
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

        TabStripCtrl.SetTabs(new[] { ("alchemy", "Alchemy") });
        TabStripCtrl.ActiveTabId = "alchemy";
        ContentHost.Children.Clear();

        var layout = new StackPanel { Spacing = 10 };

        _runtimeLabel = new TextBlock
        {
            Text = "Powder:0  Lucky:0  Immortal:0  Astral:0  Steady:0",
            Classes = { "form-label" }
        };
        layout.Children.Add(_runtimeLabel);

        _modeCombo = new ComboBox { ItemsSource = new[] { "enhance", "blues", "stats" }, SelectedItem = "enhance", Width = 220 };
        _itemCombo = new ComboBox { Width = 520 };
        _maxEnhancementBox = CreateTextBox("0", 120);
        _elixirTypeCombo = new ComboBox { ItemsSource = new[] { "weapon", "shield", "protector", "accessory" }, SelectedItem = "weapon", Width = 220 };
        _stopNoPowderCheck = CreateCheck("Stop at no powder");
        _luckyStoneCheck = CreateCheck("Use lucky stone");
        _immortalStoneCheck = CreateCheck("Use immortal stone");
        _astralStoneCheck = CreateCheck("Use astral stone");
        _steadyStoneCheck = CreateCheck("Use steady stone");

        layout.Children.Add(CreateRow("Mode", _modeCombo));
        layout.Children.Add(CreateRow("Item", _itemCombo));
        layout.Children.Add(CreateRow("Max enhancement", _maxEnhancementBox));
        layout.Children.Add(CreateRow("Elixir type", _elixirTypeCombo));
        layout.Children.Add(_stopNoPowderCheck);
        layout.Children.Add(_luckyStoneCheck);
        layout.Children.Add(_immortalStoneCheck);
        layout.Children.Add(_astralStoneCheck);
        layout.Children.Add(_steadyStoneCheck);

        var bluesPanel = new StackPanel { Spacing = 6 };
        bluesPanel.Children.Add(new TextBlock { Text = "Blue targets", Classes = { "form-label" } });
        foreach (var key in BlueKeys)
        {
            var enabled = CreateCheck(key);
            var maxBox = CreateTextBox("0", 90);
            _blueEnabledChecks[key] = enabled;
            _blueMaxBoxes[key] = maxBox;
            bluesPanel.Children.Add(CreateRowControl(enabled, maxBox));
        }
        layout.Children.Add(bluesPanel);

        var statsPanel = new StackPanel { Spacing = 6 };
        statsPanel.Children.Add(new TextBlock { Text = "Stat targets", Classes = { "form-label" } });
        foreach (var key in StatKeys)
        {
            var enabled = CreateCheck(key);
            var target = new ComboBox { ItemsSource = new[] { "off", "low", "medium", "high", "max" }, SelectedItem = "off", Width = 120 };
            _statEnabledChecks[key] = enabled;
            _statTargetCombos[key] = target;
            statsPanel.Children.Add(CreateRowControl(enabled, target));
        }
        layout.Children.Add(statsPanel);

        _bluesList = new ListBox { Height = 120 };
        _statsList = new ListBox { Height = 120 };
        layout.Children.Add(CreateRow("Blue status", _bluesList));
        layout.Children.Add(CreateRow("Stat status", _statsList));

        var saveBtn = new Button { Content = "Save", Classes = { "primary" }, Width = 140 };
        saveBtn.Click += SaveBtn_Click;
        layout.Children.Add(saveBtn);

        ContentHost.Children.Add(layout);
    }

    private async void SaveBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null || _syncing)
            return;

        var patch = new Dictionary<string, object?>
        {
            ["alchemyMode"] = _modeCombo?.SelectedItem?.ToString() ?? "enhance",
            ["alchemyItemCode"] = (_itemCombo?.SelectedItem as AlchemyItemOption)?.Code ?? string.Empty,
            ["alchemyMaxEnhancement"] = ParseInt(_maxEnhancementBox?.Text, 0),
            ["alchemyElixirType"] = _elixirTypeCombo?.SelectedItem?.ToString() ?? "weapon",
            ["stopAtNoPowder"] = _stopNoPowderCheck?.IsChecked == true,
            ["useLuckyStone"] = _luckyStoneCheck?.IsChecked == true,
            ["useImmortalStone"] = _immortalStoneCheck?.IsChecked == true,
            ["useAstralStone"] = _astralStoneCheck?.IsChecked == true,
            ["useSteadyStone"] = _steadyStoneCheck?.IsChecked == true
        };

        foreach (var key in BlueKeys)
        {
            patch[$"alchemyBlueEnabled_{key}"] = _blueEnabledChecks.TryGetValue(key, out var enabledCheck) && enabledCheck.IsChecked == true;
            patch[$"alchemyBlueMax_{key}"] = _blueMaxBoxes.TryGetValue(key, out var maxBox) ? ParseInt(maxBox.Text, 0) : 0;
        }

        foreach (var key in StatKeys)
        {
            patch[$"alchemyStatEnabled_{key}"] = _statEnabledChecks.TryGetValue(key, out var enabledCheck) && enabledCheck.IsChecked == true;
            patch[$"alchemyStatTarget_{key}"] = _statTargetCombos.TryGetValue(key, out var targetCombo)
                ? targetCombo.SelectedItem?.ToString() ?? "off"
                : "off";
        }

        await _vm.PatchConfigAsync(patch);
    }

    private static List<string> ParseRows(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var rowsNode) || rowsNode.ValueKind != JsonValueKind.Array)
            return new List<string>();

        var result = new List<string>();
        foreach (var row in rowsNode.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object)
                continue;
            var name = row.TryGetProperty("name", out var nameNode) ? nameNode.GetString() ?? string.Empty : string.Empty;
            var value = row.TryGetProperty("value", out var valueNode) ? valueNode.GetString() ?? string.Empty : string.Empty;
            var stones = row.TryGetProperty("stoneCount", out var stonesNode) && stonesNode.TryGetInt32(out var stonesValue) ? stonesValue : 0;
            result.Add($"{name} => {value} (stones: {stones})");
        }

        return result;
    }

    private static int ReadInt(JsonElement root, string key)
    {
        return root.TryGetProperty(key, out var node) && node.TryGetInt32(out var value) ? value : 0;
    }

    private static int ParseInt(string? value, int fallback)
    {
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
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

    private static CheckBox CreateCheck(string text)
    {
        return new CheckBox
        {
            Content = text,
            Classes = { "check" }
        };
    }

    private static TextBox CreateTextBox(string value, double width = 220)
    {
        return new TextBox
        {
            Text = value,
            Width = width
        };
    }
}
