using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Globalization;

namespace UBot.Avalonia.Features.Party;

public class PartyFormDialogModel
{
    public int Purpose { get; set; }
    public int LevelFrom { get; set; } = 1;
    public int LevelTo { get; set; } = 140;
    public bool ExpAutoShare { get; set; } = true;
    public bool ItemAutoShare { get; set; } = true;
    public bool AllowInvitations { get; set; } = true;
    public string Title { get; set; } = "For opening hunting on the silkroad!";
    public bool AutoReform { get; set; }
    public bool AutoAccept { get; set; } = true;
}

public sealed class PartyFormDialogResult : PartyFormDialogModel
{
}

public partial class PartyFormWindow : Window
{
    private bool _allowInvitations = true;

    public PartyFormWindow()
    {
        InitializeComponent();
    }

    public PartyFormWindow(PartyFormDialogModel model)
        : this()
    {
        if (model == null)
            return;

        _allowInvitations = model.AllowInvitations;

        var purpose = Math.Clamp(model.Purpose, 0, 3);
        PurposeHuntingRadio.IsChecked = purpose == 0;
        PurposeQuestRadio.IsChecked = purpose == 1;
        PurposeTradeRadio.IsChecked = purpose == 2;
        PurposeThiefRadio.IsChecked = purpose == 3;

        LevelFromBox.Text = Math.Clamp(model.LevelFrom, 1, 140).ToString(CultureInfo.InvariantCulture);
        LevelToBox.Text = Math.Clamp(model.LevelTo, 1, 140).ToString(CultureInfo.InvariantCulture);

        ExpAutoShareToggle.IsChecked = model.ExpAutoShare;
        ItemAutoShareToggle.IsChecked = model.ItemAutoShare;
        AutoReformToggle.IsChecked = model.AutoReform;
        AutoAcceptToggle.IsChecked = model.AutoAccept;

        TitleBox.Text = string.IsNullOrWhiteSpace(model.Title)
            ? "For opening hunting on the silkroad!"
            : model.Title.Trim();
    }

    private void Accept_Click(object? sender, RoutedEventArgs e)
    {
        var purpose = PurposeQuestRadio.IsChecked == true ? 1 :
            PurposeTradeRadio.IsChecked == true ? 2 :
            PurposeThiefRadio.IsChecked == true ? 3 : 0;

        var levelFrom = ParseInt(LevelFromBox.Text, 1);
        var levelTo = ParseInt(LevelToBox.Text, 140);
        levelFrom = Math.Clamp(levelFrom, 1, 140);
        levelTo = Math.Clamp(levelTo, 1, 140);
        if (levelFrom > levelTo)
            (levelFrom, levelTo) = (levelTo, levelFrom);

        var title = (TitleBox.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(title))
            title = "For opening hunting on the silkroad!";

        Close(new PartyFormDialogResult
        {
            Purpose = purpose,
            LevelFrom = levelFrom,
            LevelTo = levelTo,
            ExpAutoShare = ExpAutoShareToggle.IsChecked == true,
            ItemAutoShare = ItemAutoShareToggle.IsChecked == true,
            AllowInvitations = _allowInvitations,
            Title = title,
            AutoReform = AutoReformToggle.IsChecked == true,
            AutoAccept = AutoAcceptToggle.IsChecked == true
        });
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(null);
    }

    private static int ParseInt(string? text, int fallback)
    {
        if (int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            return parsed;

        return fallback;
    }
}
