using Avalonia.Controls;
using Avalonia.Interactivity;
using System;
using System.Threading.Tasks;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.General;

public partial class SoundNotificationsWindow : Window
{
    private readonly GeneralViewModel? _vm;
    private bool _isPlayerLoggedIn;

    public SoundNotificationsWindow()
    {
        InitializeComponent();
    }

    public SoundNotificationsWindow(GeneralViewModel vm)
        : this()
    {
        _vm = vm;
    }

    protected override async void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        if (Content is Control rootControl)
            DesktopLanguageService.ApplyToControl(rootControl, DesktopLanguageService.CurrentLanguage);
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        if (_vm == null)
            return;

        var settings = await _vm.LoadSoundNotificationSettingsAsync();
        _isPlayerLoggedIn = settings.IsPlayerLoggedIn;
        ApplySettings(settings);
    }

    private void ApplySettings(SoundNotificationSettingsDto settings)
    {
        UniqueAppearedToggle.IsChecked = settings.PlayUniqueAppeared;
        UniqueAppearedPathBox.Text = settings.PathUniqueAppeared;
        RegexBox.Text = settings.MatchRegex;

        TigerGirlToggle.IsChecked = settings.PlayTigerGirl;
        TigerGirlPathBox.Text = settings.PathTigerGirl;

        CerberusToggle.IsChecked = settings.PlayCerberus;
        CerberusPathBox.Text = settings.PathCerberus;

        CaptainIvyToggle.IsChecked = settings.PlayCaptainIvy;
        CaptainIvyPathBox.Text = settings.PathCaptainIvy;

        UruchiToggle.IsChecked = settings.PlayUruchi;
        UruchiPathBox.Text = settings.PathUruchi;

        IsyutaruToggle.IsChecked = settings.PlayIsyutaru;
        IsyutaruPathBox.Text = settings.PathIsyutaru;

        LordYarkanToggle.IsChecked = settings.PlayLordYarkan;
        LordYarkanPathBox.Text = settings.PathLordYarkan;

        DemonShaitanToggle.IsChecked = settings.PlayDemonShaitan;
        DemonShaitanPathBox.Text = settings.PathDemonShaitan;

        UniqueInRangeToggle.IsChecked = settings.PlayUniqueInRange;
        UniqueInRangePathBox.Text = settings.PathUniqueInRange;

        SetPlayerLoggedInState(settings.IsPlayerLoggedIn);
    }

    private void SetPlayerLoggedInState(bool isLoggedIn)
    {
        SettingsPanel.IsEnabled = isLoggedIn;
        StatusText.IsVisible = !isLoggedIn;
    }

    private async void BrowseSound_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null || sender is not Button { Tag: string tag })
            return;

        var selectedPath = await _vm.PickSoundFileAsync();
        if (string.IsNullOrWhiteSpace(selectedPath))
            return;

        var target = ResolvePathBox(tag);
        if (target != null)
            target.Text = selectedPath;
    }

    private TextBox? ResolvePathBox(string key)
    {
        return key switch
        {
            "uniqueAppeared" => UniqueAppearedPathBox,
            "tigerGirl" => TigerGirlPathBox,
            "cerberus" => CerberusPathBox,
            "captainIvy" => CaptainIvyPathBox,
            "uruchi" => UruchiPathBox,
            "isyutaru" => IsyutaruPathBox,
            "lordYarkan" => LordYarkanPathBox,
            "demonShaitan" => DemonShaitanPathBox,
            "uniqueInRange" => UniqueInRangePathBox,
            _ => null
        };
    }

    private SoundNotificationSettingsDto BuildSettings()
    {
        return new SoundNotificationSettingsDto
        {
            IsPlayerLoggedIn = _isPlayerLoggedIn,

            PlayUniqueAppeared = UniqueAppearedToggle.IsChecked == true,
            PathUniqueAppeared = NormalizePath(UniqueAppearedPathBox.Text),
            MatchRegex = NormalizeRegex(RegexBox.Text),

            PlayTigerGirl = TigerGirlToggle.IsChecked == true,
            PathTigerGirl = NormalizePath(TigerGirlPathBox.Text),

            PlayCerberus = CerberusToggle.IsChecked == true,
            PathCerberus = NormalizePath(CerberusPathBox.Text),

            PlayCaptainIvy = CaptainIvyToggle.IsChecked == true,
            PathCaptainIvy = NormalizePath(CaptainIvyPathBox.Text),

            PlayUruchi = UruchiToggle.IsChecked == true,
            PathUruchi = NormalizePath(UruchiPathBox.Text),

            PlayIsyutaru = IsyutaruToggle.IsChecked == true,
            PathIsyutaru = NormalizePath(IsyutaruPathBox.Text),

            PlayLordYarkan = LordYarkanToggle.IsChecked == true,
            PathLordYarkan = NormalizePath(LordYarkanPathBox.Text),

            PlayDemonShaitan = DemonShaitanToggle.IsChecked == true,
            PathDemonShaitan = NormalizePath(DemonShaitanPathBox.Text),

            PlayUniqueInRange = UniqueInRangeToggle.IsChecked == true,
            PathUniqueInRange = NormalizePath(UniqueInRangePathBox.Text)
        };
    }

    private static string NormalizePath(string? value)
    {
        return (value ?? string.Empty).Trim().Trim('"');
    }

    private static string NormalizeRegex(string? value)
    {
        var regex = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(regex) ? "^.*$" : regex;
    }

    private async void Ok_Click(object? sender, RoutedEventArgs e)
    {
        if (_vm == null)
            return;

        if (!_isPlayerLoggedIn)
        {
            SetPlayerLoggedInState(false);
            return;
        }

        var result = await _vm.SaveSoundNotificationSettingsAsync(BuildSettings());
        if (!result)
        {
            _isPlayerLoggedIn = false;
            SetPlayerLoggedInState(false);
            return;
        }

        Close(true);
    }

    private void Cancel_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close(false);
    }
}
