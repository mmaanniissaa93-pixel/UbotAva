using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using UBot.Avalonia.Controls;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;
using Avalonia.Media.Imaging;

namespace UBot.Avalonia.Features.Skills;

public sealed class SkillListRow
{
    public uint Id { get; set; }
    public string Display { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public Bitmap? IconBitmap { get; set; }
}

public partial class SkillsFeatureView : UserControl
{
    private static readonly (int Id, string Label)[] AttackTypeOptions =
    {
        (0, "General"),
        (1, "Champion"),
        (2, "Giant"),
        (3, "General Party"),
        (4, "Champion Party"),
        (5, "Giant Party"),
        (6, "Elite"),
        (7, "Elite Strong"),
        (8, "Unique")
    };

    private SkillsViewModel? _vm;
    private bool _syncing;
    private string _leftTab = "playerSkills";
    private string _mainTab = "generalSetup";

    private readonly ObservableCollection<SkillListRow> _availableRows = new();
    private readonly ObservableCollection<SkillListRow> _attackRows = new();
    private readonly ObservableCollection<SkillListRow> _buffRows = new();
    private readonly ObservableCollection<SkillListRow> _activeBuffRows = new();

    private readonly Dictionary<int, List<uint>> _attackSkillIds = new();
    private List<uint> _buffSkillIds = new();
    private List<SkillCatalogEntry> _catalog = new();
    private List<MasteryCatalogEntry> _masteries = new();
    private List<ActiveBuffEntry> _activeBuffs = new();

    private int _attackTypeIndex;
    private uint _imbueSkillId;
    private uint _resurrectionSkillId;
    private uint _teleportSkillId;
    private uint _selectedMasteryId;

    public SkillsFeatureView()
    {
        InitializeComponent();

        PlayerSkillsList.ItemsSource = _availableRows;
        AttackSkillsList.ItemsSource = _attackRows;
        BuffSkillsList.ItemsSource = _buffRows;
        ActiveBuffsList.ItemsSource = _activeBuffRows;

        for (var i = 0; i < AttackTypeOptions.Length; i++)
            _attackSkillIds[i] = new List<uint>();

        AttackTypeSelect.SelectionChanged += AttackTypeSelect_Changed;
        ImbueSkillSelect.SelectionChanged += value => SkillSelectChanged("imbueSkillId", value);
        ResurrectionSkillSelect.SelectionChanged += value => SkillSelectChanged("resurrectionSkillId", value);
        TeleportSkillSelect.SelectionChanged += value => SkillSelectChanged("teleportSkillId", value);
        MasterySelect.SelectionChanged += value => SkillSelectChanged("selectedMasteryId", value);
    }

    public void Initialize(SkillsViewModel vm, AppState state)
    {
        _vm = vm;

        LeftTabs.ActiveTabId = _leftTab;
        LeftTabs.SetTabs(new[]
        {
            ("playerSkills", "Player Skills"),
            ("activeBuffs", "Active Buffs")
        });
        LeftTabs.TabChanged += tab =>
        {
            _leftTab = tab;
            SyncTabVisibility();
            if (_leftTab == "activeBuffs")
                RefreshActiveBuffRows();
        };

        MainTabs.ActiveTabId = _mainTab;
        MainTabs.SetTabs(new[]
        {
            ("generalSetup", "General Setup"),
            ("advancedSetup", "Advanced Setup")
        });
        MainTabs.TabChanged += tab =>
        {
            _mainTab = tab;
            SyncTabVisibility();
        };

        SyncTabVisibility();
        _ = LoadFromConfigAsync();
    }

