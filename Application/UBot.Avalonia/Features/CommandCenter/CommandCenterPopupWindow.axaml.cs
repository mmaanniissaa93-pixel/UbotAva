using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UBot.Avalonia.Controls;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.CommandCenter;

public partial class CommandCenterPopupWindow : Window
{
    private sealed class EmoteRowState
    {
        public string Id { get; set; } = string.Empty;
        public string DefaultCommand { get; set; } = "none";
        public CustomSelect Select { get; set; } = null!;
    }

    private sealed class EmoteDto
    {
        public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string IconKey { get; set; } = string.Empty;
        public string Command { get; set; } = "none";
        public string DefaultCommand { get; set; } = "none";
    }

    private sealed class ChatCommandDto
    {
        public string Trigger { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    private static readonly (string Value, string Label)[] PreferredCommandOrder =
    {
        ("none", "No action"),
        ("buff", "Cast all buffs"),
        ("area", "Set the training area"),
        ("here", "Set training area and start bot"),
        ("show", "Show the bot window"),
        ("start", "Start the bot"),
        ("stop", "Stop the bot")
    };

    private PluginViewModelBase? _vm;
    private readonly List<EmoteRowState> _emoteRows = new();
    private readonly Dictionary<string, Bitmap> _iconCache = new(StringComparer.OrdinalIgnoreCase);
    private List<SelectOption> _commandOptions = new();
    private bool _syncing;

    public CommandCenterPopupWindow()
    {
        InitializeComponent();

        Tabs.SetTabs(new[]
        {
            ("emoteCommands", "Emote commands"),
            ("chatCommands", "Chat commands")
        });
        Tabs.ActiveTabId = "emoteCommands";
        Tabs.TabChanged += Tabs_TabChanged;

        ResetBtn.Click += ResetBtn_Click;
        SaveBtn.Click += SaveBtn_Click;

        Opened += async (_, _) => await LoadFromConfigAsync();
    }

    public CommandCenterPopupWindow(PluginViewModelBase vm, AppState state)
        : this()
    {
        _vm = vm;
    }

    private async System.Threading.Tasks.Task LoadFromConfigAsync()
    {
        if (_vm == null)
            return;

        _syncing = true;
        try
        {
            await _vm.LoadConfigAsync();

            EnabledCheck.IsChecked = _vm.BoolCfg("enabled", true);
            _commandOptions = BuildCommandOptions(_vm.ObjCfg("commandOptions"));

            var emotes = ParseEmotes(_vm.ObjCfg("emotes"));
            BuildEmoteRows(emotes);

            var chatCommands = ParseChatCommands(_vm.ObjCfg("chatCommands"));
            BuildChatRows(chatCommands);

            ApplyTabState(Tabs.ActiveTabId);
        }
        finally
        {
            _syncing = false;
        }
    }

    private void BuildEmoteRows(IReadOnlyList<EmoteDto> emotes)
    {
        EmoteRowsHost.Children.Clear();
        _emoteRows.Clear();

        foreach (var emote in emotes)
        {
            var row = new Border
            {
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.Parse("#22496D95")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(4, 8)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("42,140,*"),
                VerticalAlignment = VerticalAlignment.Center
            };

            var iconHost = new Border
            {
                Width = 34,
                Height = 34,
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Color.Parse("#22000000")),
                BorderBrush = new SolidColorBrush(Color.Parse("#3A6D8FB7")),
                BorderThickness = new Thickness(1),
                ClipToBounds = true,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center
            };
            var iconImage = new Image
            {
                Width = 30,
                Height = 30,
                Stretch = Stretch.Uniform
            };
            iconHost.Child = iconImage;

            var label = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(emote.Label) ? emote.Id : emote.Label,
                FontSize = 15,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center
            };

            var select = new CustomSelect
            {
                Height = 34,
                MinWidth = 280,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Center,
                Options = _commandOptions,
                SelectedValue = ResolveExistingCommandValue(emote.Command),
                Placeholder = "No action"
            };

            Grid.SetColumn(iconHost, 0);
            Grid.SetColumn(label, 1);
            Grid.SetColumn(select, 2);

            grid.Children.Add(iconHost);
            grid.Children.Add(label);
            grid.Children.Add(select);
            row.Child = grid;

            EmoteRowsHost.Children.Add(row);
            _emoteRows.Add(new EmoteRowState
            {
                Id = emote.Id,
                DefaultCommand = NormalizeCommand(emote.DefaultCommand),
                Select = select
            });

            _ = LoadEmoteIconAsync(iconImage, string.IsNullOrWhiteSpace(emote.IconKey) ? emote.Id : emote.Id);
        }
    }

