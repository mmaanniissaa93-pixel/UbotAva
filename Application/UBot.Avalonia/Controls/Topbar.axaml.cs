using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace UBot.Avalonia.Controls;

/// <summary>Division info for the division selector.</summary>
public record ConnectionDivision(int Index, string Name, IReadOnlyList<ServerEntry> Servers);

/// <summary>Server (gateway) entry inside a division.</summary>
public record ServerEntry(int Index, string Name);

/// <summary>
/// Mirrors Topbar.tsx — running state, profile, character,
/// language/theme toggles, division/server selects, action buttons.
/// </summary>
public partial class Topbar : UserControl
{
    // ─── Avalonia Properties ────────────────────────────────────────────────

    public static readonly StyledProperty<bool> IsRunningProperty =
        AvaloniaProperty.Register<Topbar, bool>(nameof(IsRunning), false);
    public static readonly StyledProperty<string> ProfileProperty =
        AvaloniaProperty.Register<Topbar, string>(nameof(Profile), string.Empty);
    public static readonly StyledProperty<string> CharacterProperty =
        AvaloniaProperty.Register<Topbar, string>(nameof(Character), string.Empty);
    public static readonly StyledProperty<bool> ShowQuickActionsProperty =
        AvaloniaProperty.Register<Topbar, bool>(nameof(ShowQuickActions), false);
    public static readonly StyledProperty<bool> StartClientDisabledProperty =
        AvaloniaProperty.Register<Topbar, bool>(nameof(StartClientDisabled), false);

    // Labels (localised)
    public static readonly StyledProperty<string> LblStartProperty      = AvaloniaProperty.Register<Topbar, string>(nameof(LblStart), "Start");
    public static readonly StyledProperty<string> LblStopProperty       = AvaloniaProperty.Register<Topbar, string>(nameof(LblStop), "Stop");
    public static readonly StyledProperty<string> LblDisconnectProperty = AvaloniaProperty.Register<Topbar, string>(nameof(LblDisconnect), "Disconnect");
    public static readonly StyledProperty<string> LblOnProperty         = AvaloniaProperty.Register<Topbar, string>(nameof(LblOn), "ON");
    public static readonly StyledProperty<string> LblOffProperty        = AvaloniaProperty.Register<Topbar, string>(nameof(LblOff), "OFF");
    public static readonly StyledProperty<string> LblWaitCharProperty   = AvaloniaProperty.Register<Topbar, string>(nameof(LblWaitChar), "Waiting for character...");
    public static readonly StyledProperty<string> LblStartClientProperty    = AvaloniaProperty.Register<Topbar, string>(nameof(LblStartClient), "Start Client");
    public static readonly StyledProperty<string> LblGoClientlessProperty   = AvaloniaProperty.Register<Topbar, string>(nameof(LblGoClientless), "Go Clientless");
    public static readonly StyledProperty<string> LblToggleClientProperty   = AvaloniaProperty.Register<Topbar, string>(nameof(LblToggleClient), "Hide Client");

    // Theme / lang
    public static readonly StyledProperty<bool> IsDarkThemeProperty =
        AvaloniaProperty.Register<Topbar, bool>(nameof(IsDarkTheme), true);
    public static readonly StyledProperty<string> CurrentLanguageProperty =
        AvaloniaProperty.Register<Topbar, string>(nameof(CurrentLanguage), "English");

    // ─── CLR wrappers ───────────────────────────────────────────────────────

    public bool IsRunning            { get => GetValue(IsRunningProperty);           set => SetValue(IsRunningProperty, value); }
    public string Profile            { get => GetValue(ProfileProperty);             set => SetValue(ProfileProperty, value); }
    public string Character          { get => GetValue(CharacterProperty);           set => SetValue(CharacterProperty, value); }
    public bool ShowQuickActions     { get => GetValue(ShowQuickActionsProperty);    set => SetValue(ShowQuickActionsProperty, value); }
    public bool StartClientDisabled  { get => GetValue(StartClientDisabledProperty); set => SetValue(StartClientDisabledProperty, value); }
    public bool IsDarkTheme          { get => GetValue(IsDarkThemeProperty);         set => SetValue(IsDarkThemeProperty, value); }
    public string CurrentLanguage    { get => GetValue(CurrentLanguageProperty);     set => SetValue(CurrentLanguageProperty, value); }
    public string LblStart       { get => GetValue(LblStartProperty);      set => SetValue(LblStartProperty, value); }
    public string LblStop        { get => GetValue(LblStopProperty);       set => SetValue(LblStopProperty, value); }
    public string LblDisconnect  { get => GetValue(LblDisconnectProperty); set => SetValue(LblDisconnectProperty, value); }
    public string LblOn          { get => GetValue(LblOnProperty);         set => SetValue(LblOnProperty, value); }
    public string LblOff         { get => GetValue(LblOffProperty);        set => SetValue(LblOffProperty, value); }
    public string LblWaitChar    { get => GetValue(LblWaitCharProperty);   set => SetValue(LblWaitCharProperty, value); }
    public string LblStartClient    { get => GetValue(LblStartClientProperty);   set => SetValue(LblStartClientProperty, value); }
    public string LblGoClientless   { get => GetValue(LblGoClientlessProperty);  set => SetValue(LblGoClientlessProperty, value); }
    public string LblToggleClient   { get => GetValue(LblToggleClientProperty);  set => SetValue(LblToggleClientProperty, value); }

