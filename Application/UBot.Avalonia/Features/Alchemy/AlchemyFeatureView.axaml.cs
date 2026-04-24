using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using UBot.Avalonia.Controls;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.Alchemy;

public sealed class AlchemyLogRow
{
    public string Item { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public partial class AlchemyFeatureView : UserControl
{
    private sealed class AlchemyItemOption
    {
        public string Code { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int Degree { get; set; }
        public int OptLevel { get; set; }
        public int Slot { get; set; }
    }

    private sealed class AlchemyRow
    {
        public string Key { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = "0";
        public int Current { get; set; }
        public int Max { get; set; }
        public int StoneCount { get; set; }
    }

    private static readonly (string Key, string Label)[] BlueMeta =
    {
        ("str", "Str"),
        ("int", "Int"),
        ("immortal", "Immortal"),
        ("steady", "Steady"),
        ("lucky", "Lucky"),
        ("durability", "Durability"),
        ("attackRate", "Attack rate"),
        ("blockRate", "Blocking rate"),
        ("availableSlots", "Available numb...")
    };

    private static readonly (string Key, string Label)[] StatMeta =
    {
        ("critical", "Critical"),
        ("magAtk", "Mag. atk. pwr."),
        ("phyAtk", "Phy. atk. pwr."),
        ("attackRate", "Attack rate"),
        ("magReinforce", "Mag. reinforce"),
        ("phyReinforce", "Phy. reinforce"),
        ("durability", "Durability")
    };

    private static readonly (string Value, string Label)[] ElixirOptions =
    {
        ("weapon", "Weapon Elixir"),
        ("shield", "Shield Elixir"),
        ("protector", "Protector Elixir"),
        ("accessory", "Accessory Elixir")
    };

    private static readonly (string Value, string Label)[] StatTargetOptions =
    {
        ("off", "Disabled"),
        ("low", "Low"),
        ("medium", "Medium"),
        ("high", "High"),
        ("max", "Max")
    };

    private PluginViewModelBase? _vm;
    private AppState? _state;
    private bool _syncing;
    private bool _botRunning;
    private bool _alchemySelected;

    private string _currentMode = "enhance";
    private string _leftSummaryMode = "blues";
    private string _selectedItemCode = string.Empty;

    private readonly List<AlchemyItemOption> _itemOptions = new();
    private readonly Dictionary<string, AlchemyRow> _blueRows = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, AlchemyRow> _statRows = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, CheckBox> _blueToggles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TextBox> _blueMaxBoxes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TextBlock> _blueCurrentTexts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, TextBlock> _blueStoneTexts = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, CheckBox> _statToggles = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CustomSelect> _statTargets = new(StringComparer.OrdinalIgnoreCase);

    private readonly ObservableCollection<AlchemyLogRow> _logRows = new();

    public AlchemyFeatureView()
    {
        InitializeComponent();
        BuildStaticUi();
        WireStaticEvents();
    }

    public void Initialize(PluginViewModelBase vm, AppState state)
    {
        _vm = vm;
        _state = state;
        _ = LoadFromConfigAsync();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (_vm != null && _itemOptions.Count == 0)
            _ = LoadFromConfigAsync();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        ReleaseFeatureCache();
        base.OnDetachedFromVisualTree(e);
    }

    public void UpdateFromState(JsonElement moduleState)
    {
        var root = moduleState;
        if (moduleState.ValueKind == JsonValueKind.Object && moduleState.TryGetProperty("alchemy", out var alchemyNode))
            root = alchemyNode;
        if (root.ValueKind != JsonValueKind.Object)
            return;

        _botRunning = moduleState.TryGetProperty("botRunning", out var botRunningNode) && botRunningNode.ValueKind == JsonValueKind.True;
        _alchemySelected = root.TryGetProperty("selected", out var selectedNode) && selectedNode.ValueKind == JsonValueKind.True;
        UpdateStartButtonVisual();

        UpdateItemCatalog(root);
        UpdateSelectedItemDetails(root);
        UpdateResourceCounts(root);

        _blueRows.Clear();
        foreach (var row in ParseRows(root, "alchemyBlues"))
            _blueRows[row.Key] = row;

        _statRows.Clear();
        foreach (var row in ParseRows(root, "alchemyStats"))
            _statRows[row.Key] = row;

        RenderLeftSummary();
        RenderBluesRightValues();
        UpdateLogs();
    }

    private void BuildStaticUi()
    {
        ItemSelect.Options = new List<SelectOption>();
        ElixirSelect.Options = ElixirOptions.Select(option => new SelectOption(option.Value, option.Label)).ToList();

        BuildBlueRowsUi();
        BuildStatRowsUi();

        LogList.ItemsSource = _logRows;
        ApplyModeVisuals();
        ApplyLeftSummaryVisuals();
        UpdateStartButtonVisual();
    }

    private void WireStaticEvents()
    {
        ItemSelect.SelectionChanged += ItemSelect_Changed;
        ElixirSelect.SelectionChanged += ElixirSelect_Changed;

        ModeEnhanceBtn.Click += (_, _) => SetMode("enhance", patch: true);
        ModeBluesBtn.Click += (_, _) => SetMode("blues", patch: true);
        ModeStatsBtn.Click += (_, _) => SetMode("stats", patch: true);

        LeftBluesBtn.Click += (_, _) => SetLeftSummaryMode("blues");
        LeftStatsBtn.Click += (_, _) => SetLeftSummaryMode("stats");

        MaxMinusBtn.Click += MaxMinusBtn_Click;
        MaxPlusBtn.Click += MaxPlusBtn_Click;
        MaxEnhancementBox.LostFocus += MaxEnhancementBox_LostFocus;

        StopPowderToggle.IsCheckedChanged += (_, _) => _ = PatchBoolAsync("stopAtNoPowder", StopPowderToggle.IsChecked == true);
        LuckyStoneToggle.IsCheckedChanged += (_, _) => _ = PatchBoolAsync("useLuckyStone", LuckyStoneToggle.IsChecked == true);
        ImmortalStoneToggle.IsCheckedChanged += (_, _) => _ = PatchBoolAsync("useImmortalStone", ImmortalStoneToggle.IsChecked == true);
        AstralStoneToggle.IsCheckedChanged += (_, _) => _ = PatchBoolAsync("useAstralStone", AstralStoneToggle.IsChecked == true);
        SteadyStoneToggle.IsCheckedChanged += (_, _) => _ = PatchBoolAsync("useSteadyStone", SteadyStoneToggle.IsChecked == true);
        AlchemyStartBtn.Click += AlchemyStartBtn_Click;
    }

    private void BuildBlueRowsUi()
    {
        BluesRowsHost.Children.Clear();
        _blueToggles.Clear();
        _blueMaxBoxes.Clear();
        _blueCurrentTexts.Clear();
        _blueStoneTexts.Clear();

        foreach (var (key, label) in BlueMeta)
        {
            var rowBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#214B6E95")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(6, 6, 0, 6)
            };

            var rowGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,120,120,120") };

            var optionHost = new Grid { ColumnDefinitions = new ColumnDefinitions("24,*") };
            var toggle = new CheckBox
            {
                Content = string.Empty,
                Classes = { "alchemy-check" },
                VerticalAlignment = VerticalAlignment.Center,
                Tag = key
            };
            toggle.IsCheckedChanged += BlueToggle_IsCheckedChanged;

            var nameText = new TextBlock
            {
                Text = label,
                Classes = { "label" },
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(toggle, 0);
            Grid.SetColumn(nameText, 1);
            optionHost.Children.Add(toggle);
            optionHost.Children.Add(nameText);

            var currentText = new TextBlock
            {
                Text = "0",
                Classes = { "label" },
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            var maxBox = new TextBox
            {
                Text = "0",
                Classes = { "alchemy-num" },
                Tag = key,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            maxBox.LostFocus += BlueMaxBox_LostFocus;

            var stonesText = new TextBlock
            {
                Text = "0",
                Classes = { "label" },
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Grid.SetColumn(optionHost, 0);
            Grid.SetColumn(currentText, 1);
            Grid.SetColumn(maxBox, 2);
            Grid.SetColumn(stonesText, 3);

            rowGrid.Children.Add(optionHost);
            rowGrid.Children.Add(currentText);
            rowGrid.Children.Add(maxBox);
            rowGrid.Children.Add(stonesText);

            rowBorder.Child = rowGrid;
            BluesRowsHost.Children.Add(rowBorder);

            _blueToggles[key] = toggle;
            _blueMaxBoxes[key] = maxBox;
            _blueCurrentTexts[key] = currentText;
            _blueStoneTexts[key] = stonesText;
        }
    }

    private void BuildStatRowsUi()
    {
        StatsRowsHost.Children.Clear();
        _statToggles.Clear();
        _statTargets.Clear();

        foreach (var (key, label) in StatMeta)
        {
            var rowBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#214B6E95")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(6, 6, 0, 6)
            };

            var rowGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("250,*") };

            var optionHost = new Grid { ColumnDefinitions = new ColumnDefinitions("24,*") };
            var toggle = new CheckBox
            {
                Content = string.Empty,
                Classes = { "alchemy-check" },
                VerticalAlignment = VerticalAlignment.Center,
                Tag = key
            };
            toggle.IsCheckedChanged += StatToggle_IsCheckedChanged;

            var nameText = new TextBlock
            {
                Text = label,
                Classes = { "label" },
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center
            };

            Grid.SetColumn(toggle, 0);
            Grid.SetColumn(nameText, 1);
            optionHost.Children.Add(toggle);
            optionHost.Children.Add(nameText);

            var targetSelect = new CustomSelect
            {
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Left,
                Width = 160,
                Tag = key,
                Options = StatTargetOptions.Select(option => new SelectOption(option.Value, option.Label)).ToList(),
                SelectedValue = "off"
            };
            var statKey = key;
            targetSelect.SelectionChanged += value => StatTarget_SelectionChanged(statKey, value);

            Grid.SetColumn(optionHost, 0);
            Grid.SetColumn(targetSelect, 1);

            rowGrid.Children.Add(optionHost);
            rowGrid.Children.Add(targetSelect);
            rowBorder.Child = rowGrid;
            StatsRowsHost.Children.Add(rowBorder);

            _statToggles[key] = toggle;
            _statTargets[key] = targetSelect;
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
            SetMode(_vm.TextCfg("alchemyMode", "enhance"), patch: false);
            _selectedItemCode = _vm.TextCfg("alchemyItemCode", string.Empty);

            MaxEnhancementBox.Text = ClampInt(_vm.NumCfg("alchemyMaxEnhancement", 0), 0, 15).ToString(CultureInfo.InvariantCulture);
            ElixirSelect.SelectedValue = NormalizeElixir(_vm.TextCfg("alchemyElixirType", "weapon"));

            StopPowderToggle.IsChecked = _vm.BoolCfg("stopAtNoPowder", true);
            LuckyStoneToggle.IsChecked = _vm.BoolCfg("useLuckyStone", false);
            ImmortalStoneToggle.IsChecked = _vm.BoolCfg("useImmortalStone", false);
            AstralStoneToggle.IsChecked = _vm.BoolCfg("useAstralStone", false);
            SteadyStoneToggle.IsChecked = _vm.BoolCfg("useSteadyStone", false);

            foreach (var (key, _) in BlueMeta)
            {
                if (_blueToggles.TryGetValue(key, out var toggle))
                    toggle.IsChecked = _vm.BoolCfg($"alchemyBlueEnabled_{key}", false);

                if (_blueMaxBoxes.TryGetValue(key, out var maxBox))
                    maxBox.Text = Math.Max(0, (int)Math.Round(_vm.NumCfg($"alchemyBlueMax_{key}", 0))).ToString(CultureInfo.InvariantCulture);
            }

            foreach (var (key, _) in StatMeta)
            {
                if (_statToggles.TryGetValue(key, out var toggle))
                    toggle.IsChecked = _vm.BoolCfg($"alchemyStatEnabled_{key}", false);

                if (_statTargets.TryGetValue(key, out var targetSelect))
                    targetSelect.SelectedValue = NormalizeStatTarget(_vm.TextCfg($"alchemyStatTarget_{key}", "off"));
            }
        }
        finally
        {
            _syncing = false;
        }

        RenderBluesRightValues();
        RenderLeftSummary();
    }

    private void UpdateItemCatalog(JsonElement root)
    {
        if (!root.TryGetProperty("itemsCatalog", out var itemsCatalog) || itemsCatalog.ValueKind != JsonValueKind.Array)
            return;

        _itemOptions.Clear();
        foreach (var entry in itemsCatalog.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object)
                continue;

            var code = entry.TryGetProperty("codeName", out var codeNode) ? codeNode.GetString() ?? string.Empty : string.Empty;
            var name = entry.TryGetProperty("name", out var nameNode) ? nameNode.GetString() ?? code : code;
            if (string.IsNullOrWhiteSpace(code))
                continue;

            _itemOptions.Add(new AlchemyItemOption
            {
                Code = code,
                Label = name,
                Degree = ReadInt(entry, "degree"),
                OptLevel = ReadInt(entry, "optLevel"),
                Slot = ReadInt(entry, "slot")
            });
        }

        var options = _itemOptions
            .Select(item => new SelectOption(item.Code, item.Label))
            .Cast<SelectOption>()
            .ToList();

        _syncing = true;
        try
        {
            ItemSelect.Options = options;

            var stateCode = root.TryGetProperty("selectedItem", out var selectedNode) && selectedNode.ValueKind == JsonValueKind.Object
                ? (selectedNode.TryGetProperty("codeName", out var stateCodeNode) ? stateCodeNode.GetString() ?? string.Empty : string.Empty)
                : string.Empty;

            var selectedCode = !string.IsNullOrWhiteSpace(_selectedItemCode) ? _selectedItemCode : stateCode;
            if (string.IsNullOrWhiteSpace(selectedCode) && _itemOptions.Count > 0)
                selectedCode = _itemOptions[0].Code;

            _selectedItemCode = selectedCode;
            ItemSelect.SelectedValue = selectedCode;
        }
        finally
        {
            _syncing = false;
        }
    }

    private void UpdateSelectedItemDetails(JsonElement root)
    {
        var degree = 0;
        var enhancement = 0;

        if (root.TryGetProperty("selectedItem", out var selectedNode) && selectedNode.ValueKind == JsonValueKind.Object)
        {
            degree = ReadInt(selectedNode, "degree");
            enhancement = ReadInt(selectedNode, "optLevel");
        }

        DegreeValueText.Text = degree.ToString(CultureInfo.InvariantCulture);
        EnhancementValueText.Text = $"+{enhancement}";
    }

    private void UpdateResourceCounts(JsonElement root)
    {
        LuckyPowderCountText.Text = $"x{ReadInt(root, "luckyPowderCount")}";
        LuckyStoneCountText.Text = $"x{ReadInt(root, "luckyStoneCount")}";
        ImmortalStoneCountText.Text = $"x{ReadInt(root, "immortalStoneCount")}";
        AstralStoneCountText.Text = $"x{ReadInt(root, "astralStoneCount")}";
        SteadyStoneCountText.Text = $"x{ReadInt(root, "steadyStoneCount")}";
    }

    private void RenderLeftSummary()
    {
        LeftRowsHost.Children.Clear();

        var useStats = string.Equals(_leftSummaryMode, "stats", StringComparison.OrdinalIgnoreCase);
        LeftTableNameHeader.Text = useStats ? "STAT" : "BLUE";
        LeftTableValueHeader.Text = "VALUE";

        var rows = useStats
            ? StatMeta.Select(meta => _statRows.TryGetValue(meta.Key, out var row) ? row : new AlchemyRow { Name = meta.Label, Value = "0" })
            : BlueMeta.Select(meta => _blueRows.TryGetValue(meta.Key, out var row) ? row : new AlchemyRow { Name = meta.Label, Value = "0" });

        foreach (var row in rows)
        {
            var border = new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#214B6E95")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 5)
            };

            var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,90") };
            var name = new TextBlock
            {
                Text = row.Name,
                Classes = { "label" },
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            var value = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(row.Value) ? "0" : row.Value,
                Classes = { "label" },
                FontWeight = FontWeight.SemiBold
            };

            Grid.SetColumn(name, 0);
            Grid.SetColumn(value, 1);
            grid.Children.Add(name);
            grid.Children.Add(value);
            border.Child = grid;
            LeftRowsHost.Children.Add(border);
        }
    }

    private void RenderBluesRightValues()
    {
        foreach (var (key, _) in BlueMeta)
        {
            if (_blueCurrentTexts.TryGetValue(key, out var currentText))
            {
                currentText.Text = _blueRows.TryGetValue(key, out var row)
                    ? row.Current.ToString(CultureInfo.InvariantCulture)
                    : "0";
            }

            if (_blueStoneTexts.TryGetValue(key, out var stonesText))
            {
                stonesText.Text = _blueRows.TryGetValue(key, out var row)
                    ? row.StoneCount.ToString(CultureInfo.InvariantCulture)
                    : "0";
            }
        }
    }

    private void SetMode(string mode, bool patch)
    {
        _currentMode = NormalizeMode(mode);
        ApplyModeVisuals();

        if (patch)
            _ = PatchAsync("alchemyMode", _currentMode);
    }

    private void ApplyModeVisuals()
    {
        SetTabActive(ModeEnhanceBtn, _currentMode == "enhance");
        SetTabActive(ModeBluesBtn, _currentMode == "blues");
        SetTabActive(ModeStatsBtn, _currentMode == "stats");

        EnhancePanel.IsVisible = _currentMode == "enhance";
        BluesPanel.IsVisible = _currentMode == "blues";
        StatsPanel.IsVisible = _currentMode == "stats";
    }

    private void SetLeftSummaryMode(string mode)
    {
        _leftSummaryMode = string.Equals(mode, "stats", StringComparison.OrdinalIgnoreCase) ? "stats" : "blues";
        ApplyLeftSummaryVisuals();
        RenderLeftSummary();
    }

    private void ApplyLeftSummaryVisuals()
    {
        SetTabActive(LeftBluesBtn, _leftSummaryMode == "blues");
        SetTabActive(LeftStatsBtn, _leftSummaryMode == "stats");
    }

    private static void SetTabActive(Button button, bool active)
    {
        button.Classes.Remove("active");
        if (active)
            button.Classes.Add("active");
    }

    private void UpdateLogs()
    {
        if (_state == null)
            return;

        var filtered = _state.LogLines
            .Where(line => line.Contains("[Alchemy]", StringComparison.OrdinalIgnoreCase))
            .Take(120)
            .Select(line => new AlchemyLogRow
            {
                Item = string.Empty,
                Message = line
            })
            .ToList();

        _logRows.Clear();
        foreach (var row in filtered)
            _logRows.Add(row);
    }

    private async void ItemSelect_Changed(object value)
    {
        var code = value?.ToString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(code))
            return;

        _selectedItemCode = code;
        await PatchAsync("alchemyItemCode", code);
    }

    private async void ElixirSelect_Changed(object value)
    {
        var normalized = NormalizeElixir(value?.ToString() ?? "weapon");
        await PatchAsync("alchemyElixirType", normalized);
    }

    private void MaxMinusBtn_Click(object? sender, RoutedEventArgs e)
    {
        var current = ParseInt(MaxEnhancementBox.Text, 0);
        current = Math.Clamp(current - 1, 0, 15);
        MaxEnhancementBox.Text = current.ToString(CultureInfo.InvariantCulture);
        _ = PatchAsync("alchemyMaxEnhancement", current);
    }

    private void MaxPlusBtn_Click(object? sender, RoutedEventArgs e)
    {
        var current = ParseInt(MaxEnhancementBox.Text, 0);
        current = Math.Clamp(current + 1, 0, 15);
        MaxEnhancementBox.Text = current.ToString(CultureInfo.InvariantCulture);
        _ = PatchAsync("alchemyMaxEnhancement", current);
    }

    private void MaxEnhancementBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        var value = Math.Clamp(ParseInt(MaxEnhancementBox.Text, 0), 0, 15);
        MaxEnhancementBox.Text = value.ToString(CultureInfo.InvariantCulture);
        _ = PatchAsync("alchemyMaxEnhancement", value);
    }

    private void BlueToggle_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox toggle || toggle.Tag is not string key)
            return;

