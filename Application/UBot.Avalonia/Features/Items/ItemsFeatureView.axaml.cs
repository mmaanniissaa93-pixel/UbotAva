using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using UBot.Avalonia.Controls;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;
using UBot.Core.Client.ReferenceObjects;

namespace UBot.Avalonia.Features.Items;

public sealed class ItemsSimpleRow
{
    public string CodeName { get; set; } = string.Empty;
    public string Display { get; set; } = string.Empty;
}

public sealed class ItemsShoppingTargetRow
{
    public string ShopCodeName { get; set; } = string.Empty;
    public string ItemCodeName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Amount { get; set; }
}

public sealed class ItemsFilterRow
{
    public string CodeName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Gender { get; set; } = "-";
    public string Pickup { get; set; } = string.Empty;
    public string Sell { get; set; } = string.Empty;
    public string Store { get; set; } = string.Empty;
}

public sealed class ItemsPickupRow
{
    public string Name { get; set; } = string.Empty;
    public string PickOnlyChar { get; set; } = "No";
}

public partial class ItemsFeatureView : UserControl
{
    private sealed class ItemCatalogEntry
    {
        public string CodeName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public int Degree { get; set; }
        public int Gender { get; set; }
        public int Country { get; set; }
        public int Rarity { get; set; }
        public bool IsEquip { get; set; }
        public bool IsQuest { get; set; }
        public bool IsAmmunition { get; set; }
        public int TypeId2 { get; set; }
        public int TypeId3 { get; set; }
        public int TypeId4 { get; set; }
    }

    private sealed class ShopItemEntry
    {
        public string CodeName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public bool IsEquip { get; set; }
        public int Level { get; set; }
        public int Country { get; set; }
    }

    private sealed class ShopCatalogEntry
    {
        public string CodeName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<ShopItemEntry> Items { get; set; } = new();
    }

    private sealed class ShoppingTargetEntry
    {
        public string ShopCodeName { get; set; } = string.Empty;
        public string ItemCodeName { get; set; } = string.Empty;
        public int Amount { get; set; } = 1;
    }

    private PluginViewModelBase? _vm;
    private bool _syncing;
    private string _activeTab = "shopping";

    private readonly ObservableCollection<ItemsSimpleRow> _sourceRows = new();
    private readonly ObservableCollection<ItemsShoppingTargetRow> _targetRows = new();
    private readonly ObservableCollection<ItemsSimpleRow> _sellRows = new();
    private readonly ObservableCollection<ItemsSimpleRow> _storeRows = new();
    private readonly ObservableCollection<ItemsFilterRow> _itemRows = new();
    private readonly ObservableCollection<ItemsPickupRow> _pickupRows = new();

    private readonly List<ItemCatalogEntry> _itemCatalog = new();
    private readonly List<ShopCatalogEntry> _shopCatalog = new();
    private readonly List<ShoppingTargetEntry> _shoppingTargets = new();
    private readonly HashSet<string> _sellFilter = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _storeFilter = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, bool> _pickupFilter = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _selectedCategories = new(StringComparer.OrdinalIgnoreCase);

    private string _selectedShopCodeName = string.Empty;
    private string _selectedSourceItemCodeName = string.Empty;
    private string _selectedTargetItemCodeName = string.Empty;
    private string _selectedFilterItemCodeName = string.Empty;

    public ItemsFeatureView()
    {
        InitializeComponent();

        SourceItemsList.ItemsSource = _sourceRows;
        TargetItemsList.ItemsSource = _targetRows;
        SellFilterList.ItemsSource = _sellRows;
        StoreFilterList.ItemsSource = _storeRows;
        FilteredItemsList.ItemsSource = _itemRows;
        PickupFilterList.ItemsSource = _pickupRows;

        ShopSelect.SelectionChanged += ShopSelect_Changed;
        ShoppingItemSelect.SelectionChanged += value => _selectedSourceItemCodeName = value?.ToString() ?? string.Empty;
        TargetItemSelect.SelectionChanged += value => _selectedTargetItemCodeName = value?.ToString() ?? string.Empty;
        FilterItemSelect.SelectionChanged += value => _selectedFilterItemCodeName = value?.ToString() ?? string.Empty;
    }

    public void Initialize(PluginViewModelBase vm, AppState state)
    {
        _vm = vm;

        MainTabs.ActiveTabId = _activeTab;
        MainTabs.SetTabs(new[]
        {
            ("shopping", "Shopping"),
            ("itemFilter", "Item Filter"),
            ("pickupSettings", "Pickup Settings")
        });
        MainTabs.TabChanged += tab =>
        {
            _activeTab = tab;
            SyncTabVisibility();
        };

        SyncTabVisibility();
        _ = LoadFromConfigAsync();
    }

