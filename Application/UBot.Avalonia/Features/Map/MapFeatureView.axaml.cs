using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
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
    private static readonly IBrush UniqueDotBrush = new SolidColorBrush(Color.Parse("#F87171"));
    private static readonly IBrush MonsterDotBrush = new SolidColorBrush(Color.Parse("#FB923C"));
    private static readonly IBrush PartyDotBrush = new SolidColorBrush(Color.Parse("#34D399"));
    private static readonly IBrush PlayerDotBrush = new SolidColorBrush(Color.Parse("#60A5FA"));
    private static readonly IBrush NpcDotBrush = new SolidColorBrush(Color.Parse("#A78BFA"));
    private static readonly IBrush CosDotBrush = new SolidColorBrush(Color.Parse("#38BDF8"));
    private static readonly IBrush ItemDotBrush = new SolidColorBrush(Color.Parse("#FACC15"));
    private static readonly IBrush PortalDotBrush = new SolidColorBrush(Color.Parse("#E879F9"));
    private static readonly IBrush DefaultDotBrush = new SolidColorBrush(Color.Parse("#CBD5E1"));
    private static readonly IBrush PlayerMarkerBrush = new SolidColorBrush(Color.Parse("#FFCD4C"));
    private static readonly IBrush PlayerMarkerStrokeBrush = new SolidColorBrush(Color.Parse("#4A3412"));

    private sealed class MapRenderDot
    {
        public string Type { get; init; } = string.Empty;
        public double XOffset { get; init; }
        public double YOffset { get; init; }
    }

    private PluginViewModelBase? _vm;
    private string _activeTab = "minimap";
    private string _loadedMapImageVersion = string.Empty;

    private Bitmap? _minimapBmp;
    private Bitmap? _navmeshBmp;
    private double _mapW = 1920, _mapH = 1920;
    private double _playerX, _playerY;

    private readonly ObservableCollection<MapEntityRow> _entityRows = new();
    private readonly List<MapRenderDot> _renderDots = new();

    public MapFeatureView()
    {
        InitializeComponent();
        EntityGrid.ItemsSource = _entityRows;
        MapCanvas.SizeChanged += (_, _) => RedrawMapDots();

        MapTabs.SetTabs(new[] { ("minimap", "Minimap"), ("navmesh", "NavMesh Viewer") });
        MapTabs.TabChanged += t =>
        {
            _activeTab = t;
            ResetToPlayerBtn.IsVisible = t == "navmesh";
            ApplyMapPresentation();
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
        ShowFilterSelect.SelectedValue  = vm.TextCfg("showFilter", "All");
        ApplyMapPresentation();
    }

    public void UpdateFromState(JsonElement moduleState)
    {
        static double N(JsonElement e, double fb) => e.ValueKind == JsonValueKind.Number ? e.GetDouble() : fb;
        static string S(JsonElement e, string fb) => e.ValueKind == JsonValueKind.String ? e.GetString() ?? fb : fb;

        var map = moduleState.TryGetProperty("map", out var m) ? m : moduleState;

        var incomingMapImageVersion = map.TryGetProperty("mapImageVersion", out var mv)
            ? S(mv, string.Empty)
            : string.Empty;
        var shouldReloadImages = false;
        if (!string.IsNullOrWhiteSpace(incomingMapImageVersion))
        {
            if (!string.Equals(_loadedMapImageVersion, incomingMapImageVersion, StringComparison.Ordinal))
            {
                _loadedMapImageVersion = incomingMapImageVersion;
                shouldReloadImages = true;
            }
        }
        else if (_minimapBmp == null || _navmeshBmp == null)
        {
            shouldReloadImages = true;
        }

        if ((_minimapBmp == null || _navmeshBmp == null) && (map.TryGetProperty("minimapImage", out _) || map.TryGetProperty("navmeshImage", out _)))
            shouldReloadImages = true;

        if (shouldReloadImages)
        {
            if (map.TryGetProperty("minimapImage", out var mi))
                TryLoadBmp(mi.GetString(), ref _minimapBmp);

            if (map.TryGetProperty("navmeshImage", out var ni))
                TryLoadBmp(ni.GetString(), ref _navmeshBmp);
        }

        if (map.TryGetProperty("mapWidth",  out var mw)) _mapW = N(mw, 1920);
        if (map.TryGetProperty("mapHeight", out var mh)) _mapH = N(mh, 1920);
        if (map.TryGetProperty("playerXOffset", out var px)) _playerX = N(px, 0);
        if (map.TryGetProperty("playerYOffset", out var py)) _playerY = N(py, 0);
        if (map.TryGetProperty("showFilter", out var sf))
        {
            var selectedFilter = S(sf, "All");
            var currentFilter = ShowFilterSelect.SelectedValue?.ToString() ?? string.Empty;
            if (!string.Equals(currentFilter, selectedFilter, StringComparison.OrdinalIgnoreCase))
                ShowFilterSelect.SelectedValue = selectedFilter;
        }

        var total = map.TryGetProperty("total",    out var t)  ? N(t, 0).ToString("F0") : "0";
        var mons  = map.TryGetProperty("monsters", out var mo) ? N(mo, 0).ToString("F0") : "0";
        var plrs  = map.TryGetProperty("players",  out var pl) ? N(pl, 0).ToString("F0") : "0";
        var source = map.TryGetProperty("mapImageSource", out var ms) ? S(ms, "unknown") : "unknown";
        MapOverlayText.Text = $"Entities: {total}  |  Monsters: {mons}  |  Players: {plrs}  |  Source: {source}";

        _entityRows.Clear();
        _renderDots.Clear();
        if (map.TryGetProperty("entities", out var ents) && ents.ValueKind == JsonValueKind.Array)
            foreach (var e in ents.EnumerateArray())
            {
                var lvl = e.TryGetProperty("level", out var lv) ? N(lv, 0) : 0;
                var typ = e.TryGetProperty("type", out var ty) ? S(ty, "-") : "-";
                _entityRows.Add(new MapEntityRow
                {
                    Name     = e.TryGetProperty("name",     out var n)  ? S(n, "-")  : "-",
                    Type     = typ,
                    Level    = lvl > 0 ? lvl.ToString("F0") : "-",
                    Position = e.TryGetProperty("position", out var po) ? S(po, "")  : ""
                });

                if (e.TryGetProperty("xOffset", out var xo) && e.TryGetProperty("yOffset", out var yo))
                {
                    _renderDots.Add(new MapRenderDot
                    {
                        Type = typ,
                        XOffset = N(xo, 0),
                        YOffset = N(yo, 0)
                    });
                }
            }

        ApplyMapPresentation();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        DisposeMapBitmaps();
        base.OnDetachedFromVisualTree(e);
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

    private void ApplyMapPresentation()
    {
        var activeBitmap = _activeTab == "navmesh"
            ? (_navmeshBmp ?? _minimapBmp)
            : (_minimapBmp ?? _navmeshBmp);

        if (activeBitmap != null)
        {
            MapCanvas.Background = new ImageBrush(activeBitmap) { Stretch = Stretch.Fill };
        }
        else
        {
            var fallback = ActualThemeVariant == ThemeVariant.Dark ? "#0B1322" : "#EAF1FB";
            MapCanvas.Background = new SolidColorBrush(Color.Parse(fallback));
        }

        RedrawMapDots();
    }

    private void RedrawMapDots()
    {
        var bounds = MapCanvas.Bounds;
        if (bounds.Width <= 0 || bounds.Height <= 0 || _mapW <= 0 || _mapH <= 0)
            return;

        MapCanvas.Children.Clear();

        foreach (var dot in _renderDots)
        {
            var x = Math.Clamp(dot.XOffset / _mapW * bounds.Width, 0, bounds.Width);
            var y = Math.Clamp(dot.YOffset / _mapH * bounds.Height, 0, bounds.Height);
            var marker = new Ellipse
            {
                Width = 5,
                Height = 5,
                Fill = DotBrush(dot.Type),
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };

            Canvas.SetLeft(marker, x - marker.Width / 2);
            Canvas.SetTop(marker, y - marker.Height / 2);
            MapCanvas.Children.Add(marker);
        }

        if (_playerX <= 0 && _playerY <= 0)
            return;

        var px = Math.Clamp(_playerX / _mapW * bounds.Width, 0, bounds.Width);
        var py = Math.Clamp(_playerY / _mapH * bounds.Height, 0, bounds.Height);
        var playerMarker = new Ellipse
        {
            Width = 10,
            Height = 10,
            Fill = PlayerMarkerBrush,
            Stroke = PlayerMarkerStrokeBrush,
            StrokeThickness = 2
        };

        Canvas.SetLeft(playerMarker, px - playerMarker.Width / 2);
        Canvas.SetTop(playerMarker, py - playerMarker.Height / 2);
        MapCanvas.Children.Add(playerMarker);
    }

    private static IBrush DotBrush(string type)
    {
        return type.ToLowerInvariant() switch
        {
            "unique" => UniqueDotBrush,
            "monster" => MonsterDotBrush,
            "party" => PartyDotBrush,
            "player" => PlayerDotBrush,
            "npc" => NpcDotBrush,
            "cos" => CosDotBrush,
            "item" => ItemDotBrush,
            "portal" => PortalDotBrush,
            _ => DefaultDotBrush
        };
    }

    private static void TryLoadBmp(string? b64, ref Bitmap? bmp)
    {
        if (string.IsNullOrEmpty(b64)) return;
        try
        {
            var data = b64.Contains(',') ? b64[(b64.IndexOf(',') + 1)..] : b64;
            using var ms = new MemoryStream(Convert.FromBase64String(data));
            var nextBitmap = new Bitmap(ms);
            bmp?.Dispose();
            bmp = nextBitmap;
        }
        catch { }
    }

    private void DisposeMapBitmaps()
    {
        _minimapBmp?.Dispose();
        _minimapBmp = null;

        _navmeshBmp?.Dispose();
        _navmeshBmp = null;

        _loadedMapImageVersion = string.Empty;
    }
}

