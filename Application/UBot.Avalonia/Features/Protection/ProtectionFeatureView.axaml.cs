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

    public ProtectionFeatureView() { InitializeComponent(); }

    public void Initialize(PluginViewModelBase vm)
    {
        _vm = vm;
        UseUniversalPillsCheck.Content = "Use universal pills";
        UseBadStatusSkillCheck.Content = "Use bad status skill";
        PetHpCheck.Content     = "Pet HP potions";
        PetHungerCheck.Content = "Pet hunger potions";
        PetAbnormCheck.Content = "Pet abnormal recovery";
        PetReviveCheck.Content = "Revive fellow pet";
        PetSummonCheck.Content = "Auto-summon fellow pet";
        RefreshFromConfig();
    }

    public void RefreshFromConfig()
    {
        if (_vm is null) return;
        _syncing = true;

        // Recovery rows
        var recovRows = new ObservableCollection<ThresholdRow>
        {
            MakeThresh("HP Potion",       "hpPotionEnabled",  "hpPotionThreshold",  75),
            MakeThresh("MP Potion",       "mpPotionEnabled",  "mpPotionThreshold",  75),
            MakeThresh("Vigor HP",        "vigorHpEnabled",   "vigorHpThreshold",   50),
            MakeThresh("Vigor MP",        "vigorMpEnabled",   "vigorMpThreshold",   50),
            MakeThresh("Skill HP",        "skillHpEnabled",   "skillHpThreshold",   50),
            MakeThresh("Skill MP",        "mpSkillEnabled",   "mpSkillThreshold",   50),
        };
        RecoveryList.ItemsSource = recovRows;

        // Back to town
        var backRows = new ObservableCollection<CheckRow>
        {
            MakeCheck("Dead delay enabled",   "deadDelayEnabled"),
            MakeCheck("Stop in town",         "stopInTown"),
            MakeCheck("No arrows",            "noArrows"),
            MakeCheck("Full inventory",       "fullInventory"),
            MakeCheck("Full pet inventory",   "fullPetInventory"),
            MakeCheck("Low HP potions",       "hpPotionsLow"),
            MakeCheck("Low MP potions",       "mpPotionsLow"),
            MakeCheck("Low durability",       "lowDurability"),
            MakeCheck("Level up",             "levelUp"),
            MakeCheck("Shard fatigue",        "shardFatigue"),
        };
        BackToTownList.ItemsSource = backRows;

        UseUniversalPillsCheck.IsChecked = _vm.BoolCfg("useUniversalPills", true);
        UseBadStatusSkillCheck.IsChecked = _vm.BoolCfg("useBadStatusSkill");
        IncIntBox.Text = _vm.NumCfg("increaseInt").ToString("F0");
        IncStrBox.Text = _vm.NumCfg("increaseStr").ToString("F0");
        PetHpCheck.IsChecked     = _vm.BoolCfg("petHpPotionEnabled",         true);
        PetHungerCheck.IsChecked = _vm.BoolCfg("petHgpPotionEnabled",        true);
        PetAbnormCheck.IsChecked = _vm.BoolCfg("petAbnormalRecoveryEnabled", true);
        PetReviveCheck.IsChecked = _vm.BoolCfg("reviveGrowthFellow",         true);
        PetSummonCheck.IsChecked = _vm.BoolCfg("autoSummonGrowthFellow",     true);

        _syncing = false;
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