    public void UpdateFromState(JsonElement moduleState)
    {
        var stateRoot = moduleState;
        if (moduleState.ValueKind == JsonValueKind.Object && moduleState.TryGetProperty("skills", out var nestedSkills))
            stateRoot = nestedSkills;

        var refreshCatalog = false;
        if (stateRoot.ValueKind == JsonValueKind.Object && stateRoot.TryGetProperty("skillCatalog", out var catalogElement))
        {
            _catalog = ParseSkillCatalog(catalogElement);
            refreshCatalog = true;
        }

        if (stateRoot.ValueKind == JsonValueKind.Object && stateRoot.TryGetProperty("masteryCatalog", out var masteryElement))
        {
            _masteries = ParseMasteryCatalog(masteryElement);
            refreshCatalog = true;
        }

        if (stateRoot.ValueKind == JsonValueKind.Object && stateRoot.TryGetProperty("activeBuffs", out var activeBuffsElement))
        {
            _activeBuffs = ParseActiveBuffCatalog(activeBuffsElement);
            RefreshActiveBuffRows();
        }

        if (refreshCatalog)
        {
            var previousSync = _syncing;
            _syncing = true;
            PopulateSelectOptions();
            RefreshAvailableRows();
            RefreshAttackRows();
            RefreshBuffRows();
            _syncing = previousSync;
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
            EnableAttacksCheck.IsChecked = _vm.BoolCfg("enableAttacks", true);
            EnableBuffsCheck.IsChecked = _vm.BoolCfg("enableBuffs", true);
            UseInOrderCheck.IsChecked = _vm.BoolCfg("useSkillsInOrder", false);
            NoAttackCheck.IsChecked = _vm.BoolCfg("noAttack", false);

            AcceptResurrectionCheck.IsChecked = _vm.BoolCfg("acceptResurrection", false);
            ResurrectPartyCheck.IsChecked = _vm.BoolCfg("resurrectParty", false);
            CastBuffsInTownsCheck.IsChecked = _vm.BoolCfg("castBuffsInTowns", false);
            CastBuffsWalkBackCheck.IsChecked = _vm.BoolCfg("castBuffsDuringWalkBack", true);
            CastBuffsBetweenAttacksCheck.IsChecked = _vm.BoolCfg("castBuffsBetweenAttacks", false);
            LearnMasteryCheck.IsChecked = _vm.BoolCfg("learnMastery", false);
            LearnMasteryStoppedCheck.IsChecked = _vm.BoolCfg("learnMasteryBotStopped", false);
            WarlockModeCheck.IsChecked = _vm.BoolCfg("warlockMode", false);
            UseDefaultAttackCheck.IsChecked = _vm.BoolCfg("useDefaultAttack", true);
            UseTeleportSkillCheck.IsChecked = _vm.BoolCfg("useTeleportSkill", false);

            var resDelay = ClampNumeric("resDelay", (int)Math.Round(_vm.NumCfg("resDelay", 120)));
            var resRadius = ClampNumeric("resRadius", (int)Math.Round(_vm.NumCfg("resRadius", 100)));
            var masteryGap = ClampNumeric("masteryGap", (int)Math.Round(_vm.NumCfg("masteryGap", 0)));
            ResDelayBox.Text = resDelay.ToString(CultureInfo.InvariantCulture);
            ResRadiusBox.Text = resRadius.ToString(CultureInfo.InvariantCulture);
            MasteryGapBox.Text = masteryGap.ToString(CultureInfo.InvariantCulture);

            _attackTypeIndex = Math.Clamp((int)Math.Round(_vm.NumCfg("attackTypeIndex", 0)), 0, AttackTypeOptions.Length - 1);
            _imbueSkillId = ToUInt(_vm.NumCfg("imbueSkillId", 0));
            _resurrectionSkillId = ToUInt(_vm.NumCfg("resurrectionSkillId", 0));
            _teleportSkillId = ToUInt(_vm.NumCfg("teleportSkillId", 0));
            _selectedMasteryId = ToUInt(_vm.NumCfg("selectedMasteryId", 0));

            _catalog = _vm.GetSkillCatalog();
            _masteries = _vm.GetMasteryCatalog();
            _activeBuffs = _vm.GetActiveBuffs();
            _buffSkillIds = _vm.GetBuffSkills();

            foreach (var option in AttackTypeOptions)
                _attackSkillIds[option.Id] = _vm.GetAttackSkills(option.Id);

            PopulateSelectOptions();
            RefreshAvailableRows();
            RefreshAttackRows();
            RefreshBuffRows();
            RefreshActiveBuffRows();
        }
        finally
        {
            _syncing = false;
        }
    }

