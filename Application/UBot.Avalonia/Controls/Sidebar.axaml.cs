using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using global::Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;

namespace UBot.Avalonia.Controls;

/// <summary>
/// Represents one entry in the sidebar nav.
/// Mirrors SidebarItem from Sidebar.tsx.
/// </summary>
public record SidebarItem(string Id, string Label, string? IconKey = null, bool Enabled = true);

/// <summary>
/// Mirrors Sidebar.tsx â€” grouped navigation panel with brand banner.
/// </summary>
public partial class Sidebar : UserControl
{
    // â”€â”€â”€ Avalonia Properties â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    public static readonly StyledProperty<string> ActiveIdProperty =
        AvaloniaProperty.Register<Sidebar, string>(nameof(ActiveId), string.Empty);

    public static readonly StyledProperty<IList<SidebarItem>> ItemsProperty =
        AvaloniaProperty.Register<Sidebar, IList<SidebarItem>>(
            nameof(Items), new List<SidebarItem>());

    public static readonly StyledProperty<bool> IsDarkThemeProperty =
        AvaloniaProperty.Register<Sidebar, bool>(nameof(IsDarkTheme), true);

    // Group label strings (localisation)
    public static readonly StyledProperty<string> GroupCoreLabelProperty =
        AvaloniaProperty.Register<Sidebar, string>(nameof(GroupCoreLabel), "Core");
    public static readonly StyledProperty<string> GroupAutoLabelProperty =
        AvaloniaProperty.Register<Sidebar, string>(nameof(GroupAutoLabel), "Automation");
    public static readonly StyledProperty<string> GroupDataLabelProperty =
        AvaloniaProperty.Register<Sidebar, string>(nameof(GroupDataLabel), "Data");
    public static readonly StyledProperty<string> GroupOtherLabelProperty =
        AvaloniaProperty.Register<Sidebar, string>(nameof(GroupOtherLabel), "Other");

    public string ActiveId
    {
        get => GetValue(ActiveIdProperty);
        set => SetValue(ActiveIdProperty, value);
    }

    public IList<SidebarItem> Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public bool IsDarkTheme
    {
        get => GetValue(IsDarkThemeProperty);
        set => SetValue(IsDarkThemeProperty, value);
    }

    public string GroupCoreLabel  { get => GetValue(GroupCoreLabelProperty);  set => SetValue(GroupCoreLabelProperty, value); }
    public string GroupAutoLabel  { get => GetValue(GroupAutoLabelProperty);  set => SetValue(GroupAutoLabelProperty, value); }
    public string GroupDataLabel  { get => GetValue(GroupDataLabelProperty);  set => SetValue(GroupDataLabelProperty, value); }
    public string GroupOtherLabel { get => GetValue(GroupOtherLabelProperty); set => SetValue(GroupOtherLabelProperty, value); }

    /// <summary>Fires when the user clicks a nav item. Passes the item Id.</summary>
    public event Action<string>? ItemSelected;

    private readonly Dictionary<string, Button> _buttons = new();

    // â”€â”€â”€ Groups â€” same logic as Sidebar.tsx â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private static readonly HashSet<string> CoreKeys  = new() { "general","skills","protection","party","training" };
    private static readonly HashSet<string> AutoKeys  = new() { "alchemy","trade","lure","quest","quests","autodungeon","targetassist" };
    private static readonly HashSet<string> DataKeys  = new() { "inventory","items","map","stats","statistics","chat","log","command","server","packet" };

