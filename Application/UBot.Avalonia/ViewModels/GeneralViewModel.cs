using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
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

    public Action? RequestAccountSetupDialog  { get; set; }
    public Action? RequestSoundSettingsDialog { get; set; }

    public GeneralViewModel(IUbotCoreService core, AppState state) : base(core, state) { }

    protected override async void OnAttached()
    {
        await LoadConfigAsync();
        SroExecutable = TextCfg("sroExecutable");
        var opts = await Core.GetConnectionOptionsAsync();
        ConnectionOptions  = opts;
        ClientTypeOptions  = opts.ClientTypes ?? new List<ConnectionClientTypeDto>();
        State.ConnectionOptions = opts;
    }

    public async Task SyncSroExecutablePathAsync(string raw)
    {
        var path = raw.Trim().Trim('"');
        if (string.IsNullOrEmpty(path)) return;
        await PatchConfigAsync(new Dictionary<string, object?> { ["sroExecutable"] = path });
        await Core.SetSroExecutableAsync(path);
    }

    public async void PickSroExecutableAsync()
    {
        var path = await Core.PickExecutableAsync();
        if (!string.IsNullOrEmpty(path)) { SroExecutable = path; await SyncSroExecutablePathAsync(path); }
    }

    public async void StartClientAsync()       => await Core.StartClientAsync();
    public async void GoClientlessAsync()      => await Core.GoClientlessAsync();
    public async void ToggleClientVisibilityAsync() => await Core.ToggleClientVisibilityAsync();
    public async Task TogglePendingWindowAsync() => await PluginActionAsync("general.toggle-pending-window");
    public async Task OpenSoundSettingsAsync()  { RequestSoundSettingsDialog?.Invoke(); await PluginActionAsync("general.open-sound-settings"); }
    public void OpenAccountSetupDialog()        => RequestAccountSetupDialog?.Invoke();
}