    private void BuildChatRows(IReadOnlyList<ChatCommandDto> commands)
    {
        ChatRowsHost.Children.Clear();

        foreach (var command in commands)
        {
            var row = new Border
            {
                BorderBrush = new SolidColorBrush(Color.Parse("#214B6E95")),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(12, 8)
            };

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitions("110,*")
            };

            var trigger = new TextBlock
            {
                Text = command.Trigger,
                FontSize = 29 / 2.0,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center
            };

            var description = new TextBlock
            {
                Text = command.Description,
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap
            };

            Grid.SetColumn(trigger, 0);
            Grid.SetColumn(description, 1);
            grid.Children.Add(trigger);
            grid.Children.Add(description);

            row.Child = grid;
            ChatRowsHost.Children.Add(row);
        }
    }

    private async System.Threading.Tasks.Task LoadEmoteIconAsync(Image image, string emoteId)
    {
        if (_vm == null)
            return;

        if (string.IsNullOrWhiteSpace(emoteId))
            return;

        if (_iconCache.TryGetValue(emoteId, out var cached))
        {
            image.Source = cached;
            return;
        }

        var bytes = await _vm.Core.GetEmoteIconAsync(emoteId);
        if (bytes == null || bytes.Length == 0)
            return;

        try
        {
            using var ms = new MemoryStream(bytes);
            var bitmap = new Bitmap(ms);
            _iconCache[emoteId] = bitmap;
            image.Source = bitmap;
        }
        catch
        {
            // ignore invalid icon payloads
        }
    }

    private async void SaveBtn_Click(object? sender, RoutedEventArgs e)
    {
        if (_syncing || _vm == null)
            return;

        var emotesPatch = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in _emoteRows)
        {
            var selected = row.Select.SelectedValue?.ToString();
            emotesPatch[row.Id] = NormalizeCommand(selected);
        }

        var patch = new Dictionary<string, object?>
        {
            ["enabled"] = EnabledCheck.IsChecked == true,
            ["emotes"] = emotesPatch
        };

        await _vm.PatchConfigAsync(patch);
    }

    private void ResetBtn_Click(object? sender, RoutedEventArgs e)
    {
        foreach (var row in _emoteRows)
        {
            row.Select.SelectedValue = ResolveExistingCommandValue(row.DefaultCommand);
        }
    }

    private void Tabs_TabChanged(string tabId)
    {
        ApplyTabState(tabId);
    }

    private void ApplyTabState(string? tabId)
    {
        var emoteActive = string.Equals(tabId, "emoteCommands", StringComparison.OrdinalIgnoreCase);
        EmoteScroll.IsVisible = emoteActive;
        ChatPanel.IsVisible = !emoteActive;
        HeaderText.Text = emoteActive ? "Emote Commands" : "Chat Commands";
    }

    private static List<SelectOption> BuildCommandOptions(object? raw)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (raw is IEnumerable enumerable && raw is not string)
        {
            foreach (var item in enumerable)
            {
                if (item is not IDictionary dict)
                    continue;

                if (!TryGetString(dict, "value", out var value) || string.IsNullOrWhiteSpace(value))
                    continue;

                var label = value;
                if (TryGetString(dict, "label", out var candidate) && !string.IsNullOrWhiteSpace(candidate))
                    label = candidate;

                map[value.Trim()] = label.Trim();
            }
        }

        var options = new List<SelectOption>();
        foreach (var (value, label) in PreferredCommandOrder)
        {
            options.Add(new SelectOption(value, label));
            map.Remove(value);
        }

        foreach (var extra in map.OrderBy(kvp => kvp.Value, StringComparer.OrdinalIgnoreCase))
        {
            options.Add(new SelectOption(extra.Key, extra.Value));
        }

        return options;
    }

    private static List<EmoteDto> ParseEmotes(object? raw)
    {
        var result = new List<EmoteDto>();
        if (raw is not IEnumerable enumerable || raw is string)
            return result;

        foreach (var item in enumerable)
        {
            if (item is not IDictionary dict)
                continue;

            var id = ReadString(dict, "id") ?? ReadString(dict, "name") ?? string.Empty;
            if (string.IsNullOrWhiteSpace(id))
                continue;

            result.Add(new EmoteDto
            {
                Id = id.Trim(),
                Label = (ReadString(dict, "label") ?? id).Trim(),
                IconKey = (ReadString(dict, "iconKey") ?? id).Trim(),
                Command = NormalizeCommand(ReadString(dict, "command")),
                DefaultCommand = NormalizeCommand(ReadString(dict, "defaultCommand"))
            });
        }

        return result;
    }

    private static List<ChatCommandDto> ParseChatCommands(object? raw)
    {
        var result = new List<ChatCommandDto>();
        if (raw is not IEnumerable enumerable || raw is string)
            return result;

        foreach (var item in enumerable)
        {
            if (item is not IDictionary dict)
                continue;

            if (!TryGetString(dict, "trigger", out var trigger) || string.IsNullOrWhiteSpace(trigger))
                continue;

            var description = TryGetString(dict, "description", out var desc)
                ? desc
                : string.Empty;

            result.Add(new ChatCommandDto
            {
                Trigger = trigger.Trim(),
                Description = string.IsNullOrWhiteSpace(description) ? trigger.Trim() : description.Trim()
            });
        }

        return result;
    }

    private string ResolveExistingCommandValue(string? value)
    {
        var normalized = NormalizeCommand(value);
        if (_commandOptions.Any(opt => string.Equals(opt.Index?.ToString(), normalized, StringComparison.OrdinalIgnoreCase)))
            return normalized;

        return "none";
    }

    private static string NormalizeCommand(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "none" : normalized;
    }

    private static bool TryGetString(IDictionary dict, string key, out string value)
    {
        value = string.Empty;

        foreach (DictionaryEntry entry in dict)
        {
            if (!string.Equals(entry.Key?.ToString(), key, StringComparison.OrdinalIgnoreCase))
                continue;

            value = entry.Value?.ToString() ?? string.Empty;
            return true;
        }

        return false;
    }

    private static string? ReadString(IDictionary dict, string key)
    {
        return TryGetString(dict, key, out var value) ? value : null;
    }
}
