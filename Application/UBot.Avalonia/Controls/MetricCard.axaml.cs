using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace UBot.Avalonia.Controls;

/// <summary>
/// Mirrors MetricCard.tsx — a small stat tile with label, value, optional progress bar.
/// </summary>
public partial class MetricCard : UserControl
{
    // ─── Avalonia Properties ────────────────────────────────────────────────

    public static readonly StyledProperty<string> LabelProperty =
        AvaloniaProperty.Register<MetricCard, string>(nameof(Label), string.Empty);

    public static readonly StyledProperty<string> ValueProperty =
        AvaloniaProperty.Register<MetricCard, string>(nameof(Value), "—");

    /// <summary>neutral | green | blue | amber</summary>
    public static readonly StyledProperty<string> AccentProperty =
        AvaloniaProperty.Register<MetricCard, string>(nameof(Accent), "neutral");

    public static readonly StyledProperty<double?> ProgressProperty =
        AvaloniaProperty.Register<MetricCard, double?>(nameof(Progress), null);

    public static readonly StyledProperty<bool> EmptyProperty =
        AvaloniaProperty.Register<MetricCard, bool>(nameof(Empty), false);

    public static readonly StyledProperty<string?> HintProperty =
        AvaloniaProperty.Register<MetricCard, string?>(nameof(Hint), null);

    public static readonly StyledProperty<bool> ShowProgressTextProperty =
        AvaloniaProperty.Register<MetricCard, bool>(nameof(ShowProgressText), false);

    // ─── CLR wrappers ───────────────────────────────────────────────────────

    public string Label
    {
        get => GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Accent
    {
        get => GetValue(AccentProperty);
        set => SetValue(AccentProperty, value);
    }

    public double? Progress
    {
        get => GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public bool Empty
    {
        get => GetValue(EmptyProperty);
        set => SetValue(EmptyProperty, value);
    }

    public string? Hint
    {
        get => GetValue(HintProperty);
        set => SetValue(HintProperty, value);
    }

    public bool ShowProgressText
    {
        get => GetValue(ShowProgressTextProperty);
        set => SetValue(ShowProgressTextProperty, value);
    }

    // ─── Constructor ────────────────────────────────────────────────────────

    public MetricCard()
    {
        InitializeComponent();
    }

    // ─── Property-change sync ────────────────────────────────────────────────

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == LabelProperty)
            LabelText.Text = Label;

        if (e.Property == ValueProperty)
            UpdateValue();

        if (e.Property == AccentProperty || e.Property == EmptyProperty)
            UpdateValue();

        if (e.Property == HintProperty)
        {
            HintText.Text = Hint;
            HintText.IsVisible = !string.IsNullOrEmpty(Hint);
        }

        if (e.Property == ProgressProperty || e.Property == ShowProgressTextProperty)
            UpdateProgress();
    }

    private void UpdateValue()
    {
        ValueText.Text = Value;

        // Remove previous accent classes
        ValueText.Classes.Remove("green");
        ValueText.Classes.Remove("blue");
        ValueText.Classes.Remove("amber");
        ValueText.Classes.Remove("placeholder");

        if (Empty)
        {
            ValueText.Classes.Add("placeholder");
        }
        else if (Accent != "neutral")
        {
            ValueText.Classes.Add(Accent);
        }
    }

    private void UpdateProgress()
    {
        if (Progress is double p && double.IsFinite(p))
        {
            var pct = Math.Clamp(p, 0, 100);
            ProgressPanel.IsVisible = true;

            // Width is set as a proportion of the card width via layout binding approach.
            // Use a simple Binding in real usage; here we calculate on width change.
            ProgressFill.Width = double.NaN; // will use percentage column trick below
            ProgressFill.Tag = pct;          // store for OnSizeChanged

            ProgressText.IsVisible = ShowProgressText;
            ProgressText.Text = $"{pct:F0}%";
        }
        else
        {
            ProgressPanel.IsVisible = false;
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (ProgressFill.Tag is double pct && ProgressPanel.IsVisible)
        {
            ProgressFill.Width = ProgressPanel.Bounds.Width * pct / 100.0;
        }
    }
}