    public void UpdateFromState(JsonElement moduleState)
    {
        if (_vm == null || _itemCatalog.Count > 0)
            return;

        _ = LoadFromConfigAsync();
    }

    private async System.Threading.Tasks.Task LoadFromConfigAsync()
    {
        if (_vm == null)
            return;

        await _vm.LoadConfigAsync();

        _syncing = true;
        try
        {
            ReadCatalogFromConfig();
            ReadFiltersFromConfig();
            ReadShoppingTargetsFromConfig();

            RunWhenInTownCheck.IsChecked = _vm.BoolCfg("shoppingEnabled", true);
            RepairAllGearCheck.IsChecked = _vm.BoolCfg("repairGear", true);
            SellFromPetCheck.IsChecked = _vm.BoolCfg("sellPetItems", true);
            StoreFromPetCheck.IsChecked = _vm.BoolCfg("storePetItems", true);

            UseAbilityPetCheck.IsChecked = _vm.BoolCfg("pickupUseAbilityPet", true);
            JustPickMineCheck.IsChecked = _vm.BoolCfg("pickupJustMyItems", false);
            DontPickupBerzerkCheck.IsChecked = _vm.BoolCfg("pickupDontInBerzerk", true);
            DontPickupBottingCheck.IsChecked = _vm.BoolCfg("pickupDontWhileBotting", false);
            PickupGoldCheck.IsChecked = _vm.BoolCfg("pickupGold", true);
            PickupBlueCheck.IsChecked = _vm.BoolCfg("pickupBlueItems", true);
            PickupQuestCheck.IsChecked = _vm.BoolCfg("pickupQuestItems", true);
            PickupRareCheck.IsChecked = _vm.BoolCfg("pickupRareItems", true);
            PickupAnyEquipCheck.IsChecked = _vm.BoolCfg("pickupAnyEquips", true);
            PickupEverythingCheck.IsChecked = _vm.BoolCfg("pickupEverything", true);

            ShowEquipmentCheck.IsChecked = _vm.BoolCfg("showEquipmentOnShopping", false);
            _selectedShopCodeName = _vm.TextCfg("shoppingShopCodeName", string.Empty);
            EnsureSelectedShop();
            RefreshAll();
        }
        finally
        {
            _syncing = false;
        }
    }

    private void ReadCatalogFromConfig()
    {
        _itemCatalog.Clear();
        _shopCatalog.Clear();

        if (_vm == null)
            return;

        var rawItems = _vm.ObjCfg("itemCatalog");
        if (rawItems != null)
        {
            foreach (var row in EnumerateDictionaryRows(rawItems))
            {
                var codeName = GetString(row, "codeName");
                if (string.IsNullOrWhiteSpace(codeName))
                    continue;

                _itemCatalog.Add(new ItemCatalogEntry
                {
                    CodeName = codeName,
                    Name = GetString(row, "name", codeName),
                    Level = GetInt(row, "level", 0),
                    Degree = GetInt(row, "degree", 0),
                    Gender = GetInt(row, "gender", 2),
                    Country = GetInt(row, "country", 3),
                    Rarity = GetInt(row, "rarity", 0),
                    IsEquip = GetBool(row, "isEquip", false),
                    IsQuest = GetBool(row, "isQuest", false),
                    IsAmmunition = GetBool(row, "isAmmunition", false),
                    TypeId2 = GetInt(row, "typeId2", 0),
                    TypeId3 = GetInt(row, "typeId3", 0),
                    TypeId4 = GetInt(row, "typeId4", 0)
                });
            }
        }

        var rawShops = _vm.ObjCfg("shopCatalog");
        if (rawShops != null)
        {
            foreach (var row in EnumerateDictionaryRows(rawShops))
            {
                var shop = new ShopCatalogEntry
                {
                    CodeName = GetString(row, "codeName"),
                    Name = GetString(row, "name")
                };

                if (row.TryGetValue("items", out var rawShopItems))
                {
                    foreach (var rawShopItem in EnumerateDictionaryRows(rawShopItems))
                    {
                        var codeName = GetString(rawShopItem, "codeName");
                        if (string.IsNullOrWhiteSpace(codeName))
                            continue;

                        shop.Items.Add(new ShopItemEntry
                        {
                            CodeName = codeName,
                            Name = GetString(rawShopItem, "name", ResolveItemName(codeName)),
                            IsEquip = GetBool(rawShopItem, "isEquip", false),
                            Level = GetInt(rawShopItem, "level", 0),
                            Country = GetInt(rawShopItem, "country", 3)
                        });
                    }
                }

                if (!string.IsNullOrWhiteSpace(shop.CodeName))
                    _shopCatalog.Add(shop);
            }
        }
    }