    private void PopulateSelectOptions()
    {
        var attackTypeItems = new List<SelectOption>();
        foreach (var option in AttackTypeOptions)
            attackTypeItems.Add(new SelectOption(option.Id, option.Label));

        AttackTypeSelect.Options = attackTypeItems;
        AttackTypeSelect.SelectedValue = _attackTypeIndex;

        var imbueSkills = _catalog.Where(skill => skill.IsImbue).ToList();
        ImbueSkillSelect.Options = BuildSkillOptions(imbueSkills, _imbueSkillId);
        ImbueSkillSelect.SelectedValue = _imbueSkillId;

        var selectableSkills = _catalog.Where(skill => !skill.IsPassive).ToList();
        ResurrectionSkillSelect.Options = BuildSkillOptions(selectableSkills, _resurrectionSkillId);
        ResurrectionSkillSelect.SelectedValue = _resurrectionSkillId;
        TeleportSkillSelect.Options = BuildSkillOptions(selectableSkills, _teleportSkillId);
        TeleportSkillSelect.SelectedValue = _teleportSkillId;

        var masteryOptions = new List<SelectOption> { new(0u, "Not selected") };
        foreach (var mastery in _masteries)
            masteryOptions.Add(new SelectOption(mastery.Id, $"{mastery.Name} (Lv {mastery.Level})"));

        if (_selectedMasteryId != 0 && _masteries.All(mastery => mastery.Id != _selectedMasteryId))
            masteryOptions.Add(new SelectOption(_selectedMasteryId, $"Unknown ({_selectedMasteryId})"));

        MasterySelect.Options = masteryOptions;
        MasterySelect.SelectedValue = _selectedMasteryId;
    }

    private List<SelectOption> BuildSkillOptions(IEnumerable<SkillCatalogEntry> catalog, uint selectedId)
    {
        var options = new List<SelectOption> { new(0u, "None") };
        foreach (var skill in catalog)
            options.Add(new SelectOption(skill.Id, skill.Name));

        if (selectedId != 0 && _catalog.All(skill => skill.Id != selectedId))
            options.Add(new SelectOption(selectedId, $"Unknown ({selectedId})"));

        return options;
    }

    private void SyncTabVisibility()
    {
        PlayerSkillsPanel.IsVisible = _leftTab == "playerSkills";
        ActiveBuffsPanel.IsVisible = _leftTab == "activeBuffs";
        GeneralSetupPanel.IsVisible = _mainTab == "generalSetup";
        AdvancedSetupPanel.IsVisible = _mainTab == "advancedSetup";
        UpdateEmptyHints();
    }