        _ = PatchBoolAsync($"alchemyBlueEnabled_{key}", toggle.IsChecked == true);
    }

    private void BlueMaxBox_LostFocus(object? sender, RoutedEventArgs e)
    {
        if (sender is not TextBox box || box.Tag is not string key)
            return;

        var value = Math.Max(0, ParseInt(box.Text, 0));
        box.Text = value.ToString(CultureInfo.InvariantCulture);
        _ = PatchAsync($"alchemyBlueMax_{key}", value);
    }

    private void StatToggle_IsCheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox toggle || toggle.Tag is not string key)
            return;

        _ = PatchBoolAsync($"alchemyStatEnabled_{key}", toggle.IsChecked == true);
    }

    private async void StatTarget_SelectionChanged(string key, object value)
    {
        if (value is not string target)
            return;

        await PatchAsync($"alchemyStatTarget_{key}", NormalizeStatTarget(target));
    }

    private async System.Threading.Tasks.Task PatchBoolAsync(string key, bool value)
    {
        await PatchAsync(key, value);
    }

    private async System.Threading.Tasks.Task PatchAsync(string key, object? value)
    {
        if (_vm == null || _syncing)
            return;

        await _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            [key] = value
        });
    }

    private async void AlchemyStartBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null || _state == null)
            return;

        RuntimeStatus status;
        if (_botRunning && _alchemySelected)
            status = await _vm.Core.StopBotAsync();
        else
            status = await _vm.Core.StartBotAsync();

        _state.ApplyStatus(status);
        _botRunning = status.BotRunning;
        _alchemySelected = !string.IsNullOrWhiteSpace(status.SelectedBotbase)
            && status.SelectedBotbase.Contains("alchemy", StringComparison.OrdinalIgnoreCase);
        UpdateStartButtonVisual();
    }

    private void UpdateStartButtonVisual()
    {
        var shouldShowStop = _botRunning && _alchemySelected;
        AlchemyStartBtn.Content = shouldShowStop ? "Alchemy Stop" : "Alchemy Start";

        AlchemyStartBtn.Classes.Remove("go");
        AlchemyStartBtn.Classes.Remove("halt");
        AlchemyStartBtn.Classes.Add(shouldShowStop ? "halt" : "go");
    }

    private void ReleaseFeatureCache()
    {
        _itemOptions.Clear();
        _blueRows.Clear();
        _statRows.Clear();
        _logRows.Clear();
        ItemSelect.Options = new List<SelectOption>();
    }

    private static List<AlchemyRow> ParseRows(JsonElement root, string key)
    {
        if (!root.TryGetProperty(key, out var rowsNode) || rowsNode.ValueKind != JsonValueKind.Array)
            return new List<AlchemyRow>();

        var result = new List<AlchemyRow>();
        foreach (var row in rowsNode.EnumerateArray())
        {
            if (row.ValueKind != JsonValueKind.Object)
                continue;

            var keyValue = row.TryGetProperty("key", out var keyNode) ? keyNode.GetString() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(keyValue))
                continue;

            var name = row.TryGetProperty("name", out var nameNode) ? nameNode.GetString() ?? keyValue : keyValue;
            var value = row.TryGetProperty("value", out var valueNode) ? valueNode.GetString() ?? "0" : "0";

            result.Add(new AlchemyRow
            {
                Key = keyValue,
                Name = name,
                Value = value,
                Current = ReadInt(row, "current"),
                Max = ReadInt(row, "max"),
                StoneCount = ReadInt(row, "stoneCount")
            });
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

    private static string NormalizeMode(string mode)
    {
        var normalized = (mode ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "blues" => "blues",
            "stats" => "stats",
            _ => "enhance"
        };
    }

    private static string NormalizeElixir(string mode)
    {
        var normalized = (mode ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "shield" => "shield",
            "protector" => "protector",
            "accessory" => "accessory",
            _ => "weapon"
        };
    }

    private static string NormalizeStatTarget(string target)
    {
        var normalized = (target ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "low" => "low",
            "medium" => "medium",
            "high" => "high",
            "max" => "max",
            _ => "off"
        };
    }

    private static int ClampInt(double value, int min, int max)
    {
        var rounded = (int)Math.Round(value);
        return Math.Clamp(rounded, min, max);
    }
}
