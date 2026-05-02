using Avalonia.Controls;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.Protection;

public class ThresholdRow : ObservableObject
{
    public string FlagKey  { get; set; } = "";
    public string ValueKey { get; set; } = "";
    public string Label    { get; set; } = "";
    private bool   _enabled; public bool   Enabled { get => _enabled; set { SetProperty(ref _enabled, value); Changed?.Invoke(); } }
    private double _value;   public double Value   { get => _value;   set { SetProperty(ref _value, value);   Changed?.Invoke(); } }
    public System.Action? Changed;
}

public class CheckRow : ObservableObject
{
    public string Key   { get; set; } = "";
    public string Label { get; set; } = "";
    private bool _enabled; public bool Enabled { get => _enabled; set { SetProperty(ref _enabled, value); Changed?.Invoke(); } }
    public System.Action? Changed;
}

public partial class ProtectionFeatureView : UserControl
{
    private PluginViewModelBase? _vm;
    private bool _syncing;
    private bool _rowsBuilt;
    private ObservableCollection<ThresholdRow> _recovRows = new();
    private ObservableCollection<CheckRow>     _backRows  = new();

    public ProtectionFeatureView() { InitializeComponent(); }

    public void Initialize(PluginViewModelBase vm)
    {
        _vm = vm;
        UseUniversalPillsCheck.Content = "Use Universal Pills";
        UseBadStatusSkillCheck.Content = "Use Skill";
        PetHpCheck.Content     = "Use HP potions if HP";
        PetHungerCheck.Content = "Use HGP potions if hunger";
        PetAbnormCheck.Content = "Use abnormal state recovery potions";
        PetReviveCheck.Content = "Revive growth / fellow pet";
        PetSummonCheck.Content = "Auto summon growth & fellow pet";
        RefreshFromConfig();
    }

    // Protection config is independent of runtime state — never reload config on state polls.
    // State polls carry live game data (HP bars, bot status), not user config.
    public void UpdateFromState(System.Text.Json.JsonElement state) { }

    public async void RefreshFromConfig()
    {
        if (_vm is null) return;
        await _vm.LoadConfigAsync();

        _syncing = true;

        if (!_rowsBuilt)
        {
            // First load: build collections and assign ItemsSource once.
            // ItemsSource is never replaced after this point, so DataTemplate
            // TextBox instances persist and focus is never destroyed by a reload.
            _recovRows = new ObservableCollection<ThresholdRow>
            {
                MakeThresh("Use HP potions if HP",    "hpPotionEnabled",  "hpPotionThreshold",  75),
                MakeThresh("Use MP potions if MP",    "mpPotionEnabled",  "mpPotionThreshold",  75),
                MakeThresh("Use Vigor Potions if HP", "vigorHpEnabled",   "vigorHpThreshold",   50),
                MakeThresh("Use Vigor Potions if MP", "vigorMpEnabled",   "vigorMpThreshold",   50),
                MakeThresh("Use skill if HP",         "skillHpEnabled",   "skillHpThreshold",   50),
                MakeThresh("Use skill if MP",         "mpSkillEnabled",   "mpSkillThreshold",   50),
            };
            RecoveryList.ItemsSource = _recovRows;

            _backRows = new ObservableCollection<CheckRow>
            {
                MakeCheck("Dead with delay",            "deadDelayEnabled"),
                MakeCheck("Stop bot when back in town", "stopInTown"),
                MakeCheck("No arrows / bolts left",     "noArrows"),
                MakeCheck("Full inventory",             "fullInventory"),
                MakeCheck("Full pet inventory",         "fullPetInventory"),
                MakeCheck("HP Potions left",            "hpPotionsLow"),
                MakeCheck("MP Potions left",            "mpPotionsLow"),
                MakeCheck("Equipment durability low",   "lowDurability"),
                MakeCheck("Level up",                   "levelUp"),
                MakeCheck("Shard fatigue",              "shardFatigue"),
            };
            BackToTownList.ItemsSource = _backRows;
            _rowsBuilt = true;
        }
        else
        {
            // Subsequent calls: update existing row objects in-place.
            // ThresholdRow/CheckRow use SetProperty which skips PropertyChanged
            // when the value is unchanged, so a focused TextBox is not disturbed.
            SetThresh(_recovRows, 0, "hpPotionEnabled",  "hpPotionThreshold",  75);
            SetThresh(_recovRows, 1, "mpPotionEnabled",  "mpPotionThreshold",  75);
            SetThresh(_recovRows, 2, "vigorHpEnabled",   "vigorHpThreshold",   50);
            SetThresh(_recovRows, 3, "vigorMpEnabled",   "vigorMpThreshold",   50);
            SetThresh(_recovRows, 4, "skillHpEnabled",   "skillHpThreshold",   50);
            SetThresh(_recovRows, 5, "mpSkillEnabled",   "mpSkillThreshold",   50);

            SetCheck(_backRows, 0, "deadDelayEnabled");
            SetCheck(_backRows, 1, "stopInTown");
            SetCheck(_backRows, 2, "noArrows");
            SetCheck(_backRows, 3, "fullInventory");
            SetCheck(_backRows, 4, "fullPetInventory");
            SetCheck(_backRows, 5, "hpPotionsLow");
            SetCheck(_backRows, 6, "mpPotionsLow");
            SetCheck(_backRows, 7, "lowDurability");
            SetCheck(_backRows, 8, "levelUp");
            SetCheck(_backRows, 9, "shardFatigue");
        }

        UseUniversalPillsCheck.IsChecked = _vm.BoolCfg("useUniversalPills", true);
        UseBadStatusSkillCheck.IsChecked = _vm.BoolCfg("useBadStatusSkill");

        // Don't overwrite stat boxes while the user is actively editing them.
        if (!IncIntBox.IsFocused) IncIntBox.Text = _vm.NumCfg("increaseInt").ToString("F0");
        if (!IncStrBox.IsFocused) IncStrBox.Text = _vm.NumCfg("increaseStr").ToString("F0");

        PetHpCheck.IsChecked     = _vm.BoolCfg("petHpPotionEnabled",         true);
        PetHungerCheck.IsChecked = _vm.BoolCfg("petHgpPotionEnabled",        true);
        PetAbnormCheck.IsChecked = _vm.BoolCfg("petAbnormalRecoveryEnabled", true);
        PetReviveCheck.IsChecked = _vm.BoolCfg("reviveGrowthFellow",         true);
        PetSummonCheck.IsChecked = _vm.BoolCfg("autoSummonGrowthFellow",     true);

        _syncing = false;
    }