    private void RefreshAvailableRows()
    {
        var selectedId = (PlayerSkillsList.SelectedItem as SkillListRow)?.Id ?? 0;
        var showAttacks = EnableAttacksCheck.IsChecked == true;
        var showBuffs = EnableBuffsCheck.IsChecked == true;

        var filtered = _catalog.Where(skill =>
            (showAttacks && skill.IsAttack && !skill.IsPassive && !skill.IsImbue)
            || (showBuffs && skill.IsBuff && !skill.IsImbue))
            .OrderBy(skill => skill.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (IsCollectionIdentical(_availableRows, filtered))
        {
            UpdateEmptyHints();
            return;
        }

        _availableRows.Clear();
        SkillListRow? nextSelection = null;
        foreach (var skill in filtered.OrderBy(skill => skill.Name, StringComparer.OrdinalIgnoreCase))
        {
            var row = new SkillListRow { Id = skill.Id, Display = skill.Name, Icon = skill.Icon };
            _availableRows.Add(row);
            if (row.Id == selectedId) nextSelection = row;
            _ = LoadIconAsync(row);
        }

        if (nextSelection != null)
            PlayerSkillsList.SelectedItem = nextSelection;

        UpdateEmptyHints();
    }

    private void RefreshAttackRows()
    {
        var selectedId = (AttackSkillsList.SelectedItem as SkillListRow)?.Id ?? 0;
        var skillIds = GetCurrentAttackSkills();

        if (IsCollectionIdentical(_attackRows, skillIds))
            return;

        _attackRows.Clear();
        SkillListRow? nextSelection = null;
        foreach (var skillId in skillIds)
        {
            var row = new SkillListRow { Id = skillId, Display = ResolveSkillName(skillId), Icon = ResolveSkillIcon(skillId) };
            _attackRows.Add(row);
            if (row.Id == selectedId) nextSelection = row;
            _ = LoadIconAsync(row);
        }

        if (nextSelection != null)
            AttackSkillsList.SelectedItem = nextSelection;
    }

    private void RefreshBuffRows()
    {
        var selectedId = (BuffSkillsList.SelectedItem as SkillListRow)?.Id ?? 0;
        var skillIds = _buffSkillIds;

        if (IsCollectionIdentical(_buffRows, skillIds))
            return;

        _buffRows.Clear();
        SkillListRow? nextSelection = null;
        foreach (var skillId in skillIds)
        {
            var row = new SkillListRow { Id = skillId, Display = ResolveSkillName(skillId), Icon = ResolveSkillIcon(skillId) };
            _buffRows.Add(row);
            if (row.Id == selectedId) nextSelection = row;
            _ = LoadIconAsync(row);
        }

        if (nextSelection != null)
            BuffSkillsList.SelectedItem = nextSelection;
    }

    private static bool IsCollectionIdentical(ObservableCollection<SkillListRow> current, List<SkillCatalogEntry> next)
    {
        if (current.Count != next.Count) return false;
        for (int i = 0; i < current.Count; i++)
            if (current[i].Id != next[i].Id) return false;
        return true;
    }

    private static bool IsCollectionIdentical(ObservableCollection<SkillListRow> current, List<uint> next)
    {
        if (current.Count != next.Count) return false;
        for (int i = 0; i < current.Count; i++)
            if (current[i].Id != next[i]) return false;
        return true;
    }

    private void RefreshActiveBuffRows()
    {
        _activeBuffRows.Clear();
        foreach (var buff in _activeBuffs)
        {
            var percentText = buff.RemainingPercent > 0
                ? $" ({Math.Round(buff.RemainingPercent):F0}%)"
                : string.Empty;
            _activeBuffRows.Add(new SkillListRow
            {
                Id = buff.Id,
                Display = $"{buff.Name}{percentText}"
            });
        }

        UpdateEmptyHints();
    }

    private void UpdateEmptyHints()
    {
        if (PlayerSkillsEmptyHint != null)
            PlayerSkillsEmptyHint.IsVisible = _availableRows.Count == 0 && _leftTab == "playerSkills";

        if (ActiveBuffsEmptyHint != null)
            ActiveBuffsEmptyHint.IsVisible = _activeBuffRows.Count == 0 && _leftTab == "activeBuffs";
    }

    private List<uint> GetCurrentAttackSkills()
    {
        if (!_attackSkillIds.TryGetValue(_attackTypeIndex, out var attackSkills))
        {
            attackSkills = new List<uint>();
            _attackSkillIds[_attackTypeIndex] = attackSkills;
        }

        return attackSkills;
    }

    private string ResolveSkillName(uint skillId)
    {
        var skill = _catalog.FirstOrDefault(entry => entry.Id == skillId);
        return skill?.Name ?? $"Unknown ({skillId})";
    }

    private string ResolveSkillIcon(uint skillId)
    {
        var skill = _catalog.FirstOrDefault(entry => entry.Id == skillId);
        return skill?.Icon ?? string.Empty;
    }

    private readonly Dictionary<string, Bitmap> _iconCache = new();
    
    private async System.Threading.Tasks.Task LoadIconAsync(SkillListRow row)
    {
        if (string.IsNullOrWhiteSpace(row.Icon) || _vm == null) return;

        if (_iconCache.TryGetValue(row.Icon, out var cached))
        {
            row.IconBitmap = cached;
            return;
        }

        var bytes = await _vm.GetSkillIconAsync(row.Icon);
        if (bytes != null)
        {
            using var ms = new System.IO.MemoryStream(bytes);
            var bitmap = new Bitmap(ms);
            _iconCache[row.Icon] = bitmap;
            row.IconBitmap = bitmap;
        }
    }

    private void AttackTypeSelect_Changed(object value)
    {
        if (_syncing || _vm == null)
            return;

        if (!TryConvertInt(value, out var parsed))
            return;

        _attackTypeIndex = Math.Clamp(parsed, 0, AttackTypeOptions.Length - 1);
        RefreshAttackRows();
        _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { ["attackTypeIndex"] = _attackTypeIndex });
    }

