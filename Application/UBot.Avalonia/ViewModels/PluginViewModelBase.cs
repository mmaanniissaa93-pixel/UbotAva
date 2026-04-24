using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using UBot.Avalonia.Services;

namespace UBot.Avalonia.ViewModels;

public abstract partial class PluginViewModelBase : ObservableObject
{
    public readonly IUbotCoreService Core;
    protected readonly AppState State;
    protected string PluginId { get; private set; } = "";
    public string CurrentPluginId => PluginId;

    protected PluginViewModelBase(IUbotCoreService core, AppState state)
    {
        Core = core; State = state;
    }

    public virtual void Attach(string pluginId) { PluginId = pluginId; OnAttached(); }
    protected virtual void OnAttached() { }

    protected Dictionary<string, object?> Config => State.GetConfig(PluginId);

    public bool BoolCfg(string key, bool fb = false)
        => Config.TryGetValue(key, out var v) && v is bool b ? b : fb;

    public double NumCfg(string key, double fb = 0)
    {
        if (Config.TryGetValue(key, out var v))
        {
            if (v is double d) return d;
            if (v is float f)  return f;
            if (v is int i)    return i;
            if (v is long l)   return l;
            if (v is uint ui)  return ui;
            if (v is ulong ul) return ul;
            if (v is short sh)  return sh;
            if (v is ushort us) return us;
            if (v is byte b)   return b;
            if (v is sbyte sb) return sb;
            if (v is string text && double.TryParse(text, out var p)) return p;
        }
        return fb;
    }

    public string TextCfg(string key, string fb = "")
        => Config.TryGetValue(key, out var v) ? v?.ToString() ?? fb : fb;

    public object? ObjCfg(string key)
        => Config.TryGetValue(key, out var v) ? v : null;

    public List<string> ListCfg(string key)
    {
        var r = new List<string>();
        if (Config.TryGetValue(key, out var v) && v is List<string> list)
            return list;
        return r;
    }

    public async Task PatchConfigAsync(Dictionary<string, object?> patch)
    {
        State.PatchConfig(PluginId, patch);
        await Core.SetPluginConfigAsync(PluginId, patch);
    }

    public async Task PluginActionAsync(string action, Dictionary<string, object?>? payload = null)
        => await Core.InvokePluginActionAsync(PluginId, action, payload);

    public async Task<bool> InvokeActionCandidatesAsync(string[] actions, Dictionary<string, object?>? payload = null)
    {
        foreach (var a in actions)
            if (await Core.InvokePluginActionAsync(PluginId, a, payload))
                return true;
        return false;
    }

    public Task<byte[]?> GetIconAsync(string iconFile)
        => Core.GetSkillIconAsync(iconFile);

    public async Task LoadConfigAsync()
    {
        if (string.IsNullOrEmpty(PluginId)) return;
        var cfg = await Core.GetPluginConfigAsync(PluginId);
        State.SetConfig(PluginId, cfg);
    }

    public async Task BrowseScriptFileAsync(string configKey)
    {
        var path = await Core.PickScriptFileAsync();
        if (!string.IsNullOrEmpty(path))
            await PatchConfigAsync(new Dictionary<string, object?> { [configKey] = path });
    }
}
