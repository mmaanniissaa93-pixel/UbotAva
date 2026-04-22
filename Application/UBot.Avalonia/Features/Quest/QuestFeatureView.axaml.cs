using Avalonia;
using Avalonia.Controls;
using System.Collections.ObjectModel;
using System.Text.Json;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.Quest;

public sealed class QuestRow
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Objectives { get; set; }
    public long RemainingTime { get; set; }
}

public partial class QuestFeatureView : UserControl
{
    private PluginViewModelBase? _vm;
    private AppState? _state;

    private readonly ObservableCollection<QuestRow> _rows = new();
    private TextBlock? _summaryText;
    private DataGrid? _grid;
    private bool _built;

    public QuestFeatureView()
    {
        InitializeComponent();
    }

    public void Initialize(PluginViewModelBase vm, AppState state)
    {
        _vm = vm;
        _state = state;
        Build();
    }

    public void UpdateFromState(JsonElement moduleState)
    {
        Build();
        var root = moduleState;
        if (moduleState.ValueKind == JsonValueKind.Object && moduleState.TryGetProperty("quests", out var questsNode))
            root = questsNode;

        if (root.ValueKind != JsonValueKind.Object)
            return;

        var activeCount = root.TryGetProperty("activeCount", out var activeCountNode) && activeCountNode.TryGetInt32(out var activeCountValue)
            ? activeCountValue
            : 0;
        var completedCount = root.TryGetProperty("completedCount", out var completedCountNode) && completedCountNode.TryGetInt32(out var completedCountValue)
            ? completedCountValue
            : 0;

        if (_summaryText != null)
            _summaryText.Text = $"Active quests: {activeCount}  |  Completed: {completedCount}";

        _rows.Clear();
        if (!root.TryGetProperty("active", out var activeNode) || activeNode.ValueKind != JsonValueKind.Array)
            return;

        foreach (var quest in activeNode.EnumerateArray())
        {
            if (quest.ValueKind != JsonValueKind.Object)
                continue;

            _rows.Add(new QuestRow
            {
                Name = quest.TryGetProperty("name", out var nameNode) ? nameNode.GetString() ?? string.Empty : string.Empty,
                Status = quest.TryGetProperty("status", out var statusNode) ? statusNode.GetString() ?? string.Empty : string.Empty,
                Objectives = quest.TryGetProperty("objectiveCount", out var objectivesNode) && objectivesNode.TryGetInt32(out var objectivesValue) ? objectivesValue : 0,
                RemainingTime = quest.TryGetProperty("remainingTime", out var remainingNode) && remainingNode.TryGetInt64(out var remainingValue) ? remainingValue : 0
            });
        }
    }

    private void Build()
    {
        if (_built)
            return;

        _built = true;
        TabStripCtrl.SetTabs(new[] { ("quests", "Quests") });
        TabStripCtrl.ActiveTabId = "quests";

        _summaryText = new TextBlock
        {
            Text = "Active quests: 0  |  Completed: 0",
            Classes = { "form-label" },
            Margin = new Thickness(0, 4, 0, 8)
        };

        _grid = new DataGrid
        {
            AutoGenerateColumns = false,
            Height = 420,
            IsReadOnly = true,
            CanUserResizeColumns = true,
            ItemsSource = _rows
        };

        _grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Name",
            Binding = new global::Avalonia.Data.Binding(nameof(QuestRow.Name)),
            Width = new DataGridLength(2, DataGridLengthUnitType.Star)
        });
        _grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Status",
            Binding = new global::Avalonia.Data.Binding(nameof(QuestRow.Status)),
            Width = new DataGridLength(1.2, DataGridLengthUnitType.Star)
        });
        _grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Objectives",
            Binding = new global::Avalonia.Data.Binding(nameof(QuestRow.Objectives)),
            Width = new DataGridLength(0.8, DataGridLengthUnitType.Star)
        });
        _grid.Columns.Add(new DataGridTextColumn
        {
            Header = "Remaining",
            Binding = new global::Avalonia.Data.Binding(nameof(QuestRow.RemainingTime)),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        });

        ContentHost.Children.Clear();
        ContentHost.Children.Add(_summaryText);
        ContentHost.Children.Add(_grid);
    }
}
