using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using global::Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using UBot.Avalonia.Controls;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.Map;

public class MapEntityRow
{
    public string Name     { get; set; } = "";
    public string Type     { get; set; } = "";
    public string Level    { get; set; } = "";
    public string Position { get; set; } = "";
}

public partial class MapFeatureView : UserControl
{
    private PluginViewModelBase? _vm;
    private string _activeTab = "minimap";

    private Bitmap? _minimapBmp;
    private Bitmap? _navmeshBmp;
    private double _mapW = 1920, _mapH = 1920;
    private double _playerX, _playerY;

    private readonly ObservableCollection<MapEntityRow> _entityRows = new();

    public MapFeatureView()
    {
        InitializeComponent();
        EntityGrid.ItemsSource = _entityRows;

        MapTabs.SetTabs(new[] { ("minimap", "Minimap"), ("navmesh", "NavMesh Viewer") });
        MapTabs.TabChanged += t =>
        {
            _activeTab = t;
            ResetToPlayerBtn.IsVisible = t == "navmesh";
            MapCanvas.InvalidateVisual();
        };

        var filterOpts = new List<SelectOption>();
        foreach (var f in new[] { "All","Monsters","Players","Party","NPC","COS","Items","Portals","Uniques" })
            filterOpts.Add(new SelectOption(f, f));
        ShowFilterSelect.Options = filterOpts;
        ShowFilterSelect.SelectionChanged += v =>
        {
            if (_vm != null)
                _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { ["showFilter"] = v?.ToString() });
        };
    }

    public void Initialize(PluginViewModelBase vm)
    {
        _vm = vm;
        CollisionCheck.IsChecked        = vm.BoolCfg("collisionDetection");
        AutoSelectUniqueCheck.IsChecked = vm.BoolCfg("autoSelectUniques");
    }

    public void UpdateFromState(JsonElement moduleState)
    {
        static double N(JsonElement e, double fb) => e.ValueKind == JsonValueKind.Number ? e.GetDouble() : fb;
        static string S(JsonElement e, string fb) => e.ValueKind == JsonValueKind.String ? e.GetString() ?? fb : fb;

        var map = moduleState.TryGetProperty("map", out var m) ? m : moduleState;

        if (map.TryGetProperty("minimapImage", out var mi)) TryLoadBmp(mi.GetString(), ref _minimapBmp);
        if (map.TryGetProperty("navmeshImage",  out var ni)) TryLoadBmp(ni.GetString(), ref _navmeshBmp);
        if (map.TryGetProperty("mapWidth",  out var mw)) _mapW = N(mw, 1920);
        if (map.TryGetProperty("mapHeight", out var mh)) _mapH = N(mh, 1920);
        if (map.TryGetProperty("playerXOffset", out var px)) _playerX = N(px, 0);
        if (map.TryGetProperty("playerYOffset", out var py)) _playerY = N(py, 0);

        var total = map.TryGetProperty("total",    out var t)  ? N(t, 0).ToString("F0") : "0";
        var mons  = map.TryGetProperty("monsters", out var mo) ? N(mo, 0).ToString("F0") : "0";
        var plrs  = map.TryGetProperty("players",  out var pl) ? N(pl, 0).ToString("F0") : "0";
        MapOverlayText.Text = $"Entities: {total}  |  Monsters: {mons}  |  Players: {plrs}";

        _entityRows.Clear();
        if (map.TryGetProperty("entities", out var ents) && ents.ValueKind == JsonValueKind.Array)
            foreach (var e in ents.EnumerateArray())
            {
                var lvl = e.TryGetProperty("level", out var lv) ? N(lv, 0) : 0;
                _entityRows.Add(new MapEntityRow
                {
                    Name     = e.TryGetProperty("name",     out var n)  ? S(n, "-")  : "-",
                    Type     = e.TryGetProperty("type",     out var ty) ? S(ty, "-") : "-",
                    Level    = lvl > 0 ? lvl.ToString("F0") : "-",
                    Position = e.TryGetProperty("position", out var po) ? S(po, "")  : ""
                });
            }

        MapCanvas.InvalidateVisual();
    }

    private void MapCanvas_PointerPressed(object? s, PointerPressedEventArgs e)
    {
        if (_vm is null) return;
        var pos    = e.GetPosition(MapCanvas);
        var bounds = MapCanvas.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        var mapX = pos.X / bounds.Width  * _mapW;
        var mapY = pos.Y / bounds.Height * _mapH;
        _ = _vm.PluginActionAsync("map.walk-to",
            new Dictionary<string, object?> { ["mapX"] = mapX, ["mapY"] = mapY });
    }

    private void Check_Changed(object? s, RoutedEventArgs e)
    {
        if (_vm is null || s is not CheckBox cb || cb.Tag is not string key) return;
        _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { [key] = cb.IsChecked == true });
    }

    private void ResetToPlayer_Click(object? s, RoutedEventArgs e)
        => _ = _vm?.InvokeActionCandidatesAsync(
            new[] { "map.reset-to-player", "map.center-on-player", "map.navmesh.reset-to-player" });

    private static void TryLoadBmp(string? b64, ref Bitmap? bmp)
    {
        if (string.IsNullOrEmpty(b64)) return;
        try
        {
            var data = b64.Contains(',') ? b64[(b64.IndexOf(',') + 1)..] : b64;
            using var ms = new MemoryStream(Convert.FromBase64String(data));
            bmp = new Bitmap(ms);
        }
        catch { }
    }
}

