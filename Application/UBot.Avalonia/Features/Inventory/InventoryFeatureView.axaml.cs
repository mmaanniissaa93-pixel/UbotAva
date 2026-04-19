using Avalonia.Controls;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using UBot.Avalonia.ViewModels;
using UBot.Avalonia.Services;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace UBot.Avalonia.Features.Inventory;

public partial class InventoryItemRow : ObservableObject
{
    [ObservableProperty] private int _slot;
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private int _amount;
    [ObservableProperty] private int _opt;
    [ObservableProperty] private string _icon = "";
    [ObservableProperty] private Bitmap? _iconBitmap;

    public string OptDisplay => Opt > 0 ? $"+{Opt}" : "-";
    public IBrush OptColor => Opt > 0 ? Brushes.Gold : Brushes.Gray;
}

public partial class InventoryFeatureView : UserControl
{
    private PluginViewModelBase? _vm;
    private AppState? _state;
    private bool _isUpdating;
    private readonly Dictionary<string, Bitmap> _iconCache = new();

    private readonly string[] _tabs = { 
        "Inventory", "Equipment", "Avatars", "Grab Pet", 
        "Storage", "Guild Storage", "Job Transport", 
        "Specialty", "Job Equipment", "Fellow Pet" 
    };

    private readonly ObservableCollection<InventoryItemRow> _itemRows = new();

    public InventoryFeatureView() 
    { 
        InitializeComponent(); 
        InventoryGrid.ItemsSource = _itemRows;
    }

    public void Initialize(PluginViewModelBase vm, AppState state)
    {
        _vm = vm; 
        _state = state;

        MainTabs.SetTabs(_tabs.Select(t => (t, t)));
        MainTabs.TabChanged += MainTabs_TabChanged;
    }

    public void UpdateFromState(JsonElement moduleState)
    {
        if (_vm == null) return;
        _isUpdating = true;

        try
        {
            if (moduleState.TryGetProperty("inventory", out var invState))
            {
                // Update Selected Tab
                if (invState.TryGetProperty("selectedTab", out var selTabProp))
                {
                    var selTab = selTabProp.GetString() ?? "Inventory";
                    if (MainTabs.ActiveTabId != selTab)
                        MainTabs.ActiveTabId = selTab;
                }

                // Update Items
                if (invState.TryGetProperty("items", out var itemsProp))
                {
                    var dtos = JsonSerializer.Deserialize<List<InventoryItemDto>>(itemsProp.GetRawText());
                    SyncItems(dtos ?? new List<InventoryItemDto>());
                }

                // Update Free Slots
                if (invState.TryGetProperty("freeSlots", out var freeProp) && invState.TryGetProperty("totalSlots", out var totalProp))
                {
                    var free = freeProp.GetInt32();
                    var total = totalProp.GetInt32();
                    FreeSlotCount.Text = total.ToString(); // Display total inside circle like the image
                    FreeSlotText.Text = $"{free} free";
                }

                // Update Auto Sort
                if (invState.TryGetProperty("autoSort", out var autoSortProp))
                {
                    AutoSortCheck.IsChecked = autoSortProp.GetBoolean();
                }

                // Update UI visibility based on tab
                var currentTab = MainTabs.ActiveTabId ?? "Inventory";
                SortPanel.IsVisible = currentTab == "Inventory";
                SortBtn.IsVisible = currentTab == "Inventory";
            }
        }
        finally
        {
            _isUpdating = false;
        }
    }

    private void SyncItems(List<InventoryItemDto> dtos)
    {
        // Simple sync: if count differs or items differ, rebuild
        // For production, a more efficient diffing would be better
        if (_itemRows.Count != dtos.Count)
        {
            _itemRows.Clear();
            foreach (var dto in dtos)
            {
                var row = new InventoryItemRow
                {
                    Slot = dto.Slot,
                    Name = dto.Name,
                    Amount = dto.Amount,
                    Opt = dto.Opt,
                    Icon = dto.Icon
                };
                _itemRows.Add(row);
                _ = LoadIconAsync(row);
            }
        }
        else
        {
            for (int i = 0; i < dtos.Count; i++)
            {
                var dto = dtos[i];
                var row = _itemRows[i];
                if (row.Slot != dto.Slot || row.Name != dto.Name || row.Amount != dto.Amount || row.Opt != dto.Opt || row.Icon != dto.Icon)
                {
                    row.Slot = dto.Slot;
                    row.Name = dto.Name;
                    row.Amount = dto.Amount;
                    row.Opt = dto.Opt;
                    bool iconChanged = row.Icon != dto.Icon;
                    row.Icon = dto.Icon;
                    if (iconChanged) _ = LoadIconAsync(row);
                }
            }
        }
    }

    private async System.Threading.Tasks.Task LoadIconAsync(InventoryItemRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Icon) || _vm == null) return;

        if (_iconCache.TryGetValue(row.Icon, out var cached))
        {
            row.IconBitmap = cached;
            return;
        }

        // Reuse GetIconAsync which extracts from Pk2
        var bytes = await _vm.GetIconAsync(row.Icon);

        if (bytes != null)
        {
            try
            {
                using var ms = new System.IO.MemoryStream(bytes);
                var bitmap = new Bitmap(ms);
                _iconCache[row.Icon] = bitmap;
                row.IconBitmap = bitmap;
            }
            catch { }
        }
    }

    private async void MainTabs_TabChanged(string selected)
    {
        if (_isUpdating || _vm == null) return;
        if (string.IsNullOrEmpty(selected)) return;

        await _vm.PluginActionAsync("inventory.set-type", new Dictionary<string, object?> { ["type"] = selected });
    }

    private async void AutoSort_Changed(object? sender, RoutedEventArgs e)
    {
        if (_isUpdating || _vm == null) return;

        await _vm.PluginActionAsync("inventory.set-auto-sort", new Dictionary<string, object?> { ["value"] = AutoSortCheck.IsChecked });
    }

    private async void Sort_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null) return;
        await _vm.PluginActionAsync("inventory.sort");
    }
}