    // Minimal icon paths (Avalonia Path geometries).
    // Replace with your preferred icon set / Avalonia.Controls.Shapes as needed.
    private static readonly Dictionary<string, string> IconPaths = new()
    {
        ["general"]      = "M3,12 L12,3 L21,12 L21,21 L15,21 L15,15 L9,15 L9,21 L3,21 Z",
        ["skills"]       = "M6,2 L18,2 L20,8 L12,22 L4,8 Z",
        ["protection"]   = "M12,2 L20,6 L20,12 C20,17 16,21 12,22 C8,21 4,17 4,12 L4,6 Z",
        ["party"]        = "M17,21v-2a4,4 0 0,0-4-4H5a4,4 0 0,0-4,4v2 M23,21v-2a4,4 0 0,0-3-3.87 M16,3.13a4,4 0 0,1 0,7.75 M9,11a4,4 0 1,0 0-8 4,4 0 0,0 0,8z",
        ["inventory"]    = "M20,7 L4,7 L4,19 A2,2 0 0,0 6,21 L18,21 A2,2 0 0,0 20,19 Z M16,7 L16,5 A2,2 0 0,0 14,3 L10,3 A2,2 0 0,0 8,5 L8,7",
        ["items"]        = "M9,5 L9,3 L15,3 L15,5 M4,5 L20,5 L19,19 A2,2 0 0,1 17,21 L7,21 A2,2 0 0,1 5,19 Z M10,12 L14,12 M12,10 L12,14",
        ["map"]          = "M3,7 L9,4 L15,7 L21,4 L21,17 L15,20 L9,17 L3,20 Z M9,4 L9,17 M15,7 L15,20",
        ["stats"]        = "M18,20 L18,10 M12,20 L12,4 M6,20 L6,14",
        ["training"]     = "M12,2 L15.09,8.26 L22,9.27 L17,14.14 L18.18,21.02 L12,17.77 L5.82,21.02 L7,14.14 L2,9.27 L8.91,8.26 Z",
        ["alchemy"]      = "M9,3 L9,11 L4,19 A1,1 0 0,0 5,21 L19,21 A1,1 0 0,0 20,19 L15,11 L15,3 M6,3 L18,3",
        ["trade"]        = "M17,2 L21,6 L17,10 M3,6 L21,6 M7,22 L3,18 L7,14 M21,18 L3,18",
        ["lure"]         = "M12,2 C7,2 2,7 2,12 A2,2 0 0,0 4,14 L8,14 A4,4 0 0,0 16,14 L20,14 A2,2 0 0,0 22,12 C22,7 17,2 12,2Z",
        ["quest"]        = "M9,5 L4,5 L4,19 L20,19 L20,5 L15,5 M9,5 A3,3 0 0,1 15,5 M9,12 L15,12 M9,16 L13,16",
        ["chat"]         = "M21,15 A2,2 0 0,1 19,17 L7,17 L3,21 L3,5 A2,2 0 0,1 5,3 L19,3 A2,2 0 0,1 21,5 Z",
        ["log"]          = "M3,3 L21,3 L21,5 L3,5 Z M3,9 L21,9 L21,11 L3,11 Z M3,15 L21,15 L21,17 L3,17 Z",
        ["command"]      = "M8,3 L12,8 L8,13 M13,13 L18,13",
        ["server"]       = "M22,12 A10,10 0 1,1 2,12 A10,10 0 0,1 22,12 Z M2,12 L22,12 M12,2 C9,6 7,9 7,12 C7,15 9,18 12,22 C15,18 17,15 17,12 C17,9 15,6 12,2 Z",
        ["packet"]       = "M1,6 L1,22 L5,22 M1,6 L12,2 L23,6 M23,6 L23,22 L19,22 M5,22 L5,11 L12,8 L19,11 L19,22 M5,22 L19,22",
        ["autodungeon"]  = "M22,12 A10,10 0 1,1 2,12 A10,10 0 0,1 22,12 Z M12,6 L12,12 L16,14",
        ["targetassist"] = "M12,2 A10,10 0 1,0 12,22 A10,10 0 0,0 12,2 Z M12,7 A5,5 0 1,0 12,17 A5,5 0 0,0 12,7 Z M12,11 A1,1 0 1,0 12,13 A1,1 0 0,0 12,11 Z M12,2 L12,5 M12,19 L12,22 M2,12 L5,12 M19,12 L22,12",
    };