    // ─── Events ─────────────────────────────────────────────────────────────

    public event Action? Start;
    public event Action? Stop;
    public event Action? Disconnect;
    public event Action? Save;
    public event Action? StartClient;
    public event Action? GoClientless;
    public event Action? ToggleClientVisibility;
    public event Action<string>? LanguageChanged;  // "English" | "Turkish"
    public event Action<bool>? ThemeToggled;        // true = dark
    public event Action<int>? DivisionChanged;
    public event Action<int>? GatewayChanged;

    // ─── Constructor ────────────────────────────────────────────────────────

    public Topbar()
    {
        InitializeComponent();

        DivisionSelect.SelectionChanged += v => DivisionChanged?.Invoke((int)v);
        GatewaySelect.SelectionChanged  += v => GatewayChanged?.Invoke((int)v);
    }

    // ─── Division / Server population ───────────────────────────────────────

    public void SetDivisions(IList<ConnectionDivision> divisions, int selectedDivisionIndex, int selectedGatewayIndex)
    {
        var divOpts = new List<SelectOption>();
        foreach (var d in divisions)
            divOpts.Add(new SelectOption(d.Index, d.Name));

        DivisionSelect.Options = divOpts;
        DivisionSelect.SelectedValue = selectedDivisionIndex;

        UpdateGateways(divisions, selectedDivisionIndex, selectedGatewayIndex);
    }

    private void UpdateGateways(IList<ConnectionDivision> divisions, int divIndex, int gwIndex)
    {
        ServerEntry[] servers = Array.Empty<ServerEntry>();
        foreach (var d in divisions)
            if (d.Index == divIndex) { servers = (ServerEntry[])d.Servers; break; }

        var gwOpts = new List<SelectOption>();
        foreach (var s in servers)
            gwOpts.Add(new SelectOption(s.Index, s.Name));

        GatewaySelect.Options = gwOpts;
        GatewaySelect.SelectedValue = gwIndex;
        GatewaySelect.IsDisabled = gwOpts.Count == 0;
    }

    // ─── Property change handler ─────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == IsRunningProperty)        UpdateRunningState();
        if (e.Property == ProfileProperty)          ProfileLabel.Text = Profile;
        if (e.Property == CharacterProperty)        UpdateCharacter();
        if (e.Property == ShowQuickActionsProperty) QuickActionsPanel.IsVisible = ShowQuickActions;
        if (e.Property == LblStartProperty)         StartLabel.Text = LblStart;
        if (e.Property == LblStopProperty)          StopLabel.Text = LblStop;
        if (e.Property == LblDisconnectProperty)    DisconnectLabel.Text = LblDisconnect;
        if (e.Property == LblStartClientProperty)   StartClientLabel.Text = LblStartClient;
        if (e.Property == LblGoClientlessProperty)  GoClientlessLabel.Text = LblGoClientless;
        if (e.Property == LblToggleClientProperty)  ToggleClientLabel.Text = LblToggleClient;
        if (e.Property == CurrentLanguageProperty)  UpdateLangButtons();
        if (e.Property == StartClientDisabledProperty) BtnStartClient.IsEnabled = !StartClientDisabled;

