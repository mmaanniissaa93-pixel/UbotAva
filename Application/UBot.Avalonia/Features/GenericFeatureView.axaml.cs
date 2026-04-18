using Avalonia.Controls;
using System.Collections;
using System.Collections.Generic;

namespace UBot.Avalonia.Features;

/// <summary>
/// Reusable view for features that are primarily a table + optional action buttons.
/// Used by: Statistics, Quest, ServerInfo, PacketInspector, AutoDungeon.
/// </summary>
public partial class GenericFeatureView : UserControl
{
    public GenericFeatureView()
    {
        InitializeComponent();
    }

    public void Configure(
        string sectionTitle,
        string col1Header,
        string col2Header,
        IEnumerable rows,
        IEnumerable<(string Label, System.Action OnClick, bool IsPrimary)>? footerButtons = null,
        IEnumerable<(string Label, System.Action OnClick)>? actionButtons = null)
    {
        SectionTitle.Text    = sectionTitle;
        if (MainGrid.Columns.Count > 0 && MainGrid.Columns[0] is DataGridTextColumn c1) c1.Header = col1Header;
        if (MainGrid.Columns.Count > 1 && MainGrid.Columns[1] is DataGridTextColumn c2) c2.Header = col2Header;
        MainGrid.ItemsSource = rows;

        if (footerButtons != null)
        {
            FooterBar.IsVisible = true;
            foreach (var (label, onClick, isPrimary) in footerButtons)
            {
                var btn = new Button { Content = label };
                btn.Classes.Add("legacy-btn");
                if (isPrimary) btn.Classes.Add("primary");
                btn.Click += (_, _) => onClick();
                btn.Margin = new global::Avalonia.Thickness(0, 0, 6, 0);
                FooterBar.Children.Add(btn);
            }
        }

        if (actionButtons != null)
        {
            ActionBar.IsVisible = true;
            foreach (var (label, onClick) in actionButtons)
            {
                var btn = new Button { Content = label };
                btn.Classes.Add("action-btn");
                btn.Click += (_, _) => onClick();
                btn.Margin = new global::Avalonia.Thickness(0, 0, 6, 8);
                ActionBar.Children.Add(btn);
            }
        }
    }

    public void UpdateRows(IEnumerable rows)
    {
        MainGrid.ItemsSource = rows;
    }
}