    public Sidebar()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == ItemsProperty)
            BuildNav();

        if (e.Property == ActiveIdProperty)
            RefreshActive();

        if (e.Property == IsDarkThemeProperty)
            UpdateBanner();
    }

    // â”€â”€â”€ Banner â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    /// <summary>
    /// Set banner bitmaps from your embedding (Assets/ubot_banner_day.png etc.).
    /// Call this after setting IsDarkTheme.
    /// </summary>
    public void SetBannerImages(IImage dark, IImage light)
    {
        _darkBanner = dark;
        _lightBanner = light;
        UpdateBanner();
    }

    private IImage? _darkBanner;
    private IImage? _lightBanner;

    private void UpdateBanner()
    {
        BannerImage.Source = IsDarkTheme ? _darkBanner : _lightBanner;
    }

    // â”€â”€â”€ Nav building â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

    private void BuildNav()
    {
        NavPanel.Children.Clear();
        _buttons.Clear();

        var coreItems  = new List<SidebarItem>();
        var autoItems  = new List<SidebarItem>();
        var dataItems  = new List<SidebarItem>();
        var otherItems = new List<SidebarItem>();

        foreach (var item in Items)
        {
            var key = NormalizeKey(item.IconKey ?? item.Id);
            if (CoreKeys.Contains(key))       coreItems.Add(item);
            else if (AutoKeys.Contains(key))  autoItems.Add(item);
            else if (DataKeys.Contains(key))  dataItems.Add(item);
            else                              otherItems.Add(item);
        }

        AddGroup(GroupCoreLabel,  coreItems,  isFirst: true);
        AddGroup(GroupAutoLabel,  autoItems);
        AddGroup(GroupDataLabel,  dataItems);
        AddGroup(GroupOtherLabel, otherItems);

        RefreshActive();
    }

    private void AddGroup(string groupLabel, List<SidebarItem> items, bool isFirst = false)
    {
        if (items.Count == 0) return;

        var label = new TextBlock
        {
            Text = groupLabel,
            Margin = isFirst
                ? new Thickness(12, 0, 12, 4)
                : new Thickness(12, 10, 12, 4)
        };
        label.Classes.Add("sidebar-group-label");
        NavPanel.Children.Add(label);

        foreach (var item in items)
            NavPanel.Children.Add(CreateItemButton(item));
    }

    private Button CreateItemButton(SidebarItem item)
    {
        var key = NormalizeKey(item.IconKey ?? item.Id);

        // Build inner StackPanel: icon + label
        var stack = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center
        };

        // Icon
        if (IconPaths.TryGetValue(key, out var pathData))
        {
            stack.Children.Add(new global::Avalonia.Controls.Shapes.Path
            {
                Data = Geometry.Parse(pathData),
                Stroke = new SolidColorBrush(Color.Parse("#9FB4D4")),
                StrokeThickness = 1.4,
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform,
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        // Label
        stack.Children.Add(new TextBlock
        {
            Text = item.Label,
            VerticalAlignment = VerticalAlignment.Center
        });

        // Active indicator bar (left edge) â€” shown via code
        var btn = new Button
        {
            Content = stack,
            Tag = item.Id,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        btn.Classes.Add("sidebar-item");

        btn.Click += (_, _) =>
        {
            ActiveId = item.Id;
            ItemSelected?.Invoke(item.Id);
        };

        _buttons[item.Id] = btn;
        return btn;
    }

    private void RefreshActive()
    {
        foreach (var (id, btn) in _buttons)
        {
            if (id == ActiveId)
            {
                if (!btn.Classes.Contains("active"))
                    btn.Classes.Add("active");
            }
            else
            {
                btn.Classes.Remove("active");
            }
        }
    }

    private static string NormalizeKey(string input)
        => input.ToLowerInvariant()
                .Replace("ubot.", string.Empty)
                .Replace(".", string.Empty)
                .Replace("-", string.Empty)
                .Replace("_", string.Empty);
}

