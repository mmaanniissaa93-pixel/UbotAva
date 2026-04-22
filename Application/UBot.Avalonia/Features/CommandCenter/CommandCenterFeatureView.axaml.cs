using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using global::Avalonia.Layout;
using global::Avalonia.Media;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.CommandCenter;

public partial class CommandCenterFeatureView : UserControl
{
    private sealed class EmoteRow
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public ComboBox? Combo { get; set; }
    }

    private PluginViewModelBase? _vm;
    private AppState? _state;
    private bool _built;
    private bool _syncing;

    private CheckBox? _enabledCheck;
    private TextBlock? _chatCommandsLabel;
    private readonly List<EmoteRow> _emoteRows = new();
    private List<string> _commandOptions = new();

    public CommandCenterFeatureView()
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
        // No live state stream needed for command center right now.
    }

    private async System.Threading.Tasks.Task LoadFromConfigAsync()
    {
        if (_vm == null)
            return;

        await _vm.LoadConfigAsync();
        _syncing = true;
        try
        {
            _enabledCheck!.IsChecked = _vm.BoolCfg("enabled", true);

            _commandOptions = ParseCommandOptions(_vm.ObjCfg("commandOptions"));
            if (_commandOptions.Count == 0)
                _commandOptions = new List<string> { "none", "area", "buff", "show", "start", "here", "stop" };

            var emotes = ParseEmotes(_vm.ObjCfg("emotes"));
            BuildEmoteRows(emotes);
            BuildChatCommandsLabel(_vm.ObjCfg("chatCommands"));
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
        TabStripCtrl.SetTabs(new[] { ("commandCenter", "Command Center") });
        TabStripCtrl.ActiveTabId = "commandCenter";
        ContentHost.Children.Clear();

        var layout = new StackPanel { Spacing = 10 };

        _enabledCheck = new CheckBox
        {
            Content = "Enable command center",
            Classes = { "check" }
        };
        layout.Children.Add(_enabledCheck);

        _chatCommandsLabel = new TextBlock
        {
            Text = "Chat commands: -",
            Classes = { "form-label" },
            TextWrapping = TextWrapping.Wrap
        };
        layout.Children.Add(_chatCommandsLabel);

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

    private void BuildEmoteRows(List<(string Id, string Label, string Command)> emotes)
    {
        if (ContentHost.Children.Count == 0 || ContentHost.Children[0] is not StackPanel layout)
            return;

        // Remove old dynamic rows (between enabled checkbox and chat commands label).
        for (var i = layout.Children.Count - 1; i >= 0; i--)
        {
            if (layout.Children[i] is StackPanel row && row.Tag?.ToString() == "emote-row")
                layout.Children.RemoveAt(i);
        }
        _emoteRows.Clear();

        var insertIndex = 1;
        foreach (var emote in emotes)
        {
            var combo = new ComboBox
            {
                ItemsSource = _commandOptions,
                SelectedItem = _commandOptions.Contains(emote.Command, StringComparer.OrdinalIgnoreCase)
                    ? _commandOptions.First(x => x.Equals(emote.Command, StringComparison.OrdinalIgnoreCase))
                    : "none",
                Width = 220
            };

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 10,
                Tag = "emote-row"
            };
            row.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(emote.Label) ? emote.Id : emote.Label,
                Width = 220,
                VerticalAlignment = VerticalAlignment.Center
            });
            row.Children.Add(combo);

            layout.Children.Insert(insertIndex++, row);
            _emoteRows.Add(new EmoteRow
            {
                Id = emote.Id,
                Label = emote.Label,
                Combo = combo
            });
        }
    }

    private void BuildChatCommandsLabel(object? raw)
    {
        if (_chatCommandsLabel == null)
            return;

        var commands = new List<string>();
        if (raw is IEnumerable enumerable && raw is not string)
        {
            foreach (var item in enumerable)
            {
                if (item is IDictionary dict && dict.Contains("trigger"))
                    commands.Add(dict["trigger"]?.ToString() ?? string.Empty);
            }
        }

        commands = commands.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
        _chatCommandsLabel.Text = commands.Count == 0
            ? "Chat commands: -"
            : $"Chat commands: {string.Join(", ", commands)}";
    }

    private async void SaveBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null || _syncing)
            return;

        var emotes = _emoteRows
            .Select(row => new Dictionary<string, object?>
            {
                ["id"] = row.Id,
                ["command"] = row.Combo?.SelectedItem?.ToString() ?? "none"
            })
            .Cast<object?>()
            .ToList();

        var patch = new Dictionary<string, object?>
        {
            ["enabled"] = _enabledCheck?.IsChecked == true,
            ["emotes"] = emotes
        };

        await _vm.PatchConfigAsync(patch);
    }

    private static List<string> ParseCommandOptions(object? raw)
    {
        var result = new List<string>();
        if (raw is not IEnumerable enumerable || raw is string)
            return result;

        foreach (var item in enumerable)
        {
            if (item is IDictionary dict && dict.Contains("value"))
            {
                var value = dict["value"]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                    result.Add(value.Trim());
            }
        }

        return result
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<(string Id, string Label, string Command)> ParseEmotes(object? raw)
    {
        var result = new List<(string Id, string Label, string Command)>();
        if (raw is not IEnumerable enumerable || raw is string)
            return result;

        foreach (var item in enumerable)
        {
            if (item is not IDictionary dict)
                continue;

            var id = dict.Contains("id") ? dict["id"]?.ToString() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(id))
                id = dict.Contains("name") ? dict["name"]?.ToString() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(id))
                continue;

            var label = dict.Contains("label") ? dict["label"]?.ToString() ?? id : id;
            var command = dict.Contains("command") ? dict["command"]?.ToString() ?? "none" : "none";
            result.Add((id, label, command));
        }

        return result;
    }
}