        if (e.Property == IsDarkThemeProperty)
        {
            // Moon path (dark mode icon shown when light theme is active — clicking switches to dark)
            // Sun path (shown when dark theme is active — clicking switches to light)
            ThemeIcon.Data = IsDarkTheme
                ? Geometry.Parse("M12,3 C9.1,3 6.4,4.5 4.8,7 C3.2,9.5 3.2,12.5 4.8,15 C6.4,17.5 9.1,19 12,19 C14.1,19 16.1,18.2 17.7,16.8 C16.1,20.3 12.6,22.5 8.8,22 C3.9,21.3 0.5,16.8 1.2,11.9 C1.9,7 6.4,3.6 11.3,4.3 C11.5,3.1 11.8,3 12,3 Z")
                : Geometry.Parse("M12,2 L12,4 M12,20 L12,22 M4.22,4.22 L5.64,5.64 M18.36,18.36 L19.78,19.78 M2,12 L4,12 M20,12 L22,12 M4.22,19.78 L5.64,18.36 M18.36,5.64 L19.78,4.22 M12,17 A5,5 0 1,0 12,7 A5,5 0 0,0 12,17 Z");
        }
    }

    private void UpdateRunningState()
    {
        StatusLabel.Text = IsRunning ? LblOn : LblOff;

        var chipClasses = StatusChip.Classes;
        if (IsRunning)
        {
            chipClasses.Remove("stopped");
            chipClasses.Add("running");
            StatusDot.Fill  = new SolidColorBrush(Color.Parse("#38BDF8"));
            StatusLabel.Foreground = new SolidColorBrush(Color.Parse("#38BDF8"));
        }
        else
        {
            chipClasses.Remove("running");
            chipClasses.Add("stopped");
            StatusDot.Fill  = new SolidColorBrush(Color.Parse("#F87171"));
            StatusLabel.Foreground = new SolidColorBrush(Color.Parse("#F87171"));
        }

        BtnStart.IsEnabled = !IsRunning;
        BtnStop.IsEnabled  = IsRunning;
    }

    private void UpdateCharacter()
    {
        var c = Character.Trim();
        var isWaiting = string.IsNullOrEmpty(c) || c == "-";

        CharacterLabel.Text = isWaiting ? LblWaitChar : c;

        var pillClasses = CharacterPill.Classes;
        if (isWaiting)
        {
            pillClasses.Add("waiting");
            // Animated dot
            CharacterIcon.Content = new Ellipse
            {
                Width = 8, Height = 8,
                Fill = new SolidColorBrush(Color.Parse("#FBBF24"))
            };
            CharacterLabel.Foreground = new SolidColorBrush(Color.Parse("#FFD98B"));
        }
        else
        {
            pillClasses.Remove("waiting");
            CharacterIcon.Content = new Path
            {
                Data = Geometry.Parse("M12,2 L20,6 L20,12 C20,17 16,21 12,22 C8,21 4,17 4,12 L4,6 Z"),
                Stroke = new SolidColorBrush(Color.Parse("#64F3D0")),
                StrokeThickness = 1.5,
                Width = 14, Height = 14,
                Stretch = Stretch.Uniform
            };
            CharacterLabel.Foreground = new SolidColorBrush(Color.Parse("#ECF4FF"));
        }
    }

    private void UpdateLangButtons()
    {
        if (CurrentLanguage == "English")
        {
            if (!BtnLangEn.Classes.Contains("active")) BtnLangEn.Classes.Add("active");
            BtnLangTr.Classes.Remove("active");
        }
        else
        {
            if (!BtnLangTr.Classes.Contains("active")) BtnLangTr.Classes.Add("active");
            BtnLangEn.Classes.Remove("active");
        }
    }

    // ─── Button click handlers ───────────────────────────────────────────────

    private void BtnStart_Click(object? s, RoutedEventArgs e)         => Start?.Invoke();
    private void BtnStop_Click(object? s, RoutedEventArgs e)          => Stop?.Invoke();
    private void BtnDisconnect_Click(object? s, RoutedEventArgs e)    => Disconnect?.Invoke();
    private void BtnSave_Click(object? s, RoutedEventArgs e)          => Save?.Invoke();
    private void BtnStartClient_Click(object? s, RoutedEventArgs e)   => StartClient?.Invoke();
    private void BtnGoClientless_Click(object? s, RoutedEventArgs e)  => GoClientless?.Invoke();
    private void BtnToggleClient_Click(object? s, RoutedEventArgs e)  => ToggleClientVisibility?.Invoke();

    private void BtnLangEn_Click(object? s, RoutedEventArgs e)
    {
        CurrentLanguage = "English";
        LanguageChanged?.Invoke("English");
    }

    private void BtnLangTr_Click(object? s, RoutedEventArgs e)
    {
        CurrentLanguage = "Turkish";
        LanguageChanged?.Invoke("Turkish");
    }

    private void BtnTheme_Click(object? s, RoutedEventArgs e)
    {
        IsDarkTheme = !IsDarkTheme;
        ThemeToggled?.Invoke(IsDarkTheme);
    }
}
