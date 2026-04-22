using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace UBot.Avalonia.Controls;

/// <summary>
/// One item in the select list.
/// Mirrors SelectOption from CustomSelect.tsx.
/// </summary>
public record SelectOption(object Index, string Name, Control? Icon = null, string? Hint = null);

/// <summary>
/// Mirrors CustomSelect.tsx — a portal-style dropdown with icon + label.
/// </summary>
public partial class CustomSelect : UserControl
{
    // ─── Avalonia Properties ────────────────────────────────────────────────

    public static readonly StyledProperty<IList<SelectOption>> OptionsProperty =
        AvaloniaProperty.Register<CustomSelect, IList<SelectOption>>(
            nameof(Options), new List<SelectOption>());

    public static readonly StyledProperty<object?> SelectedValueProperty =
        AvaloniaProperty.Register<CustomSelect, object?>(nameof(SelectedValue), null);

    public static readonly StyledProperty<bool> IsDisabledProperty =
        AvaloniaProperty.Register<CustomSelect, bool>(nameof(IsDisabled), false);

    public static readonly StyledProperty<string> PlaceholderProperty =
        AvaloniaProperty.Register<CustomSelect, string>(nameof(Placeholder), "Select...");

    public IList<SelectOption> Options
    {
        get => GetValue(OptionsProperty);
        set => SetValue(OptionsProperty, value);
    }

    public object? SelectedValue
    {
        get => GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }

    public bool IsDisabled
    {
        get => GetValue(IsDisabledProperty);
        set => SetValue(IsDisabledProperty, value);
    }

    public string Placeholder
    {
        get => GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    /// <summary>Fires when the user picks an item. Passes the item's Index.</summary>
    public event Action<object>? SelectionChanged;

    private bool _isOpen;

    public CustomSelect()
    {
        InitializeComponent();
    }

    // ─── Property changes ────────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == OptionsProperty || e.Property == SelectedValueProperty)
            UpdateTriggerLabel();

        if (e.Property == IsDisabledProperty)
        {
            Trigger.Opacity = IsDisabled ? 0.75 : 1.0;
            Trigger.IsHitTestVisible = !IsDisabled;
        }
    }

    // ─── Trigger interaction ─────────────────────────────────────────────────

    private void Trigger_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (IsDisabled) return;
        ToggleDropdown();
    }

    private void ToggleDropdown()
    {
        if (_isOpen) CloseDropdown();
        else OpenDropdown();
    }

    private void OpenDropdown()
    {
        _isOpen = true;
        Trigger.Classes.Add("active");
        ChevronIcon.RenderTransform = new RotateTransform(180);

        RebuildMenu();
        DropdownPopup.IsOpen = true;
    }

    private void CloseDropdown()
    {
        _isOpen = false;
        Trigger.Classes.Remove("active");
        ChevronIcon.RenderTransform = null;
        DropdownPopup.IsOpen = false;
    }

    private void Popup_Closed(object? sender, EventArgs e)
    {
        _isOpen = false;
        Trigger.Classes.Remove("active");
        ChevronIcon.RenderTransform = null;
    }

    // ─── Menu population ─────────────────────────────────────────────────────

    private void RebuildMenu()
    {
        MenuPanel.Children.Clear();

        if (Options.Count == 0)
        {
            MenuPanel.Children.Add(new TextBlock
            {
                Text = "No options available",
                FontSize = 11,
                Foreground = GetHintBrush(),
                Margin = new Thickness(9, 7)
            });
            return;
        }

        foreach (var opt in Options)
        {
            var isSelected = Equals(opt.Index, SelectedValue);

            // Row border
            var row = new Border();
            row.Classes.Add("select-item");
            if (isSelected) row.Classes.Add("selected");

            // Inner grid: icon + label + hint + checkmark
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            grid.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Auto));

            var leftStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 7,
                VerticalAlignment = VerticalAlignment.Center
            };

            if (opt.Icon != null)
            {
                leftStack.Children.Add(new ContentControl
                {
                    Content = opt.Icon,
                    Width = 16,
                    Height = 16,
                    VerticalAlignment = VerticalAlignment.Center
                });
            }

            leftStack.Children.Add(new TextBlock
            {
                Text = opt.Name,
                FontSize = 12,
                FontWeight = FontWeight.Medium,
                Foreground = GetPrimaryBrush(),
                VerticalAlignment = VerticalAlignment.Center
            });

            if (!string.IsNullOrEmpty(opt.Hint))
            {
                leftStack.Children.Add(new TextBlock
                {
                    Text = opt.Hint,
                    FontSize = 10,
                    Foreground = GetHintBrush(),
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(4, 0, 0, 0)
                });
            }

            Grid.SetColumn(leftStack, 0);
            grid.Children.Add(leftStack);

            // Check mark for selected item
            if (isSelected)
            {
                var check = new Path
                {
                    Data = Geometry.Parse("M0,5 L3,8 L9,2"),
                    Stroke = GetAccentBrush(),
                    StrokeThickness = 1.8,
                    Width = 12,
                    Height = 10,
                    Stretch = Stretch.Uniform,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 0, 0, 0)
                };
                Grid.SetColumn(check, 1);
                grid.Children.Add(check);
            }

            row.Child = grid;

            // Click handler — capture opt.Index in closure
            var capturedIndex = opt.Index;
            row.PointerPressed += (_, _) =>
            {
                SelectedValue = capturedIndex;
                SelectionChanged?.Invoke(capturedIndex);
                CloseDropdown();
            };

            MenuPanel.Children.Add(row);
        }
    }

    // ─── Trigger label ───────────────────────────────────────────────────────

    private void UpdateTriggerLabel()
    {
        var selected = FindSelected();

        if (selected is null)
        {
            TriggerIcon.IsVisible = false;
            TriggerLabel.Text = Placeholder;
            TriggerLabel.Foreground = GetHintBrush();
        }
        else
        {
            TriggerLabel.Text = selected.Name;
            TriggerLabel.Foreground = GetPrimaryBrush();

            if (selected.Icon != null)
            {
                TriggerIcon.Content = selected.Icon;
                TriggerIcon.IsVisible = true;
            }
            else
            {
                TriggerIcon.IsVisible = false;
            }
        }
    }

    private SelectOption? FindSelected()
    {
        foreach (var opt in Options)
            if (Equals(opt.Index, SelectedValue))
                return opt;
        return null;
    }

    private bool IsDarkTheme => ActualThemeVariant == global::Avalonia.Styling.ThemeVariant.Dark;

    private IBrush GetPrimaryBrush()
        => new SolidColorBrush(IsDarkTheme ? Color.Parse("#ECF4FF") : Color.Parse("#17385E"));

    private IBrush GetHintBrush()
        => new SolidColorBrush(IsDarkTheme ? Color.Parse("#7C92B4") : Color.Parse("#58779A"));

    private IBrush GetAccentBrush()
        => new SolidColorBrush(IsDarkTheme ? Color.Parse("#60A5FA") : Color.Parse("#2F7FE6"));
}