    private void SkillSelectChanged(string key, object value)
    {
        if (_syncing || _vm == null)
            return;

        if (!TryConvertUInt(value, out var selectedSkillId))
            selectedSkillId = 0;

        switch (key)
        {
            case "imbueSkillId":
                _imbueSkillId = selectedSkillId;
                break;
            case "resurrectionSkillId":
                _resurrectionSkillId = selectedSkillId;
                break;
            case "teleportSkillId":
                _teleportSkillId = selectedSkillId;
                break;
            case "selectedMasteryId":
                _selectedMasteryId = selectedSkillId;
                break;
        }

        _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { [key] = selectedSkillId });
    }

    private void SimpleCheck_Changed(object? sender, RoutedEventArgs e)
    {
        if (_syncing || _vm == null || sender is not CheckBox checkBox || checkBox.Tag is not string key)
            return;

        var isChecked = checkBox.IsChecked == true;
        _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { [key] = isChecked });

        if (key is "enableAttacks" or "enableBuffs")
            RefreshAvailableRows();
    }

    private void NumericBox_Changed(object? sender, TextChangedEventArgs e)
    {
        if (_syncing || _vm == null || sender is not TextBox textBox || textBox.Tag is not string key)
            return;

        if (!int.TryParse(textBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return;

        var clamped = ClampNumeric(key, parsed);
        if (clamped != parsed)
        {
            _syncing = true;
            textBox.Text = clamped.ToString(CultureInfo.InvariantCulture);
            _syncing = false;
        }

        _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { [key] = clamped });
    }

    private static int ClampNumeric(string key, int value)
    {
        return key switch
        {
            "resDelay" => Math.Clamp(value, 1, 3600),
            "resRadius" => Math.Clamp(value, 1, 500),
            "masteryGap" => Math.Clamp(value, 0, 120),
            _ => value
        };
    }

    private void AddAttackSkill_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null || PlayerSkillsList.SelectedItem is not SkillListRow selectedSkill)
            return;

        var currentAttackSkills = GetCurrentAttackSkills();
        if (currentAttackSkills.Contains(selectedSkill.Id))
            return;

        currentAttackSkills.Add(selectedSkill.Id);
        RefreshAttackRows();
        _ = _vm.PatchAttackSkillsAsync(_attackTypeIndex, currentAttackSkills);
    }

    private void RemoveAttackSkill_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null || AttackSkillsList.SelectedItem is not SkillListRow selectedSkill)
            return;

        var currentAttackSkills = GetCurrentAttackSkills();
        if (!currentAttackSkills.Remove(selectedSkill.Id))
            return;

        RefreshAttackRows();
        _ = _vm.PatchAttackSkillsAsync(_attackTypeIndex, currentAttackSkills);
    }

    private void MoveAttackUp_Click(object? sender, RoutedEventArgs e)
    {
        MoveSelectedSkill(AttackSkillsList, GetCurrentAttackSkills(), -1, () =>
        {
            if (_vm != null)
                _ = _vm.PatchAttackSkillsAsync(_attackTypeIndex, GetCurrentAttackSkills());
            RefreshAttackRows();
        });
    }

    private void MoveAttackDown_Click(object? sender, RoutedEventArgs e)
    {
        MoveSelectedSkill(AttackSkillsList, GetCurrentAttackSkills(), 1, () =>
        {
            if (_vm != null)
                _ = _vm.PatchAttackSkillsAsync(_attackTypeIndex, GetCurrentAttackSkills());
            RefreshAttackRows();
        });
    }

    private void AddBuffSkill_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null || PlayerSkillsList.SelectedItem is not SkillListRow selectedSkill)
            return;

        if (_buffSkillIds.Contains(selectedSkill.Id))
            return;

        _buffSkillIds.Add(selectedSkill.Id);
        RefreshBuffRows();
        _ = _vm.PatchBuffSkillsAsync(_buffSkillIds);
    }

    private void RemoveBuffSkill_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null || BuffSkillsList.SelectedItem is not SkillListRow selectedSkill)
            return;

        if (!_buffSkillIds.Remove(selectedSkill.Id))
            return;

        RefreshBuffRows();
        _ = _vm.PatchBuffSkillsAsync(_buffSkillIds);
    }

    private void MoveBuffUp_Click(object? sender, RoutedEventArgs e)
    {
        MoveSelectedSkill(BuffSkillsList, _buffSkillIds, -1, () =>
        {
            if (_vm != null)
                _ = _vm.PatchBuffSkillsAsync(_buffSkillIds);
            RefreshBuffRows();
        });
    }

    private void MoveBuffDown_Click(object? sender, RoutedEventArgs e)
    {
        MoveSelectedSkill(BuffSkillsList, _buffSkillIds, 1, () =>
        {
            if (_vm != null)
                _ = _vm.PatchBuffSkillsAsync(_buffSkillIds);
            RefreshBuffRows();
        });
    }

    private static void MoveSelectedSkill(ListBox listBox, List<uint> skillIds, int delta, Action onMoved)
    {
        if (listBox.SelectedItem is not SkillListRow selectedSkill)
            return;

        var currentIndex = skillIds.FindIndex(id => id == selectedSkill.Id);
        if (currentIndex < 0)
            return;

        var newIndex = currentIndex + delta;
        if (newIndex < 0 || newIndex >= skillIds.Count)
            return;

        (skillIds[currentIndex], skillIds[newIndex]) = (skillIds[newIndex], skillIds[currentIndex]);
        onMoved();
    }

    private static bool TryConvertInt(object? raw, out int value)
    {
        value = 0;
        if (raw == null)
            return false;

        if (raw is int intValue)
        {
            value = intValue;
            return true;
        }

        if (raw is long longValue && longValue >= int.MinValue && longValue <= int.MaxValue)
        {
            value = (int)longValue;
            return true;
        }

        if (raw is double doubleValue)
        {
            value = (int)doubleValue;
            return true;
        }

        return int.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryConvertUInt(object? raw, out uint value)
    {
        value = 0;
        if (raw == null)
            return false;

        if (raw is uint uintValue)
        {
            value = uintValue;
            return true;
        }

        if (raw is int intValue && intValue >= 0)
        {
            value = (uint)intValue;
            return true;
        }

        if (raw is long longValue && longValue >= 0 && longValue <= uint.MaxValue)
        {
            value = (uint)longValue;
            return true;
        }

        if (raw is double doubleValue && doubleValue >= 0 && doubleValue <= uint.MaxValue)
        {
            value = (uint)doubleValue;
            return true;
        }

        return uint.TryParse(raw.ToString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    private static uint ToUInt(double value)
    {
        if (value < 0)
            return 0;

        if (value > uint.MaxValue)
            return uint.MaxValue;

        return (uint)Math.Round(value);
    }

    private static List<SkillCatalogEntry> ParseSkillCatalog(JsonElement element)
    {
        var result = new List<SkillCatalogEntry>();
        if (element.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !TryReadUInt(item, "id", out var id))
                continue;

            result.Add(new SkillCatalogEntry
            {
                Id = id,
                Name = TryReadString(item, "name", out var name) ? name : $"Skill {id}",
                IsPassive = TryReadBool(item, "isPassive", out var isPassive) && isPassive,
                IsAttack = TryReadBool(item, "isAttack", out var isAttack) && isAttack,
                IsBuff = TryReadBool(item, "isBuff", out var isBuff) && isBuff,
                IsImbue = TryReadBool(item, "isImbue", out var isImbue) && isImbue,
                IsLowLevel = TryReadBool(item, "isLowLevel", out var isLowLevel) && isLowLevel,
                Icon = TryReadString(item, "icon", out var icon) ? icon : string.Empty
            });
        }

        return result
            .OrderBy(skill => skill.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(skill => skill.Id)
            .ToList();
    }

    private static List<MasteryCatalogEntry> ParseMasteryCatalog(JsonElement element)
    {
        var result = new List<MasteryCatalogEntry>();
        if (element.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !TryReadUInt(item, "id", out var id))
                continue;

            TryReadInt(item, "level", out var level);
            result.Add(new MasteryCatalogEntry
            {
                Id = id,
                Name = TryReadString(item, "name", out var name) ? name : $"Mastery {id}",
                Level = level
            });
        }

        return result
            .OrderBy(mastery => mastery.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<ActiveBuffEntry> ParseActiveBuffCatalog(JsonElement element)
    {
        var result = new List<ActiveBuffEntry>();
        if (element.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object || !TryReadUInt(item, "id", out var id))
                continue;

            TryReadUInt(item, "token", out var token);
            TryReadInt(item, "remainingMs", out var remainingMs);
            TryReadDouble(item, "remainingPercent", out var remainingPercent);

            result.Add(new ActiveBuffEntry
            {
                Id = id,
                Token = token,
                Name = TryReadString(item, "name", out var name) ? name : $"Buff {id}",
                RemainingMs = remainingMs,
                RemainingPercent = remainingPercent
            });
        }

        return result
            .OrderBy(buff => buff.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool TryReadString(JsonElement item, string key, out string value)
    {
        value = string.Empty;
        if (!item.TryGetProperty(key, out var property) || property.ValueKind != JsonValueKind.String)
            return false;

        value = property.GetString() ?? string.Empty;
        return true;
    }

    private static bool TryReadBool(JsonElement item, string key, out bool value)
    {
        value = false;
        if (!item.TryGetProperty(key, out var property))
            return false;

        if (property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False)
        {
            value = property.GetBoolean();
            return true;
        }

        return false;
    }

    private static bool TryReadUInt(JsonElement item, string key, out uint value)
    {
        value = 0;
        if (!item.TryGetProperty(key, out var property))
            return false;

        if (property.ValueKind == JsonValueKind.Number && property.TryGetUInt32(out var uintValue))
        {
            value = uintValue;
            return true;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt64(out var longValue) && longValue >= 0 && longValue <= uint.MaxValue)
        {
            value = (uint)longValue;
            return true;
        }

        if (property.ValueKind == JsonValueKind.String
            && uint.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static bool TryReadInt(JsonElement item, string key, out int value)
    {
        value = 0;
        if (!item.TryGetProperty(key, out var property))
            return false;

        if (property.ValueKind == JsonValueKind.Number && property.TryGetInt32(out var intValue))
        {
            value = intValue;
            return true;
        }

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var doubleValue))
        {
            value = (int)Math.Round(doubleValue);
            return true;
        }

        if (property.ValueKind == JsonValueKind.String
            && int.TryParse(property.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }

    private static bool TryReadDouble(JsonElement item, string key, out double value)
    {
        value = 0;
        if (!item.TryGetProperty(key, out var property))
            return false;

        if (property.ValueKind == JsonValueKind.Number && property.TryGetDouble(out var parsed))
        {
            value = parsed;
            return true;
        }

        if (property.ValueKind == JsonValueKind.String
            && double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedText))
        {
            value = parsedText;
            return true;
        }

        return false;
    }
}