    private void SetThresh(ObservableCollection<ThresholdRow> rows, int i, string flagKey, string valKey, double def)
    {
        if (i >= rows.Count) return;
        rows[i].Enabled = _vm!.BoolCfg(flagKey);
        rows[i].Value   = _vm.NumCfg(valKey, def);
    }

    private void SetCheck(ObservableCollection<CheckRow> rows, int i, string key)
    {
        if (i >= rows.Count) return;
        rows[i].Enabled = _vm!.BoolCfg(key);
    }

    private ThresholdRow MakeThresh(string label, string flagKey, string valKey, double def)
    {
        var r = new ThresholdRow { Label = label, FlagKey = flagKey, ValueKey = valKey, Enabled = _vm!.BoolCfg(flagKey), Value = _vm.NumCfg(valKey, def) };
        r.Changed = () => { if (!_syncing) _ = _vm!.PatchConfigAsync(new Dictionary<string, object?> { [r.FlagKey] = r.Enabled, [r.ValueKey] = r.Value }); };
        return r;
    }

    private CheckRow MakeCheck(string label, string key)
    {
        var r = new CheckRow { Label = label, Key = key, Enabled = _vm!.BoolCfg(key) };
        r.Changed = () => { if (!_syncing) _ = _vm!.PatchConfigAsync(new Dictionary<string, object?> { [r.Key] = r.Enabled }); };
        return r;
    }

    private void Check_Changed(object? s, RoutedEventArgs e)
    {
        if (_syncing || _vm is null || s is not CheckBox cb || cb.Tag is not string key) return;
        _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { [key] = cb.IsChecked == true });
    }

    private void NumBox_Changed(object? s, TextChangedEventArgs e)
    {
        if (_syncing || _vm is null || s is not TextBox tb || tb.Tag is not string key) return;
        if (double.TryParse(tb.Text, out var v))
            _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { [key] = System.Math.Clamp(v, 0, 3) });
    }

    private void ApplyStat_Click(object? s, RoutedEventArgs e)
        => _vm?.PluginActionAsync("protection.apply-stat-points");
}
