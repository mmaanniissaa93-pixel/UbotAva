using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.Training;

public class RarityRow : INotifyPropertyChanged
{
    public string Rarity  { get; set; } = "";
    private bool _avoid, _prefer, _berserk;
    public bool Avoid   { get => _avoid;   set { _avoid   = value; Changed?.Invoke(this, null!); PropertyChanged?.Invoke(this, new(nameof(Avoid))); } }
    public bool Prefer  { get => _prefer;  set { _prefer  = value; Changed?.Invoke(this, null!); PropertyChanged?.Invoke(this, new(nameof(Prefer))); } }
    public bool Berserk { get => _berserk; set { _berserk = value; Changed?.Invoke(this, null!); PropertyChanged?.Invoke(this, new(nameof(Berserk))); } }
    public event System.EventHandler? Changed;
    public event PropertyChangedEventHandler? PropertyChanged;
}

public partial class TrainingFeatureView : UserControl
{
    private PluginViewModelBase? _vm;
    private readonly ObservableCollection<RarityRow> _rarityRows = new();

    private static readonly string[] Rarities = {
        "General","Champion","Giant","GeneralParty","ChampionParty","GiantParty","Unique","Elite","Event"
    };

    public TrainingFeatureView()
    {
        InitializeComponent();
        AvoidanceGrid.ItemsSource = _rarityRows;
    }

    public void Initialize(PluginViewModelBase vm)
    {
        _vm = vm;
        // Labels
        AreaTitle.Text      = "Training Area";
        BackTitle.Text      = "Back to Training";
        BerserkTitle.Text   = "Berserk";
        AdvancedTitle.Text  = "Advanced";
        AvoidTitle.Text     = "Avoidance";
        SetCurrentBtn.Content = "Set Current";
        MonstersLabel.Text  = "monsters";
        UseMountCheck.Content    = "Use mount";
        CastBuffsCheck.Content   = "Cast buffs";
        UseSpeedCheck.Content    = "Use speed drug";
        UseReverseCheck.Content  = "Use reverse scroll";
        BerserkFullCheck.Content    = "Berserk when full";
        BerserkCountCheck.Content   = "Berserk by monster amount";
        BerserkAvoidCheck.Content   = "Berserk by avoidance";
        BerserkRarityCheck.Content  = "Berserk by monster rarity";
        IgnorePillarCheck.Content   = "Ignore dimension pillar";
        WeakerFirstCheck.Content    = "Attack weaker first";
        NoFollowCheck.Content       = "Don't follow mobs";

        // Build rarity rows
        _rarityRows.Clear();
        var avoidList   = vm.ListCfg("avoidanceList");
        var preferList  = vm.ListCfg("preferList");
        var berserkList = vm.ListCfg("berserkList");
        foreach (var r in Rarities)
        {
            var row = new RarityRow
            {
                Rarity  = r,
                Avoid   = avoidList.Contains(r),
                Prefer  = preferList.Contains(r),
                Berserk = berserkList.Contains(r)
            };
            row.Changed += (s, _) => OnRarityChanged();
            _rarityRows.Add(row);
        }

        RefreshFromConfig();
    }

    private bool _syncing;

    public void RefreshFromConfig()
    {
        if (_vm is null) return;
        _syncing = true;

        RegionBox.Text = _vm.NumCfg("areaRegion").ToString("F0");
        RadiusBox.Text = _vm.NumCfg("areaRadius", 50).ToString("F0");
        AreaXBox.Text  = _vm.NumCfg("areaX").ToString("F0");
        AreaYBox.Text  = _vm.NumCfg("areaY").ToString("F0");
        AreaZBox.Text  = _vm.NumCfg("areaZ").ToString("F0");
        WalkScriptBox.Text       = _vm.TextCfg("walkScript");
        UseMountCheck.IsChecked  = _vm.BoolCfg("useMount", true);
        CastBuffsCheck.IsChecked = _vm.BoolCfg("castBuffs", true);
        UseSpeedCheck.IsChecked  = _vm.BoolCfg("useSpeedDrug", true);
        UseReverseCheck.IsChecked= _vm.BoolCfg("useReverse");
        BerserkFullCheck.IsChecked    = _vm.BoolCfg("berserkWhenFull");
        BerserkCountCheck.IsChecked   = _vm.BoolCfg("berserkByMonsterAmount");
        BerserkCountBox.Text          = _vm.NumCfg("berserkMonsterAmount", 5).ToString("F0");
        BerserkAvoidCheck.IsChecked   = _vm.BoolCfg("berserkByAvoidance");
        BerserkRarityCheck.IsChecked  = _vm.BoolCfg("berserkByMonsterRarity");
        IgnorePillarCheck.IsChecked   = _vm.BoolCfg("ignoreDimensionPillar");
        WeakerFirstCheck.IsChecked    = _vm.BoolCfg("attackWeakerFirst");
        NoFollowCheck.IsChecked       = _vm.BoolCfg("dontFollowMobs");

        _syncing = false;
    }

    private void OnRarityChanged()
    {
        if (_syncing || _vm is null) return;
        var avoid   = new List<string>();
        var prefer  = new List<string>();
        var berserk = new List<string>();
        foreach (var row in _rarityRows)
        {
            if (row.Avoid)   avoid.Add(row.Rarity);
            if (row.Prefer)  prefer.Add(row.Rarity);
            if (row.Berserk) berserk.Add(row.Rarity);
        }
        _ = _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            ["avoidanceList"] = avoid,
            ["preferList"]    = prefer,
            ["berserkList"]   = berserk
        });
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
            _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { [key] = v });
    }

    private void TextBox_Changed(object? s, TextChangedEventArgs e)
    {
        if (_syncing || _vm is null || s is not TextBox tb || tb.Tag is not string key) return;
        _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { [key] = tb.Text ?? "" });
    }

    private void SetCurrent_Click(object? s, RoutedEventArgs e)
        => _vm?.PluginActionAsync("training.set-area-current");

    private void BrowseWalkScript_Click(object? s, RoutedEventArgs e)
        => _ = _vm?.BrowseScriptFileAsync("walkScript");
}
