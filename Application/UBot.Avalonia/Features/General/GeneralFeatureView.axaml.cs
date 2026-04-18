using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.Generic;
using UBot.Avalonia.Controls;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;

namespace UBot.Avalonia.Features.General;

public partial class GeneralFeatureView : UserControl
{
    private GeneralViewModel? _vm;
    private bool _syncing;

    public GeneralFeatureView() { InitializeComponent(); }

    public void Initialize(GeneralViewModel vm)
    {
        _vm = vm;
        Refresh();
    }

    public void Refresh()
    {
        if (_vm is null) return;
        _syncing = true;

        SroBox.Text = _vm.TextCfg("sroExecutable");
        StayConnectedCheck.IsChecked      = _vm.BoolCfg("stayConnectedAfterClientExit");
        MoveToTrayCheck.IsChecked         = _vm.BoolCfg("moveToTrayOnMinimize");
        AutoHidePendingCheck.IsChecked    = _vm.BoolCfg("autoHidePendingWindow");
        EnablePendingLogsCheck.IsChecked  = _vm.BoolCfg("enablePendingQueueLogs");
        EnableQueueNotifCheck.IsChecked   = _vm.BoolCfg("enableQueueNotification");
        PeopleLeftBox.Text                = _vm.NumCfg("queuePeopleLeft", 30).ToString("F0");
        EnableAutoLoginCheck.IsChecked    = _vm.BoolCfg("enableAutomatedLogin");
        EnableStaticCaptchaCheck.IsChecked= _vm.BoolCfg("enableStaticCaptcha");
        EnableLoginDelayCheck.IsChecked   = _vm.BoolCfg("enableLoginDelay");
        WaitAfterDcCheck.IsChecked        = _vm.BoolCfg("enableWaitAfterDc");
        AutoStartBotCheck.IsChecked       = _vm.BoolCfg("autoStartBot");
        UseReturnScrollCheck.IsChecked    = _vm.BoolCfg("useReturnScroll");
        AutoCharSelectCheck.IsChecked     = _vm.BoolCfg("autoCharSelect");
        AutoHideClientCheck.IsChecked     = _vm.BoolCfg("autoHideClient");
        FirstFoundRadio.IsChecked         = _vm.BoolCfg("characterAutoSelectFirst", true);
        HighestLevelRadio.IsChecked       = _vm.BoolCfg("characterAutoSelectHigher");
        CaptchaBox.Text                   = _vm.TextCfg("staticCaptcha");
        LoginDelayBox.Text                = _vm.NumCfg("loginDelay", 10).ToString("F0");
        WaitAfterDcBox.Text               = _vm.NumCfg("waitAfterDc", 3).ToString("F0");

        // Account select
        var accs = _vm.AutoLoginAccounts;
        var aOpts = new List<SelectOption> { new("", "(Not selected)") };
        foreach (var a in accs) aOpts.Add(new SelectOption(a, a));
        AccountSelect.Options       = aOpts;
        AccountSelect.SelectedValue = _vm.TextCfg("autoLoginAccount");

        // Character select
        var chars = _vm.AutoLoginCharacters;
        var cOpts = new List<SelectOption> { new("", "(Not selected)") };
        foreach (var c in chars) cOpts.Add(new SelectOption(c, c));
        CharacterSelect.Options       = cOpts;
        CharacterSelect.SelectedValue = _vm.TextCfg("selectedCharacter");

        // Client type
        var ctOpts = new List<SelectOption>();
        foreach (var ct in _vm.ClientTypeOptions) ctOpts.Add(new SelectOption(ct.Id, ct.Name));
        if (ctOpts.Count == 0) ctOpts.Add(new SelectOption("Vietnam", "Vietnam"));
        ClientTypeSelect.Options       = ctOpts;
        ClientTypeSelect.SelectedValue = _vm.ConnectionOptions.ClientType ?? "Vietnam";

        _syncing = false;
    }

    private void SroBox_Changed(object? s, TextChangedEventArgs e)
    {
        if (!_syncing && _vm != null)
            _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { ["sroExecutable"] = SroBox.Text });
    }
    private void SroBox_LostFocus(object? s, RoutedEventArgs e)
        => _vm?.SyncSroExecutablePathAsync(SroBox.Text ?? "");

    private void BrowseSro_Click(object? s, RoutedEventArgs e) => _vm?.PickSroExecutableAsync();
    private void StartClient_Click(object? s, RoutedEventArgs e) => _vm?.StartClientAsync();
    private void GoClientless_Click(object? s, RoutedEventArgs e) => _vm?.GoClientlessAsync();
    private void ToggleClientVisibility_Click(object? s, RoutedEventArgs e) => _vm?.ToggleClientVisibilityAsync();
    private void AccountSetup_Click(object? s, RoutedEventArgs e) => _vm?.OpenAccountSetupDialog();
    private void TogglePendingWindow_Click(object? s, RoutedEventArgs e) => _vm?.TogglePendingWindowAsync();
    private void OpenSoundSettings_Click(object? s, RoutedEventArgs e) => _vm?.OpenSoundSettingsAsync();

    private void Captcha_LostFocus(object? s, RoutedEventArgs e)
    {
        if (_vm != null)
            _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { ["staticCaptcha"] = CaptchaBox.Text });
    }

    private void Check_Changed(object? s, RoutedEventArgs e)
    {
        if (_syncing || _vm is null || s is not CheckBox cb || cb.Tag is not string key) return;
        _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { [key] = cb.IsChecked == true });
    }

    private void NumBox_Changed(object? s, TextChangedEventArgs e)
    {
        if (_syncing || _vm is null || s is not TextBox tb || tb.Tag is not string key) return;
        if (double.TryParse(tb.Text, out var v))
            _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { [key] = v });
    }

    private void RadioFirstFound_Checked(object? s, RoutedEventArgs e)
    {
        if (_syncing || _vm is null) return;
        _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { ["autoCharSelect"] = true, ["characterAutoSelectFirst"] = true, ["characterAutoSelectHigher"] = false });
    }
    private void RadioHighest_Checked(object? s, RoutedEventArgs e)
    {
        if (_syncing || _vm is null) return;
        _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { ["autoCharSelect"] = true, ["characterAutoSelectFirst"] = false, ["characterAutoSelectHigher"] = true });
    }
}

