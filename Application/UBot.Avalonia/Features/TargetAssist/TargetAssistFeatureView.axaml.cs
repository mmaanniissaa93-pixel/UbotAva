using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using global::Avalonia.Layout;
using System;
using System.Collections.ObjectModel;
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

    private ToggleSwitch? _enabledCheck;
    private TextBox? _maxRangeBox;
    private ToggleSwitch? _includeDeadCheck;
    private ToggleSwitch? _ignoreSnowCheck;
    private ToggleSwitch? _ignoreBloodyCheck;
    private ToggleSwitch? _onlyCustomCheck;
    private ComboBox? _roleModeCombo;
    private TextBox? _cycleKeyDisplayBox;
    private Button? _captureCycleKeyBtn;
    private TextBox? _ignoredGuildsInputBox;
    private TextBox? _customPlayersInputBox;
    private ListBox? _ignoredGuildsList;
    private ListBox? _customPlayersList;
    private TextBlock? _runtimeLabel;
    private TextBlock? _ignoredGuildsMetaLabel;
    private TextBlock? _customPlayersMetaLabel;
    private readonly ObservableCollection<string> _ignoredGuilds = new();
    private readonly ObservableCollection<string> _customPlayers = new();
    private string _capturedCycleKey = "Oem3";
    private bool _capturingCycleKey;
    private bool _built;
    private bool _syncing;

    public TargetAssistFeatureView()
    {
        InitializeComponent();
        Focusable = true;
        AddHandler(KeyDownEvent, TargetAssistFeatureView_KeyDown, RoutingStrategies.Tunnel);
        _ignoredGuilds.CollectionChanged += (_, _) =>
        {
            UpdateListMetaLabels();
            UpdateListHeights();
        };
        _customPlayers.CollectionChanged += (_, _) =>
        {
            UpdateListMetaLabels();
            UpdateListHeights();
        };
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
                ? DesktopLanguageService.Translate("No target candidates in range.")
                : string.Format(
                    DesktopLanguageService.Translate("Candidates: {0}  |  Nearest: {1} {2}"),
                    candidates,
                    nearest,
                    distance >= 0 ? $"({distance:0.0}m)" : string.Empty);
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
            _capturedCycleKey = _vm.TextCfg("targetCycleKey", "Oem3");
            SyncCycleKeyDisplay();
            ReplaceCollection(_ignoredGuilds, ToStringList(_vm.ObjCfg("ignoredGuilds")));
            ReplaceCollection(_customPlayers, ToStringList(_vm.ObjCfg("customPlayers")));
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

        var layout = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(2, 2, 2, 10)
        };

        _runtimeLabel = new TextBlock
        {
            Text = DesktopLanguageService.Translate("No target candidates in range."),
            Classes = { "ta-helper" }
        };

        _enabledCheck = CreateCheck("Enable target assist");
        _maxRangeBox = CreateTextBox("40");
        _includeDeadCheck = CreateCheck("Include dead targets");
        _ignoreSnowCheck = CreateCheck("Ignore snow shield targets");
        _ignoreBloodyCheck = CreateCheck("Ignore bloody storm targets");
        _onlyCustomCheck = CreateCheck("Only custom players");
        _enabledCheck.Classes.Add("ta-toggle");
        _includeDeadCheck.Classes.Add("ta-toggle");
        _ignoreSnowCheck.Classes.Add("ta-toggle");
        _ignoreBloodyCheck.Classes.Add("ta-toggle");
        _onlyCustomCheck.Classes.Add("ta-toggle");
        _cycleKeyDisplayBox = new TextBox
        {
            Width = 178,
            IsReadOnly = true
        };
        _cycleKeyDisplayBox.Classes.Add("ta-input");
        _captureCycleKeyBtn = new Button
        {
            Content = "[] Capture",
            Width = 110
        };
        _captureCycleKeyBtn.Classes.Add("ta-btn");
        _captureCycleKeyBtn.Classes.Add("ta-btn-accent");
        _captureCycleKeyBtn.Click += CaptureCycleKeyBtn_Click;
        _ignoredGuildsInputBox = CreateTextBox(string.Empty);
        _customPlayersInputBox = CreateTextBox(string.Empty);
        _ignoredGuildsInputBox.Classes.Add("ta-input");
        _customPlayersInputBox.Classes.Add("ta-input");
        _ignoredGuildsList = new ListBox
        {
            Height = 96,
            ItemsSource = _ignoredGuilds
        };
        _ignoredGuildsList.Classes.Add("ta-list");
        _customPlayersList = new ListBox
        {
            Height = 96,
            ItemsSource = _customPlayers
        };
        _customPlayersList.Classes.Add("ta-list");
        _ignoredGuildsMetaLabel = new TextBlock { Classes = { "ta-helper" } };
        _customPlayersMetaLabel = new TextBlock { Classes = { "ta-helper" } };

        _roleModeCombo = new ComboBox
        {
            ItemsSource = new[] { "civil", "thief", "hunterTrader" },
            SelectedItem = "civil",
            Width = 220,
            Classes = { "ta-input" }
        };
        _maxRangeBox.Width = 220;

        var settingsCard = new Border { Classes = { "ta-card", "ta-card-primary" } };
        var settingsStack = new StackPanel { Spacing = 7 };
        settingsStack.Children.Add(CreateSectionHeader("M10 1 L14 5 L10 9 L6 5 Z M1 10 L5 6 L9 10 L5 14 Z", "Core Settings"));
        settingsStack.Children.Add(new Border { Classes = { "ta-section-divider" } });
        settingsStack.Children.Add(_runtimeLabel);
        settingsStack.Children.Add(CreateRow("Enabled", _enabledCheck));
        settingsStack.Children.Add(CreateRow("Max range", _maxRangeBox));
        settingsStack.Children.Add(CreateRow("Role mode", _roleModeCombo));
        settingsStack.Children.Add(CreateHotkeyRow());
        settingsCard.Child = settingsStack;

        var optionsCard = new Border { Classes = { "ta-card", "ta-card-secondary" } };
        var optionsStack = new StackPanel { Spacing = 7 };
        optionsStack.Children.Add(CreateSectionHeader("M2 8 L8 2 L14 8 L12.5 9.5 L8 5 L3.5 9.5 Z M5 10 L11 10 L11 14 L5 14 Z", "Target Filters"));
        optionsStack.Children.Add(new Border { Classes = { "ta-section-divider" } });
        var togglesGrid = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
        _includeDeadCheck!.Margin = new Thickness(0, 0, 8, 0);
        _ignoreBloodyCheck!.Margin = new Thickness(8, 0, 0, 0);
        _ignoreSnowCheck!.Margin = new Thickness(0, 0, 8, 0);
        _onlyCustomCheck!.Margin = new Thickness(8, 0, 0, 0);
        Grid.SetColumn(_includeDeadCheck, 0);
        Grid.SetColumn(_ignoreBloodyCheck, 1);
        Grid.SetColumn(_ignoreSnowCheck, 0);
        Grid.SetRow(_ignoreSnowCheck, 1);
        Grid.SetColumn(_onlyCustomCheck, 1);
        Grid.SetRow(_onlyCustomCheck, 1);
        togglesGrid.RowDefinitions = new RowDefinitions("Auto,Auto");
        togglesGrid.Children.Add(_includeDeadCheck);
        togglesGrid.Children.Add(_ignoreBloodyCheck);
        togglesGrid.Children.Add(_ignoreSnowCheck);
        togglesGrid.Children.Add(_onlyCustomCheck);
        optionsStack.Children.Add(togglesGrid);
        optionsCard.Child = optionsStack;

        var topGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("1.7*,1.3*")
        };
        settingsCard.Margin = new Thickness(0, 0, 4, 0);
        optionsCard.Margin = new Thickness(4, 0, 0, 0);
        Grid.SetColumn(settingsCard, 0);
        Grid.SetColumn(optionsCard, 1);
        topGrid.Children.Add(settingsCard);
        topGrid.Children.Add(optionsCard);
        layout.Children.Add(topGrid);

        layout.Children.Add(CreateListEditorsSection());

        var saveBtn = new Button
        {
            Content = "Save",
            Classes = { "ta-btn", "ta-btn-primary" },
            Width = 128
        };
        saveBtn.Click += SaveBtn_Click;

        var footerRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        footerRow.Children.Add(new TextBlock
        {
            Text = "Changes apply instantly.",
            Classes = { "ta-helper" },
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetColumn(saveBtn, 1);
        footerRow.Children.Add(saveBtn);
        layout.Children.Add(footerRow);

        ContentHost.Children.Add(layout);
        UpdateListMetaLabels();
        UpdateListHeights();
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
            ["targetCycleKey"] = string.IsNullOrWhiteSpace(_capturedCycleKey) ? "Oem3" : _capturedCycleKey,
            ["ignoredGuilds"] = _ignoredGuilds.ToList(),
            ["customPlayers"] = _customPlayers.ToList()
        };

        await _vm.PatchConfigAsync(patch);
    }

    private Control CreateHotkeyRow()
    {
        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10 };
        row.Children.Add(new TextBlock
        {
            Text = "Target cycle key",
            Width = 160,
            Classes = { "ta-label" },
            VerticalAlignment = VerticalAlignment.Center
        });
        row.Children.Add(_cycleKeyDisplayBox!);
        row.Children.Add(_captureCycleKeyBtn!);
        SyncCycleKeyDisplay();
        return row;
    }

    private Control CreateListEditorsSection()
    {
        var card = new Border { Classes = { "ta-card", "ta-card-primary" } };
        var wrap = new StackPanel { Spacing = 8 };
        wrap.Children.Add(CreateSectionHeader("M2 3 L14 3 L14 5 L2 5 Z M2 7 L14 7 L14 9 L2 9 Z M2 11 L14 11 L14 13 L2 13 Z", "Custom Target Lists"));
        wrap.Children.Add(new Border { Classes = { "ta-section-divider" } });

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,*")
        };

        var ignoredPanel = CreateListEditorPanel(
            "Ignored Guilds",
            "Guild name",
            _ignoredGuildsInputBox!,
            _ignoredGuildsList!,
            _ignoredGuilds);
        ignoredPanel.Margin = new Thickness(0, 0, 4, 0);
        Grid.SetColumn(ignoredPanel, 0);
        grid.Children.Add(ignoredPanel);

        var playersPanel = CreateListEditorPanel(
            "Custom Players",
            "Player name",
            _customPlayersInputBox!,
            _customPlayersList!,
            _customPlayers);
        playersPanel.Margin = new Thickness(4, 0, 0, 0);
        Grid.SetColumn(playersPanel, 1);
        grid.Children.Add(playersPanel);

        wrap.Children.Add(grid);
        card.Child = wrap;
        return card;
    }

    private Control CreateListEditorPanel(string title, string watermark, TextBox input, ListBox listBox, ObservableCollection<string> source)
    {
        input.Width = 198;
        input.Watermark = watermark;

        var addBtn = new Button { Content = "+ Add", Width = 78 };
        addBtn.Classes.Add("ta-btn");
        addBtn.Classes.Add("ta-btn-accent");
        addBtn.Click += (_, _) => AddListItem(source, input);

        input.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                AddListItem(source, input);
                e.Handled = true;
            }
        };

        var topRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        topRow.Children.Add(input);
        topRow.Children.Add(addBtn);

        var removeBtn = new Button { Content = "- Remove", Width = 94 };
        removeBtn.Classes.Add("ta-btn");
        removeBtn.Classes.Add("ta-btn-danger");
        removeBtn.Click += (_, _) =>
        {
            if (listBox.SelectedItem is string selected)
                source.Remove(selected);
        };

        var panelCard = new Border { Classes = { "ta-subcard" } };
        var panel = new StackPanel { Spacing = 8 };
        panel.Children.Add(new TextBlock
        {
            Text = title,
            Classes = { "ta-label" }
        });
        panel.Children.Add(topRow);
        if (ReferenceEquals(source, _ignoredGuilds) && _ignoredGuildsMetaLabel != null)
            panel.Children.Add(_ignoredGuildsMetaLabel);
        if (ReferenceEquals(source, _customPlayers) && _customPlayersMetaLabel != null)
            panel.Children.Add(_customPlayersMetaLabel);
        panel.Children.Add(listBox);
        var actionRow = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto") };
        Grid.SetColumn(removeBtn, 1);
        actionRow.Children.Add(removeBtn);
        panel.Children.Add(actionRow);
        panelCard.Child = panel;
        return panelCard;
    }

    private static void AddListItem(ObservableCollection<string> target, TextBox input)
    {
        var value = NormalizeEntry(input.Text);
        if (value == null)
            return;

        if (!target.Any(x => string.Equals(x, value, StringComparison.OrdinalIgnoreCase)))
            target.Add(value);

        input.Text = string.Empty;
    }

    private static void ReplaceCollection(ObservableCollection<string> target, IEnumerable<string> values)
    {
        target.Clear();
        foreach (var value in values.Select(NormalizeEntry).Where(x => x != null)!)
        {
            if (!target.Any(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase)))
                target.Add(value!);
        }
    }

    private static string? NormalizeEntry(string? raw)
    {
        var value = (raw ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private void CaptureCycleKeyBtn_Click(object? sender, RoutedEventArgs e)
    {
        _capturingCycleKey = !_capturingCycleKey;
        SyncCycleKeyDisplay();
    }

    private void TargetAssistFeatureView_KeyDown(object? sender, KeyEventArgs e)
    {
        if (!_capturingCycleKey)
            return;

        if (e.Key == Key.Escape)
        {
            _capturingCycleKey = false;
            SyncCycleKeyDisplay();
            e.Handled = true;
            return;
        }

        if (IsModifierOnlyKey(e.Key))
        {
            e.Handled = true;
            return;
        }

        _capturedCycleKey = e.Key.ToString();
        _capturingCycleKey = false;
        SyncCycleKeyDisplay();
        e.Handled = true;
    }

    private static bool IsModifierOnlyKey(Key key)
    {
        return key is Key.LeftCtrl or Key.RightCtrl
            or Key.LeftAlt or Key.RightAlt
            or Key.LWin or Key.RWin
            or Key.LeftShift or Key.RightShift;
    }

    private void SyncCycleKeyDisplay()
    {
        if (_cycleKeyDisplayBox != null)
            _cycleKeyDisplayBox.Text = _capturingCycleKey
                ? DesktopLanguageService.Translate("Press a key... (Esc to cancel)")
                : (string.IsNullOrWhiteSpace(_capturedCycleKey) ? "Oem3" : _capturedCycleKey);

        if (_captureCycleKeyBtn != null)
            _captureCycleKeyBtn.Content = _capturingCycleKey
                ? DesktopLanguageService.Translate("[] Stop")
                : DesktopLanguageService.Translate("[] Capture");
    }

    private void UpdateListMetaLabels()
    {
        if (_ignoredGuildsMetaLabel != null)
            _ignoredGuildsMetaLabel.Text = _ignoredGuilds.Count == 0
                ? DesktopLanguageService.Translate("No guild entry yet. Add a guild to exclude from targeting.")
                : string.Format(DesktopLanguageService.Translate("{0} guild entries configured."), _ignoredGuilds.Count);

        if (_customPlayersMetaLabel != null)
            _customPlayersMetaLabel.Text = _customPlayers.Count == 0
                ? DesktopLanguageService.Translate("No custom player entry yet. Add names for explicit targeting.")
                : string.Format(DesktopLanguageService.Translate("{0} custom players configured."), _customPlayers.Count);
    }

    private void UpdateListHeights()
    {
        if (_ignoredGuildsList != null)
            _ignoredGuildsList.Height = _ignoredGuilds.Count == 0 ? 82 : 104;
        if (_customPlayersList != null)
            _customPlayersList.Height = _customPlayers.Count == 0 ? 82 : 104;
    }

    private static StackPanel CreateRow(string label, Control control)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("132,Auto")
        };
        grid.Children.Add(new TextBlock
        {
            Text = label,
            Classes = { "ta-label" }
        });
        Grid.SetColumn(control, 1);
        control.Margin = new Thickness(8, 0, 0, 0);
        grid.Children.Add(control);

        var panel = new StackPanel { Spacing = 4 };
        panel.Children.Add(grid);
        return panel;
    }

    private static Control CreateSectionHeader(string iconText, string title)
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        var badge = new Border { Classes = { "ta-icon-badge" } };
        badge.Child = new Path
        {
            Data = Geometry.Parse(iconText),
            Classes = { "ta-icon-path" }
        };

        row.Children.Add(badge);
        row.Children.Add(new TextBlock
        {
            Text = title,
            Classes = { "ta-title" },
            VerticalAlignment = VerticalAlignment.Center
        });
        return row;
    }

    private static ToggleSwitch CreateCheck(string content)
    {
        return new ToggleSwitch
        {
            Content = content,
            Classes = { "ta-toggle" }
        };
    }

    private static TextBox CreateTextBox(string value)
    {
        var box = new TextBox
        {
            Text = value,
            Width = 260
        };
        box.Classes.Add("ta-input");
        return box;
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
