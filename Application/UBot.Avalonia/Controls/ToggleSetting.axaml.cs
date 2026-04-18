using Avalonia;
using Avalonia.Controls;
using System;

namespace UBot.Avalonia.Controls;

/// <summary>
/// Mirrors ToggleSetting.tsx.
/// </summary>
public partial class ToggleSetting : UserControl
{
    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<ToggleSetting, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> DescriptionProperty =
        AvaloniaProperty.Register<ToggleSetting, string>(nameof(Description), string.Empty);

    public static readonly StyledProperty<bool> IsCheckedProperty =
        AvaloniaProperty.Register<ToggleSetting, bool>(nameof(IsChecked), false);

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Description
    {
        get => GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public bool IsChecked
    {
        get => GetValue(IsCheckedProperty);
        set => SetValue(IsCheckedProperty, value);
    }

    /// <summary>Fires when the toggle changes. Passes the new value.</summary>
    public event Action<bool>? Changed;

    public ToggleSetting()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == LabelProperty)
            LabelText.Text = Label;

        if (e.Property == DescriptionProperty)
            DescriptionText.Text = Description;

        if (e.Property == IsCheckedProperty)
            Toggle.IsChecked = IsChecked;
    }

    private void Toggle_Checked(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (IsChecked != true)
        {
            IsChecked = true;
            Changed?.Invoke(true);
        }
    }

    private void Toggle_Unchecked(object? sender, global::Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (IsChecked != false)
        {
            IsChecked = false;
            Changed?.Invoke(false);
        }
    }
}
