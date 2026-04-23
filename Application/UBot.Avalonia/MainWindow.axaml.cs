using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Animation.Easings;
using Avalonia.Styling;
using Avalonia.Threading;
using global::Avalonia.Media.Imaging;
using Avalonia.Platform;
using System;
using System.Collections.Generic;
using System.Threading;
using UBot.Avalonia.Controls;
using UBot.Avalonia.Features.CommandCenter;
using UBot.Avalonia.Services;
using UBot.Avalonia.Dialogs;
using UBot.Core;

namespace UBot.Avalonia;

public partial class MainWindow : Window
{
    private const string DesktopThemeConfigKey = "UBot.Desktop.Theme";
    private const string DesktopLanguageConfigKey = "UBot.Desktop.Language";

    public static IUbotCoreService? CoreService { get; set; }
    public static AppState?         State       { get; set; }

    private IUbotCoreService? _core;
    private AppState?         _state;
    private FeatureViewFactory? _factory;
    private bool _isDark = true;
    private string _lang = "English";
    private readonly DispatcherTimer _runtimePollTimer = new() { Interval = TimeSpan.FromMilliseconds(900) };
    private bool _pollInProgress;
    private bool _syncingConnectionSelects;
    private int _connectionPollCounter;
    private CancellationTokenSource? _statusPulseCts;

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

    private static Dictionary<string, string> IconGeo => IconPaths.Geometries;

    public MainWindow()
    {
        InitializeComponent();
        _runtimePollTimer.Tick += RuntimePollTimer_Tick;
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        _core    = CoreService ?? new UbotCoreService();
        _state   = State       ?? new AppState();
        _factory = new FeatureViewFactory(_core, _state);
        LoadDesktopPreferences();
        _core.LogReceived += (level, message) =>
        {
            _state.AddLog($"[{level.ToUpperInvariant()}] {message}");
        };
        _core.ChatMessageReceived += (channel, sender, message) =>
        {
            _state.AddChatMessage(channel, sender, message);
        };

        BuildSidebar();
        InitTopbar();
        BindStateEvents();
        if (MenuSelectProfile != null)
        {
            MenuSelectProfile.Click -= OnSelectProfileClick;
            MenuSelectProfile.Click += OnSelectProfileClick;
        }
        if (MenuProxyConfig != null)
        {
            MenuProxyConfig.Click -= OnProxyConfigClick;
            MenuProxyConfig.Click += OnProxyConfigClick;
        }

        await RefreshRuntimeStatusAsync(forceConnectionRefresh: true);

        var plugins = await _core.GetPluginsAsync();
        if (plugins.Count > 0)
        {
            _state.Plugins.Clear();
            foreach (var p in plugins) _state.Plugins.Add(p);
            BuildSidebar();
            BuildMenu();
        }

        Navigate("UBot.General");
        _runtimePollTimer.Start();
    }

    protected override void OnClosed(EventArgs e)
    {
        _runtimePollTimer.Stop();
        _runtimePollTimer.Tick -= RuntimePollTimer_Tick;
        StopStatusPulse();
        PersistDesktopPreferences();
        try
        {
            _core?.SaveConfigAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // ignore save failures during shutdown
        }
        base.OnClosed(e);
    }

    private async void RuntimePollTimer_Tick(object? sender, EventArgs e)
    {
        if (_pollInProgress)
            return;

        _pollInProgress = true;
        try
        {
            _connectionPollCounter++;
            var forceConnectionRefresh = _connectionPollCounter >= 3;
            await RefreshRuntimeStatusAsync(forceConnectionRefresh);
            if (forceConnectionRefresh)
                _connectionPollCounter = 0;
        }
        finally
        {
            _pollInProgress = false;
        }
    }

