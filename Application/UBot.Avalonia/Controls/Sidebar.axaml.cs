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

private static Dictionary<string, string> IconGeo => IconPaths.Geometries;

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
        if (IconGeo.TryGetValue(key, out var pathData))
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

