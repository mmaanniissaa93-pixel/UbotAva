using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using global::Avalonia.Layout;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.Lure;

public partial class LureFeatureView : UserControl
{
    private PluginViewModelBase? _vm;
    private AppState? _state;

    private TextBox? _centerRegionBox;
    private TextBox? _centerXBox;
    private TextBox? _centerYBox;
    private TextBox? _centerZBox;
    private TextBox? _radiusBox;
    private TextBox? _walkbackScriptBox;
    private ComboBox? _modeCombo;
    private TextBox? _selectedScriptBox;
    private Button? _openRecorderBtn;
    private CheckBox? _stayForCheck;
    private TextBox? _stayForSecondsBox;
    private CheckBox? _howlingCheck;
    private CheckBox? _dontHowlingCenterCheck;
    private CheckBox? _useNormalAttackCheck;
    private CheckBox? _useAttackSkillsCheck;
    private CheckBox? _stopDeadMembersCheck;
    private TextBox? _stopDeadMembersCountBox;
    private CheckBox? _stopMembersCheck;
    private TextBox? _stopMembersCountBox;
    private CheckBox? _stopMembersOnSpotCheck;
    private TextBox? _stopMembersOnSpotCountBox;
    private CheckBox? _stopMonsterTypeCheck;
    private ComboBox? _monsterTypeCombo;
    private TextBox? _stopMonsterCountBox;
    private TextBlock? _currentPositionLabel;

    private int _currentRegion;
    private double _currentX;
    private double _currentY;
    private double _currentZ;
    private bool _syncing;
    private bool _built;

    public LureFeatureView()
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
        if (moduleState.ValueKind == JsonValueKind.Object && moduleState.TryGetProperty("lure", out var lureNode))
            root = lureNode;
        if (root.ValueKind != JsonValueKind.Object)
            return;

