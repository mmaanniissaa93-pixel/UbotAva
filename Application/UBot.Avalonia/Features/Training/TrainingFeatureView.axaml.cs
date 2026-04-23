using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;
using UBot.Core.Objects;

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
        SetCurrentBtn.Content = "Set Current Position";
        MonstersLabel.Text  = "monsters";
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

        var region = (ushort)_vm.NumCfg("areaRegion");
        var xOffset = (float)_vm.NumCfg("areaX");
        var yOffset = (float)_vm.NumCfg("areaY");
        var zOffset = (float)_vm.NumCfg("areaZ");

        var pos = new Position { Region = region, XOffset = xOffset, YOffset = yOffset, ZOffset = zOffset };
        
        RegionBox.Text = region.ToString("F0");
        RadiusBox.Text = _vm.NumCfg("areaRadius", 50).ToString("F0");
        GlobalXBox.Text = pos.X.ToString("F2");
        GlobalYBox.Text = pos.Y.ToString("F2");

        CurrentPositionLabel.Text = $"Region: {region} | Local: {xOffset:F0}, {yOffset:F0}";
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

private void Toggle_Changed(object? s, RoutedEventArgs e)
    {
        if (_syncing || _vm is null) return;
        bool? isChecked = null;
        if (s is ToggleSwitch ts) isChecked = ts.IsChecked;
        else if (s is CheckBox cb) isChecked = cb.IsChecked;
        if (isChecked is not null && s is Control c && c.Tag is string key)
            _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { [key] = isChecked });
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

    private void GlobalNumBox_Changed(object? s, TextChangedEventArgs e)
    {
        if (_syncing || _vm is null) return;
        if (!double.TryParse(GlobalXBox.Text, out var gx) || !double.TryParse(GlobalYBox.Text, out var gy)) return;

        // Convert global to region/offset
        var region = (ushort)_vm.NumCfg("areaRegion");
        var pos = new Position((float)gx, (float)gy, region);

        _ = _vm.PatchConfigAsync(new Dictionary<string, object?>
        {
            ["areaRegion"] = (double)pos.Region.Id,
            ["areaX"] = (double)pos.XOffset,
            ["areaY"] = (double)pos.YOffset
        });
        
        // Update the small label to show what's happening behind the scenes
        CurrentPositionLabel.Text = $"Region: {pos.Region.Id} | Local: {pos.XOffset:F0}, {pos.YOffset:F0}";
    }

    private async void SetCurrent_Click(object? s, RoutedEventArgs e)
    {
        if (_vm == null) return;
        await _vm.PluginActionAsync("training.set-area-current");
        await _vm.LoadConfigAsync();
        RefreshFromConfig();
    }

    private void BrowseWalkScript_Click(object? s, RoutedEventArgs e)
        => _ = _vm?.BrowseScriptFileAsync("walkScript");
}