    private async System.Threading.Tasks.Task RefreshRuntimeStatusAsync(bool forceConnectionRefresh = false)
    {
        if (_core == null || _state == null)
            return;

        var status = await _core.GetStatusAsync();
        _state.ApplyStatus(status);

        if (forceConnectionRefresh
            || status.DivisionIndex != _state.ConnectionOptions.DivisionIndex
            || status.GatewayIndex != _state.ConnectionOptions.GatewayIndex)
        {
            var options = await _core.GetConnectionOptionsAsync();
            _state.ConnectionOptions = options;
            RebuildDivisionSelects(options);
        }

        if (_factory != null && !string.IsNullOrWhiteSpace(_activeId))
        {
            var pluginState = await _core.GetPluginStateAsync(_activeId);
            if (pluginState.State is { } moduleState)
            {
                _factory.UpdateState(_activeId, moduleState);
                ApplyLanguageToActiveViewDeferred();
            }
        }
    }

    // ——————————————————————————————————————————————————————————————————————————

    private string? _activeId;
    private readonly Dictionary<string, Button> _navBtns = new();

    private void BuildSidebar()
    {
        NavPanel.Children.Clear();
        _navBtns.Clear();

        var coreIds    = new HashSet<string> { "general","skills","protection","party","training" };
        var dataIds    = new HashSet<string> { "inventory","items","map","stats","statistics","chat","log","command","server","packet" };

        var coreItems = new List<(string id, string label, string icon)>();
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
            // Removed from sidebar: else if (autoIds.Contains(k)) autoItems.Add((id, label, k));
            else if (dataIds.Contains(k)) dataItems.Add((id, label, k));
        }