        if (root.TryGetProperty("currentPosition", out var positionNode) && positionNode.ValueKind == JsonValueKind.Object)
        {
            _currentRegion = ReadInt(positionNode, "region");
            _currentX = ReadDouble(positionNode, "x");
            _currentY = ReadDouble(positionNode, "y");
            _currentZ = ReadDouble(positionNode, "z");
            if (_currentPositionLabel != null)
                _currentPositionLabel.Text = $"Current: region {_currentRegion} | X:{_currentX:0.0} Y:{_currentY:0.0} Z:{_currentZ:0.0}";
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
            _centerRegionBox!.Text = ((int)Math.Round(_vm.NumCfg("lureCenterRegion", 0))).ToString(CultureInfo.InvariantCulture);
            _centerXBox!.Text = _vm.NumCfg("lureCenterX", 0).ToString("0.0", CultureInfo.InvariantCulture);
            _centerYBox!.Text = _vm.NumCfg("lureCenterY", 0).ToString("0.0", CultureInfo.InvariantCulture);
            _centerZBox!.Text = _vm.NumCfg("lureCenterZ", 0).ToString("0.0", CultureInfo.InvariantCulture);
            _radiusBox!.Text = ((int)Math.Round(_vm.NumCfg("lureRadius", 20))).ToString(CultureInfo.InvariantCulture);
            _walkbackScriptBox!.Text = _vm.TextCfg("lureLocationScript", string.Empty);
            _modeCombo!.SelectedItem = _vm.TextCfg("lureMode", "walkRandomly");
            _selectedScriptBox!.Text = _vm.TextCfg("lureScriptPath", string.Empty);
            _stayForCheck!.IsChecked = _vm.BoolCfg("lureStayAtCenterForEnabled", false);
            _stayForSecondsBox!.Text = ((int)Math.Round(_vm.NumCfg("lureStayAtCenterSeconds", 10))).ToString(CultureInfo.InvariantCulture);
            _howlingCheck!.IsChecked = _vm.BoolCfg("lureCastHowlingShout", false);
            _dontHowlingCenterCheck!.IsChecked = _vm.BoolCfg("lureDontCastNearCenter", true);
            _useNormalAttackCheck!.IsChecked = _vm.BoolCfg("lureUseNormalAttackSwitch", false);
            _useAttackSkillsCheck!.IsChecked = _vm.BoolCfg("lureUseAttackSkillSwitch", false);
            _stopDeadMembersCheck!.IsChecked = _vm.BoolCfg("lureStopOnDeadPartyMembersEnabled", false);
            _stopDeadMembersCountBox!.Text = ((int)Math.Round(_vm.NumCfg("lureStopDeadPartyMembers", 0))).ToString(CultureInfo.InvariantCulture);
            _stopMembersCheck!.IsChecked = _vm.BoolCfg("lureStopOnPartyMembersEnabled", false);
            _stopMembersCountBox!.Text = ((int)Math.Round(_vm.NumCfg("lureStopPartyMembersLe", 0))).ToString(CultureInfo.InvariantCulture);
            _stopMembersOnSpotCheck!.IsChecked = _vm.BoolCfg("lureStopOnPartyMembersOnSpotEnabled", false);
            _stopMembersOnSpotCountBox!.Text = ((int)Math.Round(_vm.NumCfg("lureStopPartyMembersOnSpotLe", 0))).ToString(CultureInfo.InvariantCulture);
            _stopMonsterTypeCheck!.IsChecked = _vm.BoolCfg("lureStopOnMonsterTypeEnabled", false);
            _monsterTypeCombo!.SelectedItem = _vm.TextCfg("lureMonsterType", "General");
            _stopMonsterCountBox!.Text = ((int)Math.Round(_vm.NumCfg("lureStopMonsterCount", 1))).ToString(CultureInfo.InvariantCulture);
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
        TabStripCtrl.SetTabs(new[] { ("lure", "Lure") });
        TabStripCtrl.ActiveTabId = "lure";
        ContentHost.Children.Clear();

        var layout = new StackPanel { Spacing = 10 };

        _currentPositionLabel = new TextBlock
        {
            Text = "Current: -",
            Classes = { "form-label" }
        };
        layout.Children.Add(_currentPositionLabel);

        _centerRegionBox = CreateTextBox("0");
        _centerXBox = CreateTextBox("0");
        _centerYBox = CreateTextBox("0");
        _centerZBox = CreateTextBox("0");
        _radiusBox = CreateTextBox("20");
        _walkbackScriptBox = CreateTextBox(string.Empty, 480);
        _selectedScriptBox = CreateTextBox(string.Empty, 480);
        _openRecorderBtn = new Button { Content = "Open Script Recorder", Width = 180 };
        _openRecorderBtn.Click += OpenRecorderBtn_Click;
        _modeCombo = new ComboBox { ItemsSource = new[] { "walkRandomly", "stayAtCenter", "useScript" }, SelectedItem = "walkRandomly", Width = 220 };
        _stayForCheck = CreateCheck("Stay at center for");
        _stayForSecondsBox = CreateTextBox("10", 120);
        _howlingCheck = CreateCheck("Cast howling shout");
        _dontHowlingCenterCheck = CreateCheck("Do not cast near center");
        _useNormalAttackCheck = CreateCheck("Use normal attack");
        _useAttackSkillsCheck = CreateCheck("Use attacking skills");
        _stopDeadMembersCheck = CreateCheck("Stop if dead party members >=");
        _stopDeadMembersCountBox = CreateTextBox("0", 120);
        _stopMembersCheck = CreateCheck("Stop if party members <=");
        _stopMembersCountBox = CreateTextBox("0", 120);
        _stopMembersOnSpotCheck = CreateCheck("Stop if party members on spot <=");
        _stopMembersOnSpotCountBox = CreateTextBox("0", 120);
        _stopMonsterTypeCheck = CreateCheck("Stop on monster type count");
        _monsterTypeCombo = new ComboBox
        {
            ItemsSource = new[] { "General", "Champion", "Giant", "GeneralParty", "ChampionParty", "GiantParty", "Elite", "Strong", "Unique" },
            SelectedItem = "General",
            Width = 220
        };
        _stopMonsterCountBox = CreateTextBox("1", 120);

        var useCurrentBtn = new Button { Content = "Use Current Position", Width = 180 };
        useCurrentBtn.Click += UseCurrentBtn_Click;

        var saveBtn = new Button { Content = "Save", Classes = { "primary" }, Width = 140 };
        saveBtn.Click += SaveBtn_Click;

        layout.Children.Add(CreateRow("Center region", _centerRegionBox));
        layout.Children.Add(CreateRow("Center X", _centerXBox));
        layout.Children.Add(CreateRow("Center Y", _centerYBox));
        layout.Children.Add(CreateRow("Center Z", _centerZBox));
        layout.Children.Add(CreateRow("Radius", _radiusBox));
        layout.Children.Add(CreateRow("Walkback script", _walkbackScriptBox));
        layout.Children.Add(CreateRow("Lure mode", _modeCombo));
        layout.Children.Add(CreateRow("Selected script", _selectedScriptBox));
        layout.Children.Add(_openRecorderBtn);
        layout.Children.Add(useCurrentBtn);
        layout.Children.Add(CreateRowControl(_stayForCheck, _stayForSecondsBox));
        layout.Children.Add(_howlingCheck);
        layout.Children.Add(_dontHowlingCenterCheck);
        layout.Children.Add(_useNormalAttackCheck);
        layout.Children.Add(_useAttackSkillsCheck);
        layout.Children.Add(CreateRowControl(_stopDeadMembersCheck, _stopDeadMembersCountBox));
        layout.Children.Add(CreateRowControl(_stopMembersCheck, _stopMembersCountBox));
        layout.Children.Add(CreateRowControl(_stopMembersOnSpotCheck, _stopMembersOnSpotCountBox));
        layout.Children.Add(_stopMonsterTypeCheck);
        layout.Children.Add(CreateRow("Monster type", _monsterTypeCombo));
        layout.Children.Add(CreateRow("Stop monster count", _stopMonsterCountBox));
        layout.Children.Add(saveBtn);

        ContentHost.Children.Add(layout);
    }

