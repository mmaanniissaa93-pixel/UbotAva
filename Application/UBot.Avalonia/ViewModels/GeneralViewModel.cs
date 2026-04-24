using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UBot.Avalonia.Services;

namespace UBot.Avalonia.ViewModels;

public partial class GeneralViewModel : PluginViewModelBase
{
    [ObservableProperty] private string _sroExecutable = "";
    [ObservableProperty] private ConnectionOptions _connectionOptions = new();

    public List<ConnectionClientTypeDto> ClientTypeOptions { get; private set; } = new();
    public List<string> AutoLoginAccounts   { get; private set; } = new();
    public List<string> AutoLoginCharacters { get; private set; } = new();
    private Dictionary<string, List<string>> AutoLoginCharacterMap { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public Action? RequestAccountSetupDialog  { get; set; }
    public Action? RequestSoundSettingsDialog { get; set; }
    public event Action? UiStateChanged;

    public GeneralViewModel(IUbotCoreService core, AppState state) : base(core, state) { }

    protected override async void OnAttached()
    {
        await LoadConfigAsync();
        await RefreshUiModelFromConfigAsync();
    }

    public async Task RefreshUiModelFromConfigAsync()
    {
        var sroExecutable = TextCfg("sroExecutable");
        if (string.IsNullOrWhiteSpace(sroExecutable))
        {
            sroExecutable = await Core.GetSroExecutablePathAsync();
            if (!string.IsNullOrWhiteSpace(sroExecutable))
            {
                State.PatchConfig(PluginId, new Dictionary<string, object?> { ["sroExecutable"] = sroExecutable });
            }
        }

        SroExecutable = sroExecutable;
        var opts = await Core.GetConnectionOptionsAsync();
        ConnectionOptions  = opts;
        ClientTypeOptions  = opts.ClientTypes ?? new List<ConnectionClientTypeDto>();
        AutoLoginAccounts = ParseStringList(Config.TryGetValue("autoLoginAccounts", out var autoLoginAccounts) ? autoLoginAccounts : null);
        AutoLoginCharacterMap = ParseCharacterMap(Config.TryGetValue("autoLoginCharacterMap", out var characterMap) ? characterMap : null);
        AutoLoginCharacters = ParseStringList(Config.TryGetValue("autoLoginCharacters", out var autoLoginCharacters) ? autoLoginCharacters : null);

        var selectedAccount = TextCfg("autoLoginAccount");
        if (!string.IsNullOrWhiteSpace(selectedAccount)
            && AutoLoginCharacterMap.TryGetValue(selectedAccount, out var accountCharacters)
            && accountCharacters.Count > 0)
        {
            AutoLoginCharacters = accountCharacters;
        }

        State.ConnectionOptions = opts;
        UiStateChanged?.Invoke();
    }

    public async Task SyncSroExecutablePathAsync(string raw)
    {
        var path = raw.Trim().Trim('"');
        if (string.IsNullOrEmpty(path)) return;
        await PatchConfigAsync(new Dictionary<string, object?> { ["sroExecutable"] = path });
        await Core.SetSroExecutableAsync(path);
    }

    public async Task<string?> PickSroExecutableAsync()
    {
        var path = await Core.PickExecutableAsync();
        if (!string.IsNullOrEmpty(path)) { SroExecutable = path; await SyncSroExecutablePathAsync(path); }
        return path;
    }

    public async void StartClientAsync()       => await Core.StartClientAsync();
    public async void GoClientlessAsync()      => await Core.GoClientlessAsync();
    public async void ToggleClientVisibilityAsync() => await Core.ToggleClientVisibilityAsync();
    public async Task TogglePendingWindowAsync() => await PluginActionAsync("general.toggle-pending-window");
    public async Task OpenSoundSettingsAsync()  { RequestSoundSettingsDialog?.Invoke(); await PluginActionAsync("general.open-sound-settings"); }
    public async Task OpenAccountSetupDialogAsync()
    {
        if (RequestAccountSetupDialog != null)
        {
            RequestAccountSetupDialog.Invoke();
            return;
        }

        await PluginActionAsync("general.open-account-setup");
    }

    public async Task<IReadOnlyList<AutoLoginAccountDto>> LoadAutoLoginAccountsAsync()
        => await Core.GetAutoLoginAccountsAsync();

    public async Task<bool> SaveAutoLoginAccountsAsync(IReadOnlyList<AutoLoginAccountDto> accounts)
    {
        var result = await Core.SaveAutoLoginAccountsAsync(accounts);
        if (!result)
            return false;

        await LoadConfigAsync();
        await RefreshUiModelFromConfigAsync();
        return true;
    }

    public async Task<SoundNotificationSettingsDto> LoadSoundNotificationSettingsAsync()
        => await Core.GetSoundNotificationSettingsAsync();

    public async Task<bool> SaveSoundNotificationSettingsAsync(SoundNotificationSettingsDto settings)
        => await Core.SaveSoundNotificationSettingsAsync(settings);

    public async Task<string?> PickSoundFileAsync()
    {
        var path = await Core.PickSoundFileAsync();
        return string.IsNullOrWhiteSpace(path) ? null : path;
    }

    public async Task SelectAutoLoginAccountAsync(string? account)
    {
        var normalized = account?.Trim() ?? string.Empty;
        var patch = new Dictionary<string, object?> { ["autoLoginAccount"] = normalized };

        if (AutoLoginCharacterMap.TryGetValue(normalized, out var characters))
        {
            AutoLoginCharacters = characters.ToList();
            var selectedCharacter = TextCfg("selectedCharacter");
            if (!AutoLoginCharacters.Contains(selectedCharacter, StringComparer.OrdinalIgnoreCase))
                patch["selectedCharacter"] = AutoLoginCharacters.Count > 0 ? AutoLoginCharacters[0] : string.Empty;
        }
        else
        {
            AutoLoginCharacters = new List<string>();
            patch["selectedCharacter"] = string.Empty;
        }

        await PatchConfigAsync(patch);
        UiStateChanged?.Invoke();
    }

    public async Task SelectAutoLoginCharacterAsync(string? character)
    {
        await PatchConfigAsync(new Dictionary<string, object?> { ["selectedCharacter"] = character?.Trim() ?? string.Empty });
    }

    public async Task UpdateClientTypeAsync(string? clientType)
    {
        if (string.IsNullOrWhiteSpace(clientType))
            return;

        var updated = await Core.SetConnectionOptionsAsync(
            ConnectionOptions.DivisionIndex,
            ConnectionOptions.GatewayIndex,
            ConnectionOptions.Mode,
            clientType);

        ConnectionOptions = updated;
        ClientTypeOptions = updated.ClientTypes ?? new List<ConnectionClientTypeDto>();
        State.ConnectionOptions = updated;
        UiStateChanged?.Invoke();
    }

    private static List<string> ParseStringList(object? raw)
    {
        if (raw is null)
            return new List<string>();
        if (raw is List<string> list)
            return list.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (raw is IEnumerable<string> stringEnumerable)
            return stringEnumerable.Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        if (raw is not IEnumerable enumerable)
            return new List<string>();

        var values = new List<string>();
        foreach (var item in enumerable)
        {
            var text = item?.ToString();
            if (!string.IsNullOrWhiteSpace(text))
                values.Add(text.Trim());
        }

        return values.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static Dictionary<string, List<string>> ParseCharacterMap(object? raw)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        if (raw is not IDictionary dictionary)
            return result;

        foreach (DictionaryEntry entry in dictionary)
        {
            var key = entry.Key?.ToString()?.Trim();
            if (string.IsNullOrWhiteSpace(key))
                continue;

            result[key] = ParseStringList(entry.Value);
        }

        return result;
    }
}