        AddGroup(DesktopLanguageService.Translate("CORE"), coreItems, isFirst: true);
        // Removed from sidebar: AddGroup("AUTOMATION", autoItems);
        AddGroup(DesktopLanguageService.Translate("DATA & TOOLS"), dataItems);
    }

    private void BuildMenu()
    {
        if (PluginsMenu == null || AutomationMenu == null) return;

        PluginsMenu.Items.Clear();
        AutomationMenu.Items.Clear();
        // Plugins Menu: Quests, Command center, Target Assist
        var questsMi = new MenuItem { Header = DesktopLanguageService.Translate("Quests") };
        questsMi.Click += (_, _) => Navigate("UBot.Quest");
        PluginsMenu.Items.Add(questsMi);

        var commandMi = new MenuItem { Header = DesktopLanguageService.Translate("Command center") };
        commandMi.Click += (_, _) => Navigate("UBot.CommandCenter");
        PluginsMenu.Items.Add(commandMi);

        var assistMi = new MenuItem { Header = DesktopLanguageService.Translate("Target Assist") };
        assistMi.Click += (_, _) => Navigate("UBot.TargetAssist");
        PluginsMenu.Items.Add(assistMi);

        // Automation Menu: Alchemy, Lure, Trade
        var alchemyMi = new MenuItem { Header = DesktopLanguageService.Translate("Alchemy") };
        alchemyMi.Click += (_, _) => Navigate("UBot.Alchemy");
        AutomationMenu.Items.Add(alchemyMi);

        var lureMi = new MenuItem { Header = "Lure" };
        lureMi.Click += (_, _) => Navigate("UBot.Lure");
        AutomationMenu.Items.Add(lureMi);

        var tradeMi = new MenuItem { Header = DesktopLanguageService.Translate("Trade") };
        tradeMi.Click += (_, _) => Navigate("UBot.Trade");
        AutomationMenu.Items.Add(tradeMi);
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
                Stroke = new SolidColorBrush(Color.Parse("#7FA4CC")),
                StrokeThickness = 1.6,
                StrokeLineCap = PenLineCap.Round,
                Width = 16, Height = 16, Stretch = Stretch.Uniform,
                VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center
            });
        }

        stack.Children.Add(new TextBlock
        {
            Text = DesktopLanguageService.Translate(label),
            VerticalAlignment = global::Avalonia.Layout.VerticalAlignment.Center
        });

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
        if (_factory == null)
            return;

        var view = _factory.GetView(id);
        ContentHost.Content = view;
        ApplyLanguageToActiveViewDeferred();

        if (view is CommandCenterFeatureView commandCenterView)
            commandCenterView.OpenPopup(this);
    }

    private void ApplyLanguageToActiveViewDeferred()
    {
        if (ContentHost.Content is not Control control)
            return;

        // Immediate pass for already built controls.
        DesktopLanguageService.ApplyToControl(control, _lang);

        // Deferred passes for sections that populate asynchronously after navigation.
        Dispatcher.UIThread.Post(() =>
        {
            if (ContentHost.Content is Control current)
                DesktopLanguageService.ApplyToControl(current, _lang);
        }, DispatcherPriority.Background);

        Dispatcher.UIThread.Post(() =>
        {
            if (ContentHost.Content is Control current)
                DesktopLanguageService.ApplyToControl(current, _lang);
        }, DispatcherPriority.Loaded);
    }

    // ────────────────────────────────────────────────────────────────────────────

    private void InitTopbar()
    {
        LblToggle.Text      = DesktopLanguageService.Translate("START");
        LblDisconnect.Text  = DesktopLanguageService.Translate("Disconnect");
        LblStartClient.Text = DesktopLanguageService.Translate("Start Client");
        LblGoClientless.Text = DesktopLanguageService.Translate("Go Clientless");
        LblHideClient.Text  = DesktopLanguageService.Translate("Hide Client");

        if (_lang == "Turkish")
        {
            BtnTr.Classes.Add("active");
            BtnEn.Classes.Remove("active");
        }
        else
        {
            _lang = "English";
            BtnEn.Classes.Add("active");
            BtnTr.Classes.Remove("active");
        }

        DivisionSelect.SelectionChanged += DivisionSelect_SelectionChanged;
        GatewaySelect.SelectionChanged += GatewaySelect_SelectionChanged;
        UpdateThemeIcon();
        LoadBanner();
        ApplyTranslations();
        SyncTopbar();
    }

    private void SyncTopbar()
    {
        if (_state is null) return;

        var running = _state.BotRunning;
        var ch = _state.Character.Trim();
        var waiting = string.IsNullOrEmpty(ch) || ch == "-";
        var connected = _state.AgentConnected || _state.GatewayConnected;

        // ── Status chip: multiple states ──
        StatusChipBorder.Classes.Remove("status-running");
        StatusChipBorder.Classes.Remove("status-stopped");
        StatusChipBorder.Classes.Remove("status-waiting");
        StatusChipBorder.Classes.Remove("status-connected");

        if (running)
        {
            StopStatusPulse();
            StatusPulseDot.IsVisible = false;
            StatusIcon.IsVisible = true;
            StatusChipBorder.Classes.Add("status-running");
            StatusText.Text = DesktopLanguageService.Translate("Running");
            StatusText.Foreground = new SolidColorBrush(Color.Parse("#34D399"));
            StatusIcon.Stroke = new SolidColorBrush(Color.Parse("#34D399"));
            StatusIcon.Data = Geometry.Parse("M5,12 L10,17 L19,8"); // checkmark
        }
        else if (connected && waiting)
        {
            StopStatusPulse();
            StatusPulseDot.IsVisible = false;
            StatusIcon.IsVisible = true;
            StatusChipBorder.Classes.Add("status-waiting");
            StatusText.Text = DesktopLanguageService.Translate("Waiting for Character");
            var waitingColor = _isDark ? Color.Parse("#F59E0B") : Color.Parse("#A66A00");
            StatusText.Foreground = new SolidColorBrush(waitingColor);
            StatusIcon.Stroke = new SolidColorBrush(waitingColor);
            StatusIcon.Data = Geometry.Parse("M12,2 A10,10 0 1,0 12,22 A10,10 0 0,0 12,2 M12,6 L12,12 L16,14"); // clock
        }
        else if (connected)
        {
            StartStatusPulse(_isDark ? Color.Parse("#34D399") : Color.Parse("#0A6A4A"));
            StatusPulseDot.IsVisible = true;
            StatusIcon.IsVisible = false;
            StatusChipBorder.Classes.Add("status-connected");
            StatusText.Text = DesktopLanguageService.Translate("Connected");
            StatusText.Foreground = new SolidColorBrush(_isDark ? Color.Parse("#34D399") : Color.Parse("#0A6A4A"));
        }
        else
        {
            StopStatusPulse();
            StatusPulseDot.IsVisible = false;
            StatusIcon.IsVisible = true;
            StatusChipBorder.Classes.Add("status-stopped");
            StatusText.Text = DesktopLanguageService.Translate("Off");
            var stopColor = _isDark ? Color.Parse("#F26C6C") : Color.Parse("#B7485A");
            StatusText.Foreground = new SolidColorBrush(stopColor);
            StatusIcon.Stroke = new SolidColorBrush(stopColor);
            StatusIcon.Data = Geometry.Parse("M18,6 L6,18 M6,6 L18,18"); // X
        }

        // ── Primary toggle button ──
        if (running)
        {
            BtnToggle.Classes.Remove("go");
            BtnToggle.Classes.Add("halt");
            LblToggle.Text = DesktopLanguageService.Translate("STOP");
            ToggleIcon.Fill = new SolidColorBrush(Color.Parse("#FFE4E4"));
            ToggleIcon.Stroke = null;
            ToggleIcon.Data = Geometry.Parse("M6,6 L18,6 L18,18 L6,18 Z"); // stop square
        }
        else
        {
            BtnToggle.Classes.Remove("halt");
            BtnToggle.Classes.Add("go");
            LblToggle.Text = DesktopLanguageService.Translate("START");
            ToggleIcon.Fill = new SolidColorBrush(Color.Parse("#F0FFFFFF"));
            ToggleIcon.Stroke = null;
            ToggleIcon.Data = Geometry.Parse("M5,3 L19,12 L5,21 Z"); // play triangle
        }

        ProfileLabel.Text = _state.Profile;

        // ── Character pill ──
        CharLabel.Text = waiting ? DesktopLanguageService.Translate("Waiting for Character...") : ch;
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
                Stroke = new SolidColorBrush(_isDark ? Color.Parse("#64F3D0") : Color.Parse("#2E6E5C")),
                StrokeThickness = 1.4, Width = 13, Height = 13, Stretch = Stretch.Uniform
            };
            CharLabel.Foreground = new SolidColorBrush(_isDark ? Color.Parse("#EEF4FF") : Color.Parse("#17385E"));
        }

        // Metrics
        if (_state.HasLiveStats)
        {
            MetricLevel.Text = _state.PlayerLevel.ToString();
            MetricHp.Text    = $"{_state.PlayerHealth:N0} / {_state.PlayerMaxHealth:N0}";
            MetricMp.Text    = $"{_state.PlayerMana:N0} / {_state.PlayerMaxMana:N0}";
            MetricExp.Text   = $"{_state.PlayerExpPercent:F2}%";
            var live = DesktopLanguageService.Translate("Live");
            MetricLevelHint.Text = live;
            MetricHpHint.Text = live;
            MetricMpHint.Text = live;
            MetricExpHint.Text = live;
            UpdateBar(HpBar,  _state.PlayerHealthPercent);
            UpdateBar(MpBar,  _state.PlayerManaPercent);
            UpdateBar(ExpBar, _state.PlayerExpPercent);
            SetMetricEmpty(MetricLevel, false); SetMetricEmpty(MetricHp, false); SetMetricEmpty(MetricMp, false); SetMetricEmpty(MetricExp, false);
        }
        else
        {
            MetricLevel.Text = "—";
            MetricHp.Text = "—";
            MetricMp.Text = "—";
            MetricExp.Text = "—";
            MetricLevelHint.Text = "";
            MetricHpHint.Text = "";
            MetricMpHint.Text = "";
            MetricExpHint.Text = "";
            UpdateBar(HpBar, 0); UpdateBar(MpBar, 0); UpdateBar(ExpBar, 0);
            SetMetricEmpty(MetricLevel, true); SetMetricEmpty(MetricHp, true); SetMetricEmpty(MetricMp, true); SetMetricEmpty(MetricExp, true);
        }

        RecalcBars();
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
        _syncingConnectionSelects = true;

        var divs = new List<SelectOption>();
        foreach (var d in opts.Divisions) divs.Add(new SelectOption(d.Index, d.Name));
        DivisionSelect.Options       = divs;
        DivisionSelect.SelectedValue = opts.DivisionIndex;

        var found = opts.Divisions.Find(d => d.Index == opts.DivisionIndex);
        var srvs  = new List<SelectOption>();
        if (found != null) foreach (var s in found.Servers) srvs.Add(new SelectOption(s.Index, s.Name));
        GatewaySelect.Options       = srvs;
        GatewaySelect.SelectedValue = opts.GatewayIndex;
        GatewaySelect.IsDisabled    = srvs.Count == 0;

        _syncingConnectionSelects = false;
    }

    private async void DivisionSelect_SelectionChanged(object value)
    {
        if (_syncingConnectionSelects || _core == null || _state == null)
            return;
        if (!TryGetSelectIndex(value, out var divisionIndex))
            return;

        var current = _state.ConnectionOptions;
        var updated = await _core.SetConnectionOptionsAsync(
            divisionIndex,
            0,
            current.Mode,
            current.ClientType);

        _state.ConnectionOptions = updated;
        RebuildDivisionSelects(updated);
        await RefreshRuntimeStatusAsync();
    }

    private async void GatewaySelect_SelectionChanged(object value)
    {
        if (_syncingConnectionSelects || _core == null || _state == null)
            return;
        if (!TryGetSelectIndex(value, out var gatewayIndex))
            return;

        var current = _state.ConnectionOptions;
        var updated = await _core.SetConnectionOptionsAsync(
            current.DivisionIndex,
            gatewayIndex,
            current.Mode,
            current.ClientType);

        _state.ConnectionOptions = updated;
        RebuildDivisionSelects(updated);
        await RefreshRuntimeStatusAsync();
    }

    private static bool TryGetSelectIndex(object? value, out int index)
    {
        index = 0;

        if (value is int intValue)
        {
            index = intValue;
            return true;
        }

        if (value is long longValue && longValue >= int.MinValue && longValue <= int.MaxValue)
        {
            index = (int)longValue;
            return true;
        }

        if (value is string text && int.TryParse(text, out var parsed))
        {
            index = parsed;
            return true;
        }

        return false;
    }

    // ——————————————————————————————————————————————————————————————————————————

    private void BindStateEvents()
    {
        _state!.PropertyChanged += (_, pe) =>
        {
            switch (pe.PropertyName)
            {
                case nameof(AppState.BotRunning):
                case nameof(AppState.Profile):
                case nameof(AppState.Character):
                case nameof(AppState.AgentConnected):
                case nameof(AppState.GatewayConnected):
                case nameof(AppState.HasLiveStats):
                case nameof(AppState.PlayerLevel):
                case nameof(AppState.PlayerHealth):
                case nameof(AppState.PlayerMaxHealth):
                case nameof(AppState.PlayerHealthPercent):
                case nameof(AppState.PlayerMana):
                case nameof(AppState.PlayerMaxMana):
                case nameof(AppState.PlayerManaPercent):
                case nameof(AppState.PlayerExpPercent):
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(SyncTopbar);
                    break;
                case nameof(AppState.ConnectionOptions):
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(() => RebuildDivisionSelects(_state.ConnectionOptions));
                    break;
                case nameof(AppState.Plugins):
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(BuildSidebar);
                    break;
            }
        };
    }

    // ——————————————————————————————————————————————————————————————————————————

    private async void BtnToggle_Click(object? s, RoutedEventArgs e)
    {
        RuntimeStatus r;
        if (_state!.BotRunning)
            r = await _core!.StopBotAsync();
        else
            r = await _core!.StartBotAsync();
        _state.ApplyStatus(r);
        await RefreshRuntimeStatusAsync();
    }
    private async void BtnDisconnect_Click(object? s, RoutedEventArgs e)
    {
        var r = await _core!.DisconnectAsync();
        _state!.ApplyStatus(r);
        await RefreshRuntimeStatusAsync(forceConnectionRefresh: true);
    }
    private async void BtnSave_Click(object? s, RoutedEventArgs e) => await _core!.SaveConfigAsync();
    private async void BtnStartClient_Click(object? s, RoutedEventArgs e)
    {
        await _core!.StartClientAsync();
        await RefreshRuntimeStatusAsync(forceConnectionRefresh: true);
    }
    private async void BtnGoClientless_Click(object? s, RoutedEventArgs e)
    {
        await _core!.GoClientlessAsync();
        await RefreshRuntimeStatusAsync(forceConnectionRefresh: true);
    }
    private async void BtnHideClient_Click(object? s, RoutedEventArgs e)
    {
        await _core!.ToggleClientVisibilityAsync();
        await RefreshRuntimeStatusAsync();
    }

    private void OnAboutClick(object? s, RoutedEventArgs e)
    {
        // Placeholder for About dialog
    }

    private async void OnSelectProfileClick(object? s, RoutedEventArgs e)
    {
        try
        {
            var dlg = new ProfileSelectionWindow();
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.Closed += async (_, _) =>
            {
                if (dlg.Applied)
                    await RefreshRuntimeStatusAsync(forceConnectionRefresh: true);
            };
            dlg.Show(this);
        }
        catch (Exception ex)
        {
            _state?.AddLog($"[ERROR] Profile dialog failed: {ex.Message}");
        }
    }

    private async void OnProxyConfigClick(object? s, RoutedEventArgs e)
    {
        try
        {
            var dlg = new ProxyConfigWindow();
            dlg.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            dlg.Closed += async (_, _) =>
            {
                if (dlg.Applied)
                    await RefreshRuntimeStatusAsync(forceConnectionRefresh: true);
            };
            dlg.Show(this);
        }
        catch (Exception ex)
        {
            _state?.AddLog($"[ERROR] Proxy dialog failed: {ex.Message}");
        }
    }

    private void BtnEn_Click(object? s, RoutedEventArgs e)
    {
        _lang = "English"; BtnEn.Classes.Add("active"); BtnTr.Classes.Remove("active");
        PersistDesktopPreferences();
        ApplyTranslations();
    }
    private void BtnTr_Click(object? s, RoutedEventArgs e)
    {
        _lang = "Turkish"; BtnTr.Classes.Add("active"); BtnEn.Classes.Remove("active");
        PersistDesktopPreferences();
        ApplyTranslations();
    }
    private void BtnTheme_Click(object? s, RoutedEventArgs e)
    {
        _isDark = !_isDark;
        var target = _isDark ? ThemeVariant.Dark : ThemeVariant.Light;
        RequestedThemeVariant = target;
        if (Application.Current is { } app)
            app.RequestedThemeVariant = target;
        PersistDesktopPreferences();
        UpdateThemeIcon();
        LoadBanner();
    }

    private void LoadDesktopPreferences()
    {
        var savedTheme = GlobalConfig.Get(DesktopThemeConfigKey, "dark");
        _isDark = !string.Equals(savedTheme, "light", StringComparison.OrdinalIgnoreCase);

        var target = _isDark ? ThemeVariant.Dark : ThemeVariant.Light;
        RequestedThemeVariant = target;
        if (Application.Current is { } app)
            app.RequestedThemeVariant = target;

        var savedLanguage = GlobalConfig.Get(DesktopLanguageConfigKey, "English");
        _lang = string.Equals(savedLanguage, "Turkish", StringComparison.OrdinalIgnoreCase)
            ? "Turkish"
            : "English";
    }

    private void PersistDesktopPreferences()
    {
        GlobalConfig.Set(DesktopThemeConfigKey, _isDark ? "dark" : "light");
        GlobalConfig.Set(DesktopLanguageConfigKey, _lang);
    }

    private void UpdateThemeIcon()
    {
        IBrush iconBrush = new SolidColorBrush(_isDark ? Color.Parse("#5C7899") : Color.Parse("#5D7A9B"));
        if (_isDark)
        {
            ThemeIcon.Stroke = null;
            ThemeIcon.Fill = iconBrush;
            ThemeIcon.Data = Geometry.Parse("M9.37,5.51 C9.19,6.15 9.1,6.82 9.1,7.5 C9.1,11.08 12.02,14 15.6,14 C16.28,14 16.95,13.91 17.59,13.73 C16.67,16.42 14.11,18.3 11.1,18.3 C7.3,18.3 4.2,15.2 4.2,11.4 C4.2,8.39 6.08,5.83 8.77,4.91 Z");
        }
        else
        {
            ThemeIcon.Fill = null;
            ThemeIcon.Stroke = iconBrush;
            ThemeIcon.Data = Geometry.Parse("M12,2 L12,4 M12,20 L12,22 M4.22,4.22 L5.64,5.64 M18.36,18.36 L19.78,19.78 M2,12 L4,12 M20,12 L22,12 M4.22,19.78 L5.64,18.36 M18.36,5.64 L19.78,4.22 M12,17 A5,5 0 1,0 12,7 A5,5 0 0,0 12,17 Z");
        }
    }


    private void ApplyTranslations()
    {
        bool tr = _lang == "Turkish";
        DesktopLanguageService.SetLanguage(_lang);
        GlobalConfig.Set("UBot.Language", tr ? "tr_TR" : "en_US");
        Kernel.Language = tr ? "tr_TR" : "en_US";
        LblToggle.Text       = _state!.BotRunning
            ? DesktopLanguageService.Translate("STOP")
            : DesktopLanguageService.Translate("START");
        LblDisconnect.Text   = DesktopLanguageService.Translate("Disconnect");
        LblStartClient.Text  = DesktopLanguageService.Translate("Start Client");
        LblGoClientless.Text = DesktopLanguageService.Translate("Go Clientless");
        LblHideClient.Text   = DesktopLanguageService.Translate("Hide Client");
        BuildSidebar();
        BuildMenu();
        if (!string.IsNullOrWhiteSpace(_activeId))
            Navigate(_activeId);
        DesktopLanguageService.ApplyToControl(this, _lang);
        SyncTopbar();
    }

    // â”€â”€â”€ Banner â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void LoadBanner()
    {
        try
        {
            var bannerFile = _isDark ? "ubot_banner_night.png" : "ubot_banner_day.png";
            var bmp = new Bitmap(AssetLoader.Open(new Uri($"avares://UBot.Avalonia/Assets/{bannerFile}")));
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

    private void SetMetricEmpty(Control c, bool empty)
    {
        if (c.Parent is Grid g && g.Parent is Border b)
        {
            b.Classes.Remove("empty");
            if (empty) b.Classes.Add("empty");
        }
    }

    private void StartStatusPulse(Color color)
    {
        StatusPulseDot.Fill = new SolidColorBrush(color);
        StopStatusPulse();

        _statusPulseCts = new CancellationTokenSource();
        var animation = new Animation
        {
            Duration = TimeSpan.FromSeconds(1.6),
            IterationCount = IterationCount.Infinite,
            Easing = new SineEaseInOut(),
            Children =
            {
                new KeyFrame
                {
                    Cue = new Cue(0d),
                    Setters = { new Setter(Visual.OpacityProperty, 1d) }
                },
                new KeyFrame
                {
                    Cue = new Cue(0.5d),
                    Setters = { new Setter(Visual.OpacityProperty, 0.3d) }
                },
                new KeyFrame
                {
                    Cue = new Cue(1d),
                    Setters = { new Setter(Visual.OpacityProperty, 1d) }
                }
            }
        };

        _ = animation.RunAsync(StatusPulseDot, _statusPulseCts.Token);
    }

    private void StopStatusPulse()
    {
        if (_statusPulseCts == null)
            return;

        _statusPulseCts.Cancel();
        _statusPulseCts.Dispose();
        _statusPulseCts = null;
        StatusPulseDot.Opacity = 1;
    }
}