    private void ReadFiltersFromConfig()
    {
        _sellFilter.Clear();
        _storeFilter.Clear();
        _pickupFilter.Clear();

        if (_vm == null)
            return;

        foreach (var codeName in EnumerateStringRows(_vm.ObjCfg("sellFilter")))
            _sellFilter.Add(codeName);

        foreach (var codeName in EnumerateStringRows(_vm.ObjCfg("storeFilter")))
            _storeFilter.Add(codeName);

        foreach (var row in EnumerateDictionaryRows(_vm.ObjCfg("pickupFilter")))
        {
            var codeName = GetString(row, "codeName");
            if (string.IsNullOrWhiteSpace(codeName))
                continue;
            _pickupFilter[codeName] = GetBool(row, "pickOnlyChar", false);
        }
    }

    private void ReadShoppingTargetsFromConfig()
    {
        _shoppingTargets.Clear();
        if (_vm == null)
            return;

        var rawTargets = _vm.ObjCfg("shoppingTargets");
        if (rawTargets == null)
            return;

        foreach (var row in EnumerateDictionaryRows(rawTargets))
        {
            var shopCodeName = GetString(row, "shopCodeName");
            var itemCodeName = GetString(row, "itemCodeName");
            if (string.IsNullOrWhiteSpace(shopCodeName) || string.IsNullOrWhiteSpace(itemCodeName))
                continue;

            _shoppingTargets.Add(new ShoppingTargetEntry
            {
                ShopCodeName = shopCodeName,
                ItemCodeName = itemCodeName,
                Amount = Math.Clamp(GetInt(row, "amount", 1), 1, 50000)
            });
        }

        var deduped = _shoppingTargets
            .GroupBy(item => $"{item.ShopCodeName}|{item.ItemCodeName}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .ToList();
        _shoppingTargets.Clear();
        _shoppingTargets.AddRange(deduped);
    }

    private void RefreshAll()
    {
        RefreshShoppingSections();
        RefreshSellStoreRows();
        RefreshPickupRows();
        RefreshItemFilterRows();
    }

    private void SyncTabVisibility()
    {
        ShoppingPanel.IsVisible = _activeTab == "shopping";
        ItemFilterPanel.IsVisible = _activeTab == "itemFilter";
        PickupSettingsPanel.IsVisible = _activeTab == "pickupSettings";
    }

    private void EnsureSelectedShop()
    {
        if (_shopCatalog.Count == 0)
        {
            _selectedShopCodeName = string.Empty;
            return;
        }

        if (!_shopCatalog.Any(shop => string.Equals(shop.CodeName, _selectedShopCodeName, StringComparison.OrdinalIgnoreCase)))
            _selectedShopCodeName = _shopCatalog[0].CodeName;
    }

    private void RefreshShoppingSections()
    {
        EnsureSelectedShop();

        var shopOptions = _shopCatalog
            .Select(shop => new SelectOption(shop.CodeName, shop.Name))
            .Cast<SelectOption>()
            .ToList();
        ShopSelect.Options = shopOptions;
        ShopSelect.SelectedValue = _selectedShopCodeName;

        var selectedShop = _shopCatalog.FirstOrDefault(shop =>
            string.Equals(shop.CodeName, _selectedShopCodeName, StringComparison.OrdinalIgnoreCase));

        var search = ShoppingSearchBox.Text?.Trim() ?? string.Empty;
        var showEquipment = ShowEquipmentCheck.IsChecked == true;
        var sourceItems = (selectedShop?.Items ?? new List<ShopItemEntry>())
            .Where(item => showEquipment || !item.IsEquip)
            .Where(item =>
            {
                if (string.IsNullOrWhiteSpace(search))
                    return true;
                return item.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                       || item.CodeName.Contains(search, StringComparison.OrdinalIgnoreCase);
            })
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _sourceRows.Clear();
        foreach (var item in sourceItems)
        {
            _sourceRows.Add(new ItemsSimpleRow
            {
                CodeName = item.CodeName,
                Display = item.Name
            });
        }

        if (!_sourceRows.Any(row => string.Equals(row.CodeName, _selectedSourceItemCodeName, StringComparison.OrdinalIgnoreCase)))
            _selectedSourceItemCodeName = _sourceRows.FirstOrDefault()?.CodeName ?? string.Empty;

        ShoppingItemSelect.Options = _sourceRows
            .Select(row => new SelectOption(row.CodeName, row.Display))
            .ToList();
        ShoppingItemSelect.SelectedValue = _selectedSourceItemCodeName;

        var targetsForShop = _shoppingTargets
            .Where(item => string.Equals(item.ShopCodeName, _selectedShopCodeName, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => ResolveItemName(item.ItemCodeName), StringComparer.OrdinalIgnoreCase)
            .ToList();

        _targetRows.Clear();
        foreach (var target in targetsForShop)
        {
            _targetRows.Add(new ItemsShoppingTargetRow
            {
                ShopCodeName = target.ShopCodeName,
                ItemCodeName = target.ItemCodeName,
                Name = ResolveItemName(target.ItemCodeName),
                Amount = target.Amount
            });
        }

        if (!_targetRows.Any(row => string.Equals(row.ItemCodeName, _selectedTargetItemCodeName, StringComparison.OrdinalIgnoreCase)))
            _selectedTargetItemCodeName = _targetRows.FirstOrDefault()?.ItemCodeName ?? string.Empty;

        TargetItemSelect.Options = _targetRows
            .Select(row => new SelectOption(row.ItemCodeName, row.Name))
            .ToList();
        TargetItemSelect.SelectedValue = _selectedTargetItemCodeName;
    }

    private void RefreshSellStoreRows()
    {
        _sellRows.Clear();
        foreach (var codeName in _sellFilter.OrderBy(value => ResolveItemName(value), StringComparer.OrdinalIgnoreCase))
        {
            _sellRows.Add(new ItemsSimpleRow
            {
                CodeName = codeName,
                Display = ResolveItemName(codeName)
            });
        }

        _storeRows.Clear();
        foreach (var codeName in _storeFilter.OrderBy(value => ResolveItemName(value), StringComparer.OrdinalIgnoreCase))
        {
            _storeRows.Add(new ItemsSimpleRow
            {
                CodeName = codeName,
                Display = ResolveItemName(codeName)
            });
        }
    }

    private void RefreshPickupRows()
    {
        _pickupRows.Clear();
        foreach (var pair in _pickupFilter.OrderBy(pair => ResolveItemName(pair.Key), StringComparer.OrdinalIgnoreCase))
        {
            _pickupRows.Add(new ItemsPickupRow
            {
                Name = ResolveItemName(pair.Key),
                PickOnlyChar = pair.Value ? "Yes" : "No"
            });
        }
    }

    private void RefreshItemFilterRows()
    {
        var search = FilterSearchBox.Text?.Trim() ?? string.Empty;
        var degreeMin = ParseInt(DegreeMinBox.Text, 0);
        var degreeMax = ParseInt(DegreeMaxBox.Text, 0);

        var male = FilterMaleCheck.IsChecked == true;
        var female = FilterFemaleCheck.IsChecked == true;
        var china = FilterChinaCheck.IsChecked == true;
        var europe = FilterEuropeCheck.IsChecked == true;
        var sox = FilterSoxCheck.IsChecked == true;

        var filtered = _itemCatalog.Where(item =>
        {
            if (!string.IsNullOrWhiteSpace(search)
                && !item.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                && !item.CodeName.Contains(search, StringComparison.OrdinalIgnoreCase))
                return false;

            if (degreeMin > 0 && item.Degree < degreeMin)
                return false;
            if (degreeMax > 0 && item.Degree > degreeMax)
                return false;

            if (male ^ female)
            {
                var requiredGender = male ? (int)ObjectGender.Male : (int)ObjectGender.Female;
                if (item.Gender != requiredGender)
                    return false;
            }

            if (china ^ europe)
            {
                var requiredCountry = china ? (int)ObjectCountry.Chinese : (int)ObjectCountry.Europe;
                if (item.Country != requiredCountry)
                    return false;
            }

            if (sox && item.Rarity < (int)ObjectRarity.ClassC)
                return false;

            if (_selectedCategories.Count > 0 && !_selectedCategories.Any(category => MatchesCategory(item, category)))
                return false;

            return true;
        }).OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase).ToList();

        _itemRows.Clear();
        foreach (var item in filtered)
        {
            _itemRows.Add(new ItemsFilterRow
            {
                CodeName = item.CodeName,
                Name = item.Name,
                Level = item.Level,
                Gender = item.Gender switch
                {
                    (int)ObjectGender.Male => "M",
                    (int)ObjectGender.Female => "F",
                    _ => "-"
                },
                Pickup = _pickupFilter.ContainsKey(item.CodeName) ? "Y" : string.Empty,
                Sell = _sellFilter.Contains(item.CodeName) ? "Y" : string.Empty,
                Store = _storeFilter.Contains(item.CodeName) ? "Y" : string.Empty
            });
        }

        if (!_itemRows.Any(row => string.Equals(row.CodeName, _selectedFilterItemCodeName, StringComparison.OrdinalIgnoreCase)))
            _selectedFilterItemCodeName = _itemRows.FirstOrDefault()?.CodeName ?? string.Empty;

        FilterItemSelect.Options = filtered
            .Select(item => new SelectOption(item.CodeName, item.Name))
            .ToList();
        FilterItemSelect.SelectedValue = _selectedFilterItemCodeName;
    }

    private async System.Threading.Tasks.Task SaveShoppingTargetsAsync()
    {
        if (_vm == null)
            return;

        var payload = _shoppingTargets
            .Select(target => new Dictionary<string, object?>
            {
                ["shopCodeName"] = target.ShopCodeName,
                ["itemCodeName"] = target.ItemCodeName,
                ["amount"] = target.Amount
            })
            .Cast<object?>()
            .ToList();

        await _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            ["shoppingTargets"] = payload
        });
    }

    private async System.Threading.Tasks.Task SaveFiltersAsync()
    {
        if (_vm == null)
            return;

        var pickupPayload = _pickupFilter
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => new Dictionary<string, object?>
            {
                ["codeName"] = pair.Key,
                ["pickOnlyChar"] = pair.Value
            })
            .Cast<object?>()
            .ToList();

        await _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            ["sellFilter"] = _sellFilter.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList(),
            ["storeFilter"] = _storeFilter.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToList(),
            ["pickupFilter"] = pickupPayload
        });
    }

    private async System.Threading.Tasks.Task PatchShoppingTogglesAsync()
    {
        if (_vm == null)
            return;

        await _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            ["shoppingEnabled"] = RunWhenInTownCheck.IsChecked == true,
            ["repairGear"] = RepairAllGearCheck.IsChecked == true,
            ["sellPetItems"] = SellFromPetCheck.IsChecked == true,
            ["storePetItems"] = StoreFromPetCheck.IsChecked == true
        });
    }

    private async System.Threading.Tasks.Task PatchPickupTogglesAsync()
    {
        if (_vm == null)
            return;

        await _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            ["pickupUseAbilityPet"] = UseAbilityPetCheck.IsChecked == true,
            ["pickupJustMyItems"] = JustPickMineCheck.IsChecked == true,
            ["pickupDontInBerzerk"] = DontPickupBerzerkCheck.IsChecked == true,
            ["pickupDontWhileBotting"] = DontPickupBottingCheck.IsChecked == true,
            ["pickupGold"] = PickupGoldCheck.IsChecked == true,
            ["pickupBlueItems"] = PickupBlueCheck.IsChecked == true,
            ["pickupQuestItems"] = PickupQuestCheck.IsChecked == true,
            ["pickupRareItems"] = PickupRareCheck.IsChecked == true,
            ["pickupAnyEquips"] = PickupAnyEquipCheck.IsChecked == true,
            ["pickupEverything"] = PickupEverythingCheck.IsChecked == true
        });
    }

    private async System.Threading.Tasks.Task PatchSettingAsync(string key, object? value)
    {
        if (_vm == null)
            return;

        await _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            [key] = value
        });
    }

    private void ShopSelect_Changed(object value)
    {
        if (_syncing)
            return;

        _selectedShopCodeName = value?.ToString() ?? string.Empty;
        _ = PatchSettingAsync("shoppingShopCodeName", _selectedShopCodeName);
        RefreshShoppingSections();
    }

    private void ShoppingSearchBox_Changed(object? sender, TextChangedEventArgs e)
    {
        RefreshShoppingSections();
    }

    private void ShowEquipmentCheck_Changed(object? sender, RoutedEventArgs e)
    {
        if (_syncing)
            return;

        _ = PatchSettingAsync("showEquipmentOnShopping", ShowEquipmentCheck.IsChecked == true);
        RefreshShoppingSections();
    }

    private async void AddOrUpdateShopping_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedShopCodeName))
            return;

        var itemCodeName = _selectedSourceItemCodeName;
        if (string.IsNullOrWhiteSpace(itemCodeName))
            return;

        var amount = Math.Clamp(ParseInt(ShoppingAmountBox.Text, 1), 1, 50000);
        var existing = _shoppingTargets.FirstOrDefault(target =>
            string.Equals(target.ShopCodeName, _selectedShopCodeName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(target.ItemCodeName, itemCodeName, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            _shoppingTargets.Add(new ShoppingTargetEntry
            {
                ShopCodeName = _selectedShopCodeName,
                ItemCodeName = itemCodeName,
                Amount = amount
            });
        }
        else
        {
            existing.Amount = amount;
        }

        await SaveShoppingTargetsAsync();
        RefreshShoppingSections();
    }

    private async void RemoveShopping_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedShopCodeName))
            return;

        var itemCodeName = _selectedTargetItemCodeName;
        if (string.IsNullOrWhiteSpace(itemCodeName))
            return;

        _shoppingTargets.RemoveAll(target =>
            string.Equals(target.ShopCodeName, _selectedShopCodeName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(target.ItemCodeName, itemCodeName, StringComparison.OrdinalIgnoreCase));

        await SaveShoppingTargetsAsync();
        RefreshShoppingSections();
    }

    private async void OnQuickAddShoppingFromSource_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: ItemsSimpleRow row })
            return;

        await QuickAddShoppingAsync(row.CodeName);
    }

    private async void OnQuickAddShoppingFromFilter_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: ItemsFilterRow row })
            return;

        await QuickAddShoppingAsync(row.CodeName);
    }

    private async void OnQuickRemoveShoppingFromTarget_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: ItemsShoppingTargetRow row })
            return;

        _selectedTargetItemCodeName = row.ItemCodeName;
        TargetItemSelect.SelectedValue = row.ItemCodeName;

        if (string.IsNullOrWhiteSpace(_selectedShopCodeName))
            return;

        _shoppingTargets.RemoveAll(target =>
            string.Equals(target.ShopCodeName, _selectedShopCodeName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(target.ItemCodeName, row.ItemCodeName, StringComparison.OrdinalIgnoreCase));

        await SaveShoppingTargetsAsync();
        RefreshShoppingSections();
    }

    private async void OnQuickSellFromFilter_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: ItemsFilterRow row })
            return;

        _selectedFilterItemCodeName = row.CodeName;
        FilterItemSelect.SelectedValue = row.CodeName;
        _sellFilter.Add(row.CodeName);
        await SaveFiltersAsync();
        RefreshSellStoreRows();
        RefreshItemFilterRows();
    }

    private async void OnQuickStoreFromFilter_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: ItemsFilterRow row })
            return;

        _selectedFilterItemCodeName = row.CodeName;
        FilterItemSelect.SelectedValue = row.CodeName;
        _storeFilter.Add(row.CodeName);
        await SaveFiltersAsync();
        RefreshSellStoreRows();
        RefreshItemFilterRows();
    }

    private async void OnQuickPickupFromFilter_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: ItemsFilterRow row })
            return;

        _selectedFilterItemCodeName = row.CodeName;
        FilterItemSelect.SelectedValue = row.CodeName;
        _pickupFilter[row.CodeName] = _pickupFilter.TryGetValue(row.CodeName, out var pickOnlyChar) && pickOnlyChar;
        await SaveFiltersAsync();
        RefreshPickupRows();
        RefreshItemFilterRows();
    }

    private async void OnPickupOnlyCharacterFromFilter_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: ItemsFilterRow row })
            return;

        _selectedFilterItemCodeName = row.CodeName;
        FilterItemSelect.SelectedValue = row.CodeName;
        _pickupFilter[row.CodeName] = true;
        await SaveFiltersAsync();
        RefreshPickupRows();
        RefreshItemFilterRows();
    }

    private async void OnRemoveAllFromFilterContext_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: ItemsFilterRow row })
            return;

        _selectedFilterItemCodeName = row.CodeName;
        FilterItemSelect.SelectedValue = row.CodeName;

        _pickupFilter.Remove(row.CodeName);
        _sellFilter.Remove(row.CodeName);
        _storeFilter.Remove(row.CodeName);

        await SaveFiltersAsync();
        RefreshPickupRows();
        RefreshSellStoreRows();
        RefreshItemFilterRows();
    }

    private void SourceItemsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SourceItemsList.SelectedItem is not ItemsSimpleRow row)
            return;

        _selectedSourceItemCodeName = row.CodeName;
        ShoppingItemSelect.SelectedValue = row.CodeName;
    }

    private void TargetItemsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (TargetItemsList.SelectedItem is not ItemsShoppingTargetRow row)
            return;

        _selectedTargetItemCodeName = row.ItemCodeName;
        TargetAmountBox.Text = row.Amount.ToString(CultureInfo.InvariantCulture);
        TargetItemSelect.SelectedValue = row.ItemCodeName;
    }

    private async void ShoppingToggle_Changed(object? sender, RoutedEventArgs e)
    {
        if (_syncing)
            return;
        await PatchShoppingTogglesAsync();
    }

    private async void PickupToggle_Changed(object? sender, RoutedEventArgs e)
    {
        if (_syncing)
            return;
        await PatchPickupTogglesAsync();
    }

    private void RunItemSearch_Click(object? sender, RoutedEventArgs e)
    {
        RefreshItemFilterRows();
    }

    private void ResetItemSearch_Click(object? sender, RoutedEventArgs e)
    {
        _syncing = true;
        try
        {
            FilterSearchBox.Text = string.Empty;
            DegreeMinBox.Text = "0";
            DegreeMaxBox.Text = "0";
            FilterMaleCheck.IsChecked = false;
            FilterFemaleCheck.IsChecked = false;
            FilterChinaCheck.IsChecked = false;
            FilterEuropeCheck.IsChecked = false;
            FilterSoxCheck.IsChecked = false;
            _selectedCategories.Clear();
        }
        finally
        {
            _syncing = false;
        }

        foreach (var checkBox in ItemFilterPanel.GetLogicalDescendants().OfType<CheckBox>())
        {
            if (checkBox.Tag is string)
                checkBox.IsChecked = false;
        }

        RefreshItemFilterRows();
    }

    private void FilterFlag_Changed(object? sender, RoutedEventArgs e)
    {
        if (_syncing)
            return;
        RefreshItemFilterRows();
    }

    private void CategoryFlag_Changed(object? sender, RoutedEventArgs e)
    {
        if (sender is not CheckBox checkBox || checkBox.Tag is not string category)
            return;

        if (checkBox.IsChecked == true)
            _selectedCategories.Add(category);
        else
            _selectedCategories.Remove(category);

        RefreshItemFilterRows();
    }

    private void FilteredItemsList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (FilteredItemsList.SelectedItem is not ItemsFilterRow row)
            return;

        _selectedFilterItemCodeName = row.CodeName;
        FilterItemSelect.SelectedValue = row.CodeName;
    }

    private async void AddPickupFilter_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedFilterItemCodeName))
            return;

        _pickupFilter[_selectedFilterItemCodeName] = _pickupFilter.TryGetValue(_selectedFilterItemCodeName, out var pickOnlyChar) && pickOnlyChar;
        await SaveFiltersAsync();
        RefreshPickupRows();
        RefreshItemFilterRows();
    }

    private async void AddSellFilter_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedFilterItemCodeName))
            return;

        _sellFilter.Add(_selectedFilterItemCodeName);
        await SaveFiltersAsync();
        RefreshSellStoreRows();
        RefreshItemFilterRows();
    }

    private async void AddStoreFilter_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedFilterItemCodeName))
            return;

        _storeFilter.Add(_selectedFilterItemCodeName);
        await SaveFiltersAsync();
        RefreshSellStoreRows();
        RefreshItemFilterRows();
    }

    private async void RemoveAllFilters_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedFilterItemCodeName))
            return;

        _pickupFilter.Remove(_selectedFilterItemCodeName);
        _sellFilter.Remove(_selectedFilterItemCodeName);
        _storeFilter.Remove(_selectedFilterItemCodeName);

        await SaveFiltersAsync();
        RefreshPickupRows();
        RefreshSellStoreRows();
        RefreshItemFilterRows();
    }

    private static bool MatchesCategory(ItemCatalogEntry item, string category)
    {
        category = category.ToLowerInvariant();
        var codeName = item.CodeName.ToLowerInvariant();

        return category switch
        {
            "clothes" => item.IsEquip && item.TypeId3 is 1 or 2 or 3 or 9 or 10 or 11,
            "light" => codeName.Contains("_light_"),
            "heavy" => codeName.Contains("_heavy_"),
            "head" => codeName.Contains("_ha_") || codeName.Contains("_head_"),
            "shoulder" => codeName.Contains("_sa_") || codeName.Contains("_shoulder_"),
            "chest" => codeName.Contains("_ba_") || codeName.Contains("_chest_"),
            "legs" => codeName.Contains("_la_") || codeName.Contains("_legs_"),
            "boots" => codeName.Contains("_fa_") || codeName.Contains("_boots_"),
            "hands" => codeName.Contains("_aa_") || codeName.Contains("_hands_"),
            "rings" => codeName.Contains("ring"),
            "necklace" => codeName.Contains("necklace"),
            "earrings" => codeName.Contains("earring"),
            "blade" => codeName.Contains("blade"),
            "sword" => codeName.Contains("sword"),
            "glaive" => codeName.Contains("glaive"),
            "spear" => codeName.Contains("spear"),
            "bow" => codeName.Contains("bow"),
            "staff" => codeName.Contains("staff"),
            "1hsword" => codeName.Contains("sword_1h") || codeName.Contains("tsh"),
            "2hsword" => codeName.Contains("sword_2h") || codeName.Contains("esh"),
            "crossbow" => codeName.Contains("crossbow"),
            "dagger" => codeName.Contains("dagger"),
            "harp" => codeName.Contains("harp"),
            "axe" => codeName.Contains("axe"),
            "shield" => item.TypeId3 == 4 || codeName.Contains("shield"),
            "quest" => item.IsQuest,
            "coins" => codeName.Contains("gold") || codeName.Contains("coin"),
            "ammo" => item.IsAmmunition,
            "alchemy" => item.TypeId3 is 10 or 11 || codeName.Contains("alchemy"),
            "other" => true,
            _ => false
        };
    }

    private string ResolveItemName(string codeName)
    {
        var fromCatalog = _itemCatalog.FirstOrDefault(item =>
            string.Equals(item.CodeName, codeName, StringComparison.OrdinalIgnoreCase));
        if (fromCatalog != null)
            return fromCatalog.Name;

        if (_shopCatalog.Count > 0)
        {
            foreach (var shop in _shopCatalog)
            {
                var shopItem = shop.Items.FirstOrDefault(item =>
                    string.Equals(item.CodeName, codeName, StringComparison.OrdinalIgnoreCase));
                if (shopItem != null)
                    return shopItem.Name;
            }
        }

        return codeName;
    }

    private static int ParseInt(string? text, int fallback)
    {
        return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    private static IEnumerable<string> EnumerateStringRows(object? raw)
    {
        if (raw == null)
            yield break;

        if (raw is string single)
        {
            if (!string.IsNullOrWhiteSpace(single))
                yield return single.Trim();
            yield break;
        }

        if (raw is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in jsonElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String)
                    continue;
                var value = item.GetString();
                if (!string.IsNullOrWhiteSpace(value))
                    yield return value.Trim();
            }

            yield break;
        }

        if (raw is not IEnumerable enumerable)
            yield break;

        foreach (var item in enumerable)
        {
            var value = item?.ToString();
            if (!string.IsNullOrWhiteSpace(value))
                yield return value.Trim();
        }
    }

    private static IEnumerable<Dictionary<string, object?>> EnumerateDictionaryRows(object? raw)
    {
        if (raw == null)
            yield break;

        if (raw is JsonElement jsonElement && jsonElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in jsonElement.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                    continue;
                yield return JsonElementToDictionary(item);
            }

            yield break;
        }

        if (raw is not IEnumerable enumerable || raw is string)
            yield break;

        foreach (var item in enumerable)
        {
            if (item is Dictionary<string, object?> directDictionary)
            {
                yield return directDictionary;
                continue;
            }

            if (item is IDictionary<string, object?> typedDictionary)
            {
                yield return new Dictionary<string, object?>(typedDictionary, StringComparer.OrdinalIgnoreCase);
                continue;
            }

            if (item is IDictionary untyped)
            {
                var rowDictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (DictionaryEntry entry in untyped)
                {
                    if (entry.Key == null)
                        continue;
                    var key = entry.Key.ToString();
                    if (string.IsNullOrWhiteSpace(key))
                        continue;
                    rowDictionary[key] = entry.Value;
                }

                if (rowDictionary.Count > 0)
                    yield return rowDictionary;
                continue;
            }

            if (item is JsonElement element && element.ValueKind == JsonValueKind.Object)
                yield return JsonElementToDictionary(element);
        }
    }

    private static Dictionary<string, object?> JsonElementToDictionary(JsonElement element)
    {
        var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in element.EnumerateObject())
            dictionary[property.Name] = JsonElementToObject(property.Value);
        return dictionary;
    }

    private static object? JsonElementToObject(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.Object => JsonElementToDictionary(element),
            JsonValueKind.Array => element.EnumerateArray().Select(JsonElementToObject).ToList(),
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number => element.TryGetInt64(out var longValue) ? longValue : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static string GetString(IDictionary<string, object?> row, string key, string fallback = "")
    {
        if (!row.TryGetValue(key, out var raw) || raw == null)
            return fallback;

        return raw.ToString() ?? fallback;
    }

    private static int GetInt(IDictionary<string, object?> row, string key, int fallback)
    {
        if (!row.TryGetValue(key, out var raw) || raw == null)
            return fallback;

        if (raw is int value)
            return value;
        if (raw is long longValue && longValue >= int.MinValue && longValue <= int.MaxValue)
            return (int)longValue;
        if (raw is double doubleValue)
            return (int)Math.Round(doubleValue);
        if (int.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed;
        return fallback;
    }

    private static bool GetBool(IDictionary<string, object?> row, string key, bool fallback)
    {
        if (!row.TryGetValue(key, out var raw) || raw == null)
            return fallback;

        if (raw is bool value)
            return value;
        if (bool.TryParse(raw.ToString(), out var parsed))
            return parsed;
        return fallback;
    }

    private async System.Threading.Tasks.Task QuickAddShoppingAsync(string itemCodeName)
    {
        if (string.IsNullOrWhiteSpace(itemCodeName))
            return;

        EnsureSelectedShop();
        if (string.IsNullOrWhiteSpace(_selectedShopCodeName))
            return;

        _selectedSourceItemCodeName = itemCodeName;
        ShoppingItemSelect.SelectedValue = itemCodeName;

        var amount = Math.Clamp(ParseInt(ShoppingAmountBox.Text, 1), 1, 50000);
        var existing = _shoppingTargets.FirstOrDefault(target =>
            string.Equals(target.ShopCodeName, _selectedShopCodeName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(target.ItemCodeName, itemCodeName, StringComparison.OrdinalIgnoreCase));

        if (existing == null)
        {
            _shoppingTargets.Add(new ShoppingTargetEntry
            {
                ShopCodeName = _selectedShopCodeName,
                ItemCodeName = itemCodeName,
                Amount = amount
            });
        }
        else
        {
            existing.Amount = amount;
        }

        await SaveShoppingTargetsAsync();
        RefreshShoppingSections();
    }
}
