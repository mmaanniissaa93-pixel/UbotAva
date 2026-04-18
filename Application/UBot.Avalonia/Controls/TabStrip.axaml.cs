using Avalonia;
using Avalonia.Controls;
using System.Collections.Generic;
using System;

namespace UBot.Avalonia.Controls;

/// <summary>
/// Mirrors TabStrip.tsx — horizontal list of tab buttons.
/// </summary>
public partial class TabStrip : UserControl
{
    public static readonly StyledProperty<string> ActiveTabIdProperty =
        AvaloniaProperty.Register<TabStrip, string>(nameof(ActiveTabId), string.Empty);

    public string ActiveTabId
    {
        get => GetValue(ActiveTabIdProperty);
        set => SetValue(ActiveTabIdProperty, value);
    }

    /// <summary>Raised when the user selects a tab.</summary>
    public event Action<string>? TabChanged;

    private readonly Dictionary<string, Button> _buttons = new();

    public TabStrip()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Populate the strip.  Call whenever the tab list changes.
    /// </summary>
    public void SetTabs(IEnumerable<(string Id, string Label)> tabs)
    {
        TabsPanel.Children.Clear();
        _buttons.Clear();

        foreach (var (id, label) in tabs)
        {
            var btn = new Button
            {
                Content = label,
                Tag = id
            };
            btn.Classes.Add("tab-btn");

            if (id == ActiveTabId)
                btn.Classes.Add("active");

            btn.Click += (_, _) => SelectTab(id);
            _buttons[id] = btn;
            TabsPanel.Children.Add(btn);
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == ActiveTabIdProperty)
            RefreshActiveState();
    }

    private void SelectTab(string id)
    {
        ActiveTabId = id;
        TabChanged?.Invoke(id);
    }

    private void RefreshActiveState()
    {
        foreach (var (id, btn) in _buttons)
        {
            if (id == ActiveTabId)
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
}
