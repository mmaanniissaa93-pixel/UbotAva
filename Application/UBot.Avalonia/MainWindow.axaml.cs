using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using global::Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using UBot.Avalonia.Controls;
using UBot.Avalonia.Services;

namespace UBot.Avalonia;

public partial class MainWindow : Window
{
    public static IUbotCoreService? CoreService { get; set; }
    public static AppState?         State       { get; set; }

    private IUbotCoreService? _core;
    private AppState?         _state;
    private FeatureViewFactory? _factory;
    private bool _isDark = true;
    private string _lang = "English";

    private static readonly (string Id, string Label, string Icon)[] DefaultPlugins =
    {
        ("UBot.General","General","general"),
        ("UBot.Training","Training","training"),
        ("UBot.Skills","Skills","skills"),
        ("UBot.Protection","Protection","protection"),
        ("UBot.Party","Party","party"),
        ("UBot.Alchemy","Alchemy","alchemy"),
        ("UBot.Trade","Trade","trade"),
        ("UBot.Lure","Lure","lure"),
        ("UBot.Quest","Quests","quest"),
        ("UBot.Inventory","Inventory","inventory"),
        ("UBot.Items","Items","items"),
        ("UBot.Map","Map","map"),
        ("UBot.Statistics","Statistics","stats"),
        ("UBot.Chat","Chat","chat"),
        ("UBot.Log","Log","log"),
        ("UBot.AutoDungeon","Auto Dungeon","autodungeon"),
        ("UBot.TargetAssist","Target Assist","targetassist"),
    };

    // Icon path geometry per key
    private static readonly Dictionary<string,string> IconGeo = new()
    {
        ["general"]     = "M3,12 L12,3 L21,12 L21,21 L15,21 L15,15 L9,15 L9,21 Z",
        ["training"]    = "M12,2 L15,8 L22,9 L17,14 L18,21 L12,18 L6,21 L7,14 L2,9 L9,8 Z",
        ["skills"]      = "M14.5,2.5 C14.5,2.5 18,6 18,10 C18,14 14,16 12,20 C10,16 6,14 6,10 C6,6 9.5,2.5 9.5,2.5 M12,10 A2,2 0 1,0 12,14 A2,2 0 0,0 12,10",
        ["protection"]  = "M12,2 L20,6 L20,12 C20,17 16,21 12,22 C8,21 4,17 4,12 L4,6 Z",
        ["party"]       = "M17,21 L17,19 A4,4 0 0,0 13,15 L5,15 A4,4 0 0,0 1,19 L1,21 M23,21 L23,19 A4,4 0 0,0 19,15 M16,3 A4,4 0 0,1 16,11 M9,11 A4,4 0 1,0 9,3 A4,4 0 0,0 9,11 Z",
        ["alchemy"]     = "M9,3 L9,10 L4,19 A1,1 0 0,0 5,21 L19,21 A1,1 0 0,0 20,19 L15,10 L15,3 M6,3 L18,3",
        ["trade"]       = "M17,2 L21,6 L17,10 M3,6 L21,6 M7,22 L3,18 L7,14 M21,18 L3,18",
        ["lure"]        = "M12,2 C7,2 2,7 2,12 C2,17 7,20 12,22 C17,20 22,17 22,12 C22,7 17,2 12,2 M12,8 L12,16 M8,12 L16,12",
        ["quest"]       = "M9,5 L4,5 L4,19 L20,19 L20,5 L15,5 M9,5 A3,3 0 0,1 15,5 M9,12 L15,12 M9,16 L13,16",
        ["inventory"]   = "M20,7 L4,7 L4,19 A2,2 0 0,0 6,21 L18,21 A2,2 0 0,0 20,19 Z M16,7 L16,5 A2,2 0 0,0 14,3 L10,3 A2,2 0 0,0 8,5 L8,7",
        ["items"]       = "M9,5 L9,3 L15,3 L15,5 M4,5 L20,5 L19,19 A2,2 0 0,1 17,21 L7,21 A2,2 0 0,1 5,19 Z",
        ["map"]         = "M3,7 L9,4 L15,7 L21,4 L21,17 L15,20 L9,17 L3,20 Z M9,4 L9,17 M15,7 L15,20",
        ["stats"]       = "M18,20 L18,10 M12,20 L12,4 M6,20 L6,14",
        ["chat"]        = "M21,15 A2,2 0 0,1 19,17 L7,17 L3,21 L3,5 A2,2 0 0,1 5,3 L19,3 A2,2 0 0,1 21,5 Z",
        ["log"]         = "M4,6 L20,6 M4,12 L20,12 M4,18 L14,18",
        ["autodungeon"] = "M12,2 A10,10 0 1,0 12,22 A10,10 0 0,0 12,2 Z M12,6 L12,12 L16,14",
        ["targetassist"]= "M12,2 A10,10 0 1,0 12,22 A10,10 0 0,0 12,2 Z M12,7 A5,5 0 1,0 12,17 A5,5 0 0,0 12,7 Z M12,2 L12,5 M12,19 L12,22 M2,12 L5,12 M19,12 L22,12",
    };