    private void UseCurrentBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (_syncing)
            return;

        _centerRegionBox!.Text = _currentRegion.ToString(CultureInfo.InvariantCulture);
        _centerXBox!.Text = _currentX.ToString("0.0", CultureInfo.InvariantCulture);
        _centerYBox!.Text = _currentY.ToString("0.0", CultureInfo.InvariantCulture);
        _centerZBox!.Text = _currentZ.ToString("0.0", CultureInfo.InvariantCulture);
    }

    private async void SaveBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null || _syncing)
            return;

        var patch = new Dictionary<string, object?>
        {
            ["lureCenterRegion"] = ParseInt(_centerRegionBox?.Text, 0),
            ["lureCenterX"] = ParseDouble(_centerXBox?.Text, 0),
            ["lureCenterY"] = ParseDouble(_centerYBox?.Text, 0),
            ["lureCenterZ"] = ParseDouble(_centerZBox?.Text, 0),
            ["lureRadius"] = ParseInt(_radiusBox?.Text, 20),
            ["lureLocationScript"] = (_walkbackScriptBox?.Text ?? string.Empty).Trim(),
            ["lureMode"] = _modeCombo?.SelectedItem?.ToString() ?? "walkRandomly",
            ["lureScriptPath"] = (_selectedScriptBox?.Text ?? string.Empty).Trim(),
            ["lureStayAtCenterForEnabled"] = _stayForCheck?.IsChecked == true,
            ["lureStayAtCenterSeconds"] = ParseInt(_stayForSecondsBox?.Text, 10),
            ["lureCastHowlingShout"] = _howlingCheck?.IsChecked == true,
            ["lureDontCastNearCenter"] = _dontHowlingCenterCheck?.IsChecked == true,
            ["lureUseNormalAttackSwitch"] = _useNormalAttackCheck?.IsChecked == true,
            ["lureUseAttackSkillSwitch"] = _useAttackSkillsCheck?.IsChecked == true,
            ["lureStopOnDeadPartyMembersEnabled"] = _stopDeadMembersCheck?.IsChecked == true,
            ["lureStopDeadPartyMembers"] = ParseInt(_stopDeadMembersCountBox?.Text, 0),
            ["lureStopOnPartyMembersEnabled"] = _stopMembersCheck?.IsChecked == true,
            ["lureStopPartyMembersLe"] = ParseInt(_stopMembersCountBox?.Text, 0),
            ["lureStopOnPartyMembersOnSpotEnabled"] = _stopMembersOnSpotCheck?.IsChecked == true,
            ["lureStopPartyMembersOnSpotLe"] = ParseInt(_stopMembersOnSpotCountBox?.Text, 0),
            ["lureStopOnMonsterTypeEnabled"] = _stopMonsterTypeCheck?.IsChecked == true,
            ["lureMonsterType"] = _monsterTypeCombo?.SelectedItem?.ToString() ?? "General",
            ["lureStopMonsterCount"] = ParseInt(_stopMonsterCountBox?.Text, 1)
        };

        await _vm.PatchConfigAsync(patch);
    }

    private void OpenRecorderBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        var currentPath = (_selectedScriptBox?.Text ?? string.Empty).Trim();
        var owner = TopLevel.GetTopLevel(this) as Window;
        var recorder = new LureRecorderWindow(
            currentPath,
            async savedPath =>
            {
                if (string.IsNullOrWhiteSpace(savedPath))
                    return;

                if (_selectedScriptBox != null)
                    _selectedScriptBox.Text = savedPath;

                await _vm.PatchConfigAsync(new Dictionary<string, object?>
                {
                    ["lureScriptPath"] = savedPath
                });
            },
            _vm.Core
        );

        if (owner != null)
        {
            recorder.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            recorder.Show(owner);
        }
        else
        {
            recorder.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            recorder.Show();
        }
    }

    private static StackPanel CreateRow(string label, Control control)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        panel.Children.Add(new TextBlock { Text = label, Width = 220, VerticalAlignment = VerticalAlignment.Center });
        panel.Children.Add(control);
        return panel;
    }

    private static StackPanel CreateRowControl(CheckBox check, Control control)
    {
        var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        panel.Children.Add(check);
        panel.Children.Add(control);
        return panel;
    }

    private static TextBox CreateTextBox(string value, double width = 220)
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

    private static double ParseDouble(string? value, double fallback)
    {
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : fallback;
    }

    private static int ReadInt(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) && value.TryGetInt32(out var parsed)
            ? parsed
            : 0;
    }

    private static double ReadDouble(JsonElement root, string name)
    {
        return root.TryGetProperty(name, out var value) && value.TryGetDouble(out var parsed)
            ? parsed
            : 0;
    }
}
