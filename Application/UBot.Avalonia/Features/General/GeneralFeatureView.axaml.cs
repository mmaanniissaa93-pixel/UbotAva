using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Globalization;
using UBot.Avalonia.Controls;
using UBot.Avalonia.Services;
using UBot.Avalonia.ViewModels;
using UBot.Core;

namespace UBot.Avalonia.Features.General;

public partial class GeneralFeatureView : UserControl
{
    private GeneralViewModel? _vm;
    private bool _syncing;

    public GeneralFeatureView() { InitializeComponent(); }

    public void Initialize(GeneralViewModel vm)
    {
        _vm = vm;
        _vm.UiStateChanged += OnVmUiStateChanged;
        _vm.RequestAccountSetupDialog = () => _ = OpenAccountSetupDialogAsync();
        _vm.RequestSoundSettingsDialog = () => _ = OpenSoundSettingsDialogAsync();

        AccountSelect.SelectionChanged += AccountSelect_SelectionChanged;
        CharacterSelect.SelectionChanged += CharacterSelect_SelectionChanged;
        ClientTypeSelect.SelectionChanged += ClientTypeSelect_SelectionChanged;
    }

    public void Refresh()
    {
        if (_vm is null) return;
        _syncing = true;

        var sroPath = _vm.SroExecutable;
        if (string.IsNullOrWhiteSpace(sroPath))
            sroPath = _vm.TextCfg("sroExecutable");
        SroBox.Text = sroPath;
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

        // Keep sync guard until the current UI cycle ends so programmatic Text updates
        // never trigger persistence handlers with fallback/default values.
        Dispatcher.UIThread.Post(() => _syncing = false);
    }

    private void SroBox_Changed(object? s, TextChangedEventArgs e)
    {
        if (!_syncing && _vm != null)
            _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { ["sroExecutable"] = SroBox.Text });
    }
    private void SroBox_LostFocus(object? s, RoutedEventArgs e)
        => _vm?.SyncSroExecutablePathAsync(SroBox.Text ?? "");

    private async void BrowseSro_Click(object? s, RoutedEventArgs e)
    {
        if (_vm == null) return;
        var path = await _vm.PickSroExecutableAsync();
        if (!string.IsNullOrEmpty(path))
        {
            _syncing = true;
            SroBox.Text = path;
            _syncing = false;
        }
    }
    private void StartClient_Click(object? s, RoutedEventArgs e) => _vm?.StartClientAsync();
    private void GoClientless_Click(object? s, RoutedEventArgs e) => _vm?.GoClientlessAsync();
    private void ToggleClientVisibility_Click(object? s, RoutedEventArgs e) => _vm?.ToggleClientVisibilityAsync();
    private void AccountSetup_Click(object? s, RoutedEventArgs e) => _ = _vm?.OpenAccountSetupDialogAsync();
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
        PersistCriticalGeneralBool(key, cb.IsChecked == true);
        _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { [key] = cb.IsChecked == true });
    }

    private void NumBox_Changed(object? s, TextChangedEventArgs e)
    {
        if (_syncing || _vm is null || s is not TextBox tb || tb.Tag is not string key) return;
        if (double.TryParse(tb.Text, out var v))
        {
            if (key is "loginDelay" or "waitAfterDc")
                PersistCriticalGeneralInt(key, Math.Max(0, (int)Math.Round(v)));
            _ = _vm.PatchConfigAsync(new Dictionary<string, object?> { [key] = v });
        }
    }

    private async void NumBox_LostFocus(object? s, RoutedEventArgs e)
    {
        if (_syncing || _vm is null || s is not TextBox tb || tb.Tag is not string key)
            return;

        if (!TryParseNumeric(tb.Text, out var parsed))
            return;

        // Persist a normalized integer on focus loss so close/reopen keeps the last value.
        var normalized = Math.Max(0, (int)Math.Round(parsed));
        tb.Text = normalized.ToString(CultureInfo.InvariantCulture);
        PersistCriticalGeneralInt(key, normalized);
        await _vm.PatchConfigAsync(new Dictionary<string, object?> { [key] = normalized });
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

    private void AccountSelect_SelectionChanged(object value)
    {
        if (_syncing || _vm is null) return;
        _ = _vm.SelectAutoLoginAccountAsync(value?.ToString());
    }

    private void CharacterSelect_SelectionChanged(object value)
    {
        if (_syncing || _vm is null) return;
        _ = _vm.SelectAutoLoginCharacterAsync(value?.ToString());
    }

    private void ClientTypeSelect_SelectionChanged(object value)
    {
        if (_syncing || _vm is null) return;
        _ = _vm.UpdateClientTypeAsync(value?.ToString());
    }

    private void OnVmUiStateChanged()
    {
        Dispatcher.UIThread.Post(Refresh);
    }

    private static bool TryParseNumeric(string? raw, out double value)
    {
        value = 0;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        var text = raw.Trim();
        return double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value)
               || double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static void PersistCriticalGeneralBool(string key, bool value)
    {
        string? configKey = key switch
        {
            "enableLoginDelay" => "UBot.General.EnableLoginDelay",
            "enableWaitAfterDc" => "UBot.General.EnableWaitAfterDC",
            _ => null
        };

        if (configKey == null)
            return;

        UBot.Core.RuntimeAccess.Global.Set(configKey, value);
        UBot.Core.RuntimeAccess.Global.Save();
    }

    private static void PersistCriticalGeneralInt(string key, int value)
    {
        string? configKey = key switch
        {
            "loginDelay" => "UBot.General.LoginDelay",
            "waitAfterDc" => "UBot.General.WaitAfterDC",
            _ => null
        };

        if (configKey == null)
            return;

        UBot.Core.RuntimeAccess.Global.Set(configKey, Math.Clamp(value, 0, 3600));
        UBot.Core.RuntimeAccess.Global.Save();
    }

    private async System.Threading.Tasks.Task OpenAccountSetupDialogAsync()
    {
        if (_vm == null)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var dialog = new AccountSetupWindow(_vm);
        if (owner != null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();

        await _vm.LoadConfigAsync();
        await _vm.RefreshUiModelFromConfigAsync();
    }

    private async System.Threading.Tasks.Task OpenSoundSettingsDialogAsync()
    {
        if (_vm == null)
            return;

        var owner = TopLevel.GetTopLevel(this) as Window;
        var dialog = new SoundNotificationsWindow(_vm);
        if (owner != null)
            await dialog.ShowDialog(owner);
        else
            dialog.Show();
    }
}