    public MainWindow() { InitializeComponent(); }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _core    = CoreService ?? new UbotCoreService();
        _state   = State       ?? new AppState();
        _factory = new FeatureViewFactory(_core, _state);
        _core.LogReceived += (level, message) =>
        {
            _state.AddLog($"[{level.ToUpperInvariant()}] {message}");
        };

        LoadBanner();
        BuildSidebar();
        InitTopbar();
        BindStateEvents();

        // Load initial status
        var status = await _core.GetStatusAsync();
        _state.ApplyStatus(status);
        SyncTopbar();

        var conn = await _core.GetConnectionOptionsAsync();
        _state.ConnectionOptions = conn;
        RebuildDivisionSelects(conn);

        var plugins = await _core.GetPluginsAsync();
        if (plugins.Count > 0)
        {
            _state.Plugins.Clear();
            foreach (var p in plugins) _state.Plugins.Add(p);
            BuildSidebar();
        }

        Navigate("UBot.General");
    }

    // â”€â”€â”€ Sidebar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private string? _activeId;
    private readonly Dictionary<string, Button> _navBtns = new();

    private void BuildSidebar()
    {
        NavPanel.Children.Clear();
        _navBtns.Clear();

        var coreIds    = new HashSet<string> { "general","skills","protection","party","training" };
        var autoIds    = new HashSet<string> { "alchemy","trade","lure","quest","quests","autodungeon","targetassist" };
        var dataIds    = new HashSet<string> { "inventory","items","map","stats","statistics","chat","log","command","server","packet" };

        var coreItems = new List<(string id, string label, string icon)>();
        var autoItems = new List<(string id, string label, string icon)>();
        var dataItems = new List<(string id, string label, string icon)>();

        IEnumerable<(string, string, string)> source;
        if (_state!.Plugins.Count > 0)
        {
            var list = new List<(string,string,string)>();
            foreach (var p in _state.Plugins)
                if (p.DisplayAsTab) list.Add((p.Id, p.Title, p.IconKey ?? NormKey(p.Id)));
            source = list;
        }
        else
        {
            source = DefaultPlugins;
        }

        foreach (var (id, label, icon) in source)
        {
            var k = NormKey(icon);
            if (coreIds.Contains(k))   coreItems.Add((id, label, k));
            else if (autoIds.Contains(k)) autoItems.Add((id, label, k));
            else                          dataItems.Add((id, label, k));
        }

        AddGroup("CORE", coreItems, isFirst: true);
        AddGroup("AUTOMATION", autoItems);
        AddGroup("DATA & TOOLS", dataItems);
    }

    private void AddGroup(string label, List<(string id, string lbl, string icon)> items, bool isFirst = false)
    {
        if (items.Count == 0) return;
        var grp = new TextBlock { Text = label };
        grp.Classes.Add("sidebar-group");
        if (isFirst) grp.Margin = new Thickness(12, 2, 12, 3);
        NavPanel.Children.Add(grp);
        foreach (var (id, lbl, icon) in items) NavPanel.Children.Add(MakeNavBtn(id, lbl, icon));
    }

    private Button MakeNavBtn(string id, string label, string iconKey)
    {
        var stack = new StackPanel { Orientation = global::Avalonia.Layout.Orientation.Horizontal, Spacing = 9, VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center };

        if (IconGeo.TryGetValue(iconKey, out var geo))
        {
            stack.Children.Add(new Path
            {
                Data = Geometry.Parse(geo),
                Stroke = new SolidColorBrush(Color.Parse("#6688AACC")),
                StrokeThickness = 1.4,
                Width = 15, Height = 15, Stretch = Stretch.Uniform,
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center
            });
        }

        stack.Children.Add(new TextBlock { Text = label, VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center });

        var btn = new Button { Content = stack, Tag = id };
        btn.Classes.Add("sidebar-item");
        btn.Click += (_, _) => Navigate(id);
        _navBtns[id] = btn;
        return btn;
    }

    private void Navigate(string id)
    {
        _activeId = id;
        foreach (var (k, b) in _navBtns)
        {
            b.Classes.Remove("active");
            if (k == id) b.Classes.Add("active");
        }
        if (_factory != null) ContentHost.Content = _factory.GetView(id);
    }

    // â”€â”€â”€ Topbar â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void InitTopbar()
    {
        LblStart.Text      = "START";
        LblStop.Text       = "STOP";
        LblDisconnect.Text = "Disconnect";
        LblStartClient.Text = "Start Client";
        LblGoClientless.Text = "Go Clientless";
        LblHideClient.Text = "Hide Client";
        BtnEn.Classes.Add("active");
        SyncTopbar();
    }

    private void SyncTopbar()
    {
        if (_state is null) return;

        // Status chip
        var running = _state.BotRunning;
        StatusText.Text = running ? "On" : "Off";
        if (running)
        {
            StatusChipBorder.Classes.Remove("status-stopped");
            StatusChipBorder.Classes.Add("status-running");
            StatusDot.Fill  = new SolidColorBrush(Color.Parse("#34D399"));
            StatusText.Foreground = new SolidColorBrush(Color.Parse("#34D399"));
        }
        else
        {
            StatusChipBorder.Classes.Remove("status-running");
            StatusChipBorder.Classes.Add("status-stopped");
            StatusDot.Fill  = new SolidColorBrush(Color.Parse("#F26C6C"));
            StatusText.Foreground = new SolidColorBrush(Color.Parse("#F26C6C"));
        }

        BtnStart.IsEnabled = !running;
        BtnStop.IsEnabled  = running;
        ProfileLabel.Text  = _state.Profile;

        // Character pill
        var ch = _state.Character.Trim();
        var waiting = string.IsNullOrEmpty(ch) || ch == "-";
        CharLabel.Text = waiting ? "Waiting for Character..." : ch;
        if (waiting)
        {
            CharPill.Classes.Add("waiting");
            CharIcon.Content = new Ellipse { Width = 8, Height = 8, Fill = new SolidColorBrush(Color.Parse("#F59E0B")) };
            CharLabel.Foreground = new SolidColorBrush(Color.Parse("#FFC96A"));
        }
        else
        {
            CharPill.Classes.Remove("waiting");
            CharIcon.Content = new Path
            {
                Data = Geometry.Parse("M12,2 L20,6 L20,12 C20,17 16,21 12,22 C8,21 4,17 4,12 L4,6 Z"),
                Stroke = new SolidColorBrush(Color.Parse("#64F3D0")),
                StrokeThickness = 1.4, Width = 13, Height = 13, Stretch = Stretch.Uniform
            };
            CharLabel.Foreground = new SolidColorBrush(Color.Parse("#EEF4FF"));
        }

        // Metrics
        if (_state.HasLiveStats)
        {
            MetricLevel.Text = _state.PlayerLevel.ToString();
            MetricHp.Text    = $"{_state.PlayerHealth:N0} / {_state.PlayerMaxHealth:N0}";
            MetricMp.Text    = $"{_state.PlayerMana:N0} / {_state.PlayerMaxMana:N0}";
            MetricExp.Text   = $"{_state.PlayerExpPercent:F2}%";
            MetricLevelHint.Text = "Live";
            MetricHpHint.Text = "Live";
            MetricMpHint.Text = "Live";
            MetricExpHint.Text = "Live";
            UpdateBar(HpBar,  _state.PlayerHealthPercent);
            UpdateBar(MpBar,  _state.PlayerManaPercent);
            UpdateBar(ExpBar, _state.PlayerExpPercent);
        }
        else
        {
            MetricLevel.Text = "-";
            MetricHp.Text = "-";
            MetricMp.Text = "-";
            MetricExp.Text = "-";
            MetricLevelHint.Text = "Waiting";
            MetricHpHint.Text = "Waiting";
            MetricMpHint.Text = "Waiting";
            MetricExpHint.Text = "Waiting";
            UpdateBar(HpBar, 0); UpdateBar(MpBar, 0); UpdateBar(ExpBar, 0);
        }
    }

    private static void UpdateBar(Border bar, double pct)
    {
        // Width is set proportionally on SizeChanged; store pct as Tag
        bar.Tag = Math.Clamp(pct, 0, 100);
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        RecalcBars();
    }

    private void RecalcBars()
    {
        // The bars recalc after layout; use deferred
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // Find parent Grid to get track width
            if (HpBar.Parent is Grid g1)  { var t = g1.Children[0] as Border; if(t!=null) HpBar.Width  = t.Bounds.Width * (HpBar.Tag  is double d1 ? d1/100 : 0); }
            if (MpBar.Parent is Grid g2)  { var t = g2.Children[0] as Border; if(t!=null) MpBar.Width  = t.Bounds.Width * (MpBar.Tag  is double d2 ? d2/100 : 0); }
            if (ExpBar.Parent is Grid g3) { var t = g3.Children[0] as Border; if(t!=null) ExpBar.Width = t.Bounds.Width * (ExpBar.Tag is double d3 ? d3/100 : 0); }
        });
    }

    private void RebuildDivisionSelects(ConnectionOptions opts)
    {
        var divs = new List<SelectOption>();
        foreach (var d in opts.Divisions) divs.Add(new SelectOption(d.Index, d.Name));
        DivisionSelect.Options       = divs;
        DivisionSelect.SelectedValue = opts.DivisionIndex;
        DivisionSelect.SelectionChanged += v => _ = _core!.SetConnectionOptionsAsync((int)v!, opts.GatewayIndex);

        var found = opts.Divisions.Find(d => d.Index == opts.DivisionIndex);
        var srvs  = new List<SelectOption>();
        if (found != null) foreach (var s in found.Servers) srvs.Add(new SelectOption(s.Index, s.Name));
        GatewaySelect.Options       = srvs;
        GatewaySelect.SelectedValue = opts.GatewayIndex;
        GatewaySelect.SelectionChanged += v => _ = _core!.SetConnectionOptionsAsync(opts.DivisionIndex, (int)v!);
    }

    // â”€â”€â”€ State events â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void BindStateEvents()
    {
        _state!.PropertyChanged += (_, pe) =>
        {
            switch (pe.PropertyName)
            {
                case nameof(AppState.BotRunning):
                case nameof(AppState.Profile):
                case nameof(AppState.Character):
                case nameof(AppState.HasLiveStats):
                case nameof(AppState.PlayerLevel):
                case nameof(AppState.PlayerHealthPercent):
                case nameof(AppState.PlayerManaPercent):
                case nameof(AppState.PlayerExpPercent):
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(SyncTopbar);
                    break;
                case nameof(AppState.Plugins):
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(BuildSidebar);
                    break;
            }
        };
    }

    // â”€â”€â”€ Button handlers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private async void BtnStart_Click(object? s, RoutedEventArgs e)
    {
        var r = await _core!.StartBotAsync();
        _state!.ApplyStatus(r);
    }
    private async void BtnStop_Click(object? s, RoutedEventArgs e)
    {
        var r = await _core!.StopBotAsync();
        _state!.ApplyStatus(r);
    }
    private async void BtnDisconnect_Click(object? s, RoutedEventArgs e)
    {
        var r = await _core!.DisconnectAsync();
        _state!.ApplyStatus(r);
    }
    private async void BtnSave_Click(object? s, RoutedEventArgs e) => await _core!.SaveConfigAsync();
    private async void BtnStartClient_Click(object? s, RoutedEventArgs e) => await _core!.StartClientAsync();
    private async void BtnGoClientless_Click(object? s, RoutedEventArgs e) => await _core!.GoClientlessAsync();
    private async void BtnHideClient_Click(object? s, RoutedEventArgs e) => await _core!.ToggleClientVisibilityAsync();

    private void BtnEn_Click(object? s, RoutedEventArgs e)
    {
        _lang = "English"; BtnEn.Classes.Add("active"); BtnTr.Classes.Remove("active");
        ApplyTranslations();
    }
    private void BtnTr_Click(object? s, RoutedEventArgs e)
    {
        _lang = "Turkish"; BtnTr.Classes.Add("active"); BtnEn.Classes.Remove("active");
        ApplyTranslations();
    }
    private void BtnTheme_Click(object? s, RoutedEventArgs e)
    {
        _isDark = !_isDark;
        RequestedThemeVariant = _isDark ? global::Avalonia.Styling.ThemeVariant.Dark : global::Avalonia.Styling.ThemeVariant.Light;
        ThemeIcon.Data = _isDark
            ? Geometry.Parse("M12,3 C9.1,3 6.4,4.5 4.8,7 C3.2,9.5 3.2,12.5 4.8,15 C6.4,17.5 9.1,19 12,19 C16,19 19,16 19,12 A9,9 0 0,0 12,3 Z")
            : Geometry.Parse("M12,2 L12,4 M12,20 L12,22 M4.22,4.22 L5.64,5.64 M18.36,18.36 L19.78,19.78 M2,12 L4,12 M20,12 L22,12 M4.22,19.78 L5.64,18.36 M18.36,5.64 L19.78,4.22 M12,17 A5,5 0 1,0 12,7 A5,5 0 0,0 12,17 Z");
    }

    private void ApplyTranslations()
    {
        bool tr = _lang == "Turkish";
        LblStart.Text      = tr ? "Baslat" : "START";
        LblStop.Text       = tr ? "Durdur" : "STOP";
        LblDisconnect.Text = tr ? "Baglantiyi Kes" : "Disconnect";
        LblStartClient.Text = tr ? "Client Baslat" : "Start Client";
        LblGoClientless.Text = tr ? "Clientless" : "Go Clientless";
        LblHideClient.Text = tr ? "Client Gizle" : "Hide Client";
        SyncTopbar();
    }

    // â”€â”€â”€ Banner â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void LoadBanner()
    {
        try
        {
            var bmp = new Bitmap(AssetLoader.Open(new Uri("avares://UBot.Avalonia/Assets/ubot_banner_night.png")));
            BannerImage.Source = bmp;
        }
        catch { /* banner not yet added */ }
    }

    // â”€â”€â”€ Drag â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void DragZone_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
    }

    private void MinimizeWindow_Click(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void CloseWindow_Click(object? sender, RoutedEventArgs e) => Close();

    private static string NormKey(string s)
        => s.ToLowerInvariant().Replace("ubot.", "").Replace(".", "").Replace(" ", "").Replace("-", "");
}

