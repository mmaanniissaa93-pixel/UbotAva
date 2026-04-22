using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.Statistics;

public sealed class StatisticsRow
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}

public partial class StatisticsFeatureView : UserControl
{
    private PluginViewModelBase? _vm;
    private bool _syncing;

    private readonly ObservableCollection<StatisticsRow> _rows = new();

    public StatisticsFeatureView()
    {
        InitializeComponent();
        StatsGrid.ItemsSource = _rows;
    }

    public void Initialize(PluginViewModelBase vm, AppState state)
    {
        _vm = vm;
        StatsTabs.SetTabs(new[] { ("statistics", "Statistics") });
        StatsTabs.ActiveTabId = "statistics";
        _ = LoadConfigAsync();
    }

    public void UpdateFromState(JsonElement moduleState)
    {
        var root = moduleState;
        if (moduleState.ValueKind == JsonValueKind.Object && moduleState.TryGetProperty("stats", out var nestedStats))
            root = nestedStats;

        if (root.ValueKind != JsonValueKind.Object)
            return;

        var player = default(JsonElement);
        if (moduleState.ValueKind == JsonValueKind.Object && moduleState.TryGetProperty("player", out var playerNode))
            player = playerNode;

        var playerName = ReadString(player, "name", "-");
        var level = ReadInt(player, "level", 0);
        var health = ReadInt(player, "health", 0);
        var maxHealth = ReadInt(player, "maxHealth", 0);
        var mana = ReadInt(player, "mana", 0);
        var maxMana = ReadInt(player, "maxMana", 0);
        var experiencePercent = ReadDouble(player, "experiencePercent", 0);
        var gold = ReadLong(player, "gold", 0);
        var skillPoints = ReadInt(player, "skillPoints", 0);
        var statPoints = ReadInt(player, "statPoints", 0);

        var monsterCount = ReadInt(root, "monsterCount", 0);
        var inventoryCount = ReadInt(root, "inventoryCount", 0);
        var botRunning = ReadBool(root, "botRunning", false);
        var clientless = ReadBool(root, "clientless", true);
        var status = ReadString(root, "status", "Ready");

        _rows.Clear();
        AddRow("Player", playerName);
        AddRow("Level", level.ToString(CultureInfo.InvariantCulture));
        AddRow("HP", $"{health}/{maxHealth}");
        AddRow("MP", $"{mana}/{maxMana}");
        AddRow("Experience", $"{experiencePercent:F2} %");
        AddRow("Gold", gold.ToString("N0", CultureInfo.InvariantCulture));
        AddRow("Skill Points", skillPoints.ToString(CultureInfo.InvariantCulture));
        AddRow("Stat Points", statPoints.ToString(CultureInfo.InvariantCulture));
        AddRow("Monsters Nearby", monsterCount.ToString(CultureInfo.InvariantCulture));
        AddRow("Inventory Used", inventoryCount.ToString(CultureInfo.InvariantCulture));
        AddRow("Bot Running", botRunning ? "Yes" : "No");
        AddRow("Mode", clientless ? "Clientless" : "Client");

        StatusTextBlock.Text = $"Status: {status}";
    }

    private async System.Threading.Tasks.Task LoadConfigAsync()
    {
        if (_vm == null)
            return;

        await _vm.LoadConfigAsync();

        _syncing = true;
        try
        {
            ProgExperiencePerHour.IsChecked = _vm.BoolCfg("prog_Experience / hour", true);
            ProgGoldPerHour.IsChecked = _vm.BoolCfg("prog_Gold / hour", true);
            ProgKillsPerHour.IsChecked = _vm.BoolCfg("prog_Kills / hour", true);
            ProgItemsPerHour.IsChecked = _vm.BoolCfg("prog_Items / hour", true);
            ProgSkillPointsPerHour.IsChecked = _vm.BoolCfg("prog_Skill points / hour", true);

            TrackExperienceGained.IsChecked = _vm.BoolCfg("track_Experience gained", true);
            TrackGoldGained.IsChecked = _vm.BoolCfg("track_Gold Gained", true);
            TrackKills.IsChecked = _vm.BoolCfg("track_Kills", true);
            TrackLevelUps.IsChecked = _vm.BoolCfg("track_Level ups", true);
            TrackItemsLooted.IsChecked = _vm.BoolCfg("track_Items looted", true);
            TrackSkillPointsGained.IsChecked = _vm.BoolCfg("track_Skill points gained", true);
        }
        finally
        {
            _syncing = false;
        }
    }

    private void TrackingOption_CheckedChanged(object? sender, RoutedEventArgs e)
    {
        if (_syncing || _vm == null || sender is not CheckBox checkBox || checkBox.Tag is not string key)
            return;

        _ = _vm.PatchConfigAsync(new System.Collections.Generic.Dictionary<string, object?>
        {
            [key] = checkBox.IsChecked == true
        });
    }

    private void Reset_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        _ = _vm.PatchConfigAsync(new System.Collections.Generic.Dictionary<string, object?>
        {
            ["statisticsResetAt"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        });
        _ = _vm.PluginActionAsync("statistics.reset");
    }

    private void AddRow(string name, string value)
    {
        _rows.Add(new StatisticsRow
        {
            Name = name,
            Value = value
        });
    }

    private static string ReadString(JsonElement element, string name, string fallback)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var property)
            && property.ValueKind == JsonValueKind.String)
        {
            return property.GetString() ?? fallback;
        }

        return fallback;
    }

    private static int ReadInt(JsonElement element, string name, int fallback)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetInt32(out var value))
        {
            return value;
        }

        return fallback;
    }

    private static long ReadLong(JsonElement element, string name, long fallback)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetInt64(out var value))
        {
            return value;
        }

        return fallback;
    }

    private static double ReadDouble(JsonElement element, string name, double fallback)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var property)
            && property.ValueKind == JsonValueKind.Number
            && property.TryGetDouble(out var value))
        {
            return value;
        }

        return fallback;
    }

    private static bool ReadBool(JsonElement element, string name, bool fallback)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var property)
            && property.ValueKind is JsonValueKind.True or JsonValueKind.False)
        {
            return property.GetBoolean();
        }

        return fallback;
    }
}
