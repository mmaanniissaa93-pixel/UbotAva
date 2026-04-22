using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace UBot.Avalonia.Services;

public sealed class ChatMessageEntry
{
    public string Channel { get; init; } = "all";
    public string Sender { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string DisplayText { get; init; } = string.Empty;
}

public partial class AppState : ObservableObject
{
    [ObservableProperty] private bool   _botRunning;
    [ObservableProperty] private string _profile        = "Default";
    [ObservableProperty] private string _character      = "-";
    [ObservableProperty] private string _server         = "Unknown";
    [ObservableProperty] private bool   _agentConnected;
    [ObservableProperty] private bool   _gatewayConnected;
    [ObservableProperty] private bool   _clientReady;
    [ObservableProperty] private bool   _clientStarted;
    [ObservableProperty] private string _connectionMode = "clientless";
    [ObservableProperty] private ConnectionOptions _connectionOptions = new();

    // Player stats
    [ObservableProperty] private int    _playerLevel;
    [ObservableProperty] private long   _playerHealth;
    [ObservableProperty] private long   _playerMaxHealth;
    [ObservableProperty] private double _playerHealthPercent;
    [ObservableProperty] private long   _playerMana;
    [ObservableProperty] private long   _playerMaxMana;
    [ObservableProperty] private double _playerManaPercent;
    [ObservableProperty] private double _playerExpPercent;
    [ObservableProperty] private bool   _hasLiveStats;

    public ObservableCollection<PluginDescriptor> Plugins { get; } = new();

    private readonly Dictionary<string, Dictionary<string, object?>> _configs = new();

    public Dictionary<string, object?> GetConfig(string pluginId)
        => _configs.TryGetValue(pluginId, out var c) ? c : new();

    public void SetConfig(string pluginId, Dictionary<string, object?> cfg)
        => _configs[pluginId] = cfg;

    public void PatchConfig(string pluginId, Dictionary<string, object?> patch)
    {
        if (!_configs.TryGetValue(pluginId, out var existing))
            existing = new();
        foreach (var kv in patch) existing[kv.Key] = kv.Value;
        _configs[pluginId] = existing;
        OnPropertyChanged(nameof(Plugins));
    }

    public ObservableCollection<string> LogLines { get; } = new();
    public ObservableCollection<ChatMessageEntry> ChatMessages { get; } = new();

    public void AddLog(string message)
    {
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            LogLines.Insert(0, message);
            if (LogLines.Count > 800) LogLines.RemoveAt(LogLines.Count - 1);
        });
    }

    public void AddChatMessage(string channel, string sender, string message)
    {
        var normalizedChannel = string.IsNullOrWhiteSpace(channel) ? "all" : channel.Trim().ToLowerInvariant();
        var normalizedSender = sender?.Trim() ?? string.Empty;
        var normalizedMessage = message?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedMessage))
            return;

        var display = string.IsNullOrWhiteSpace(normalizedSender)
            ? $"[{normalizedChannel.ToUpperInvariant()}] {normalizedMessage}"
            : $"[{normalizedChannel.ToUpperInvariant()}] {normalizedSender}: {normalizedMessage}";

        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            ChatMessages.Insert(0, new ChatMessageEntry
            {
                Channel = normalizedChannel,
                Sender = normalizedSender,
                Message = normalizedMessage,
                DisplayText = display
            });

            if (ChatMessages.Count > 800)
                ChatMessages.RemoveAt(ChatMessages.Count - 1);
        });
    }

    public void ApplyStatus(RuntimeStatus s)
    {
        BotRunning       = s.BotRunning;
        Profile          = s.Profile;
        Character        = s.Character;
        Server           = s.Server;
        AgentConnected   = s.AgentConnected   ?? false;
        GatewayConnected = s.GatewayConnected ?? false;
        ClientReady      = s.ClientReady      ?? false;
        ClientStarted    = s.ClientStarted    ?? false;
        ConnectionMode   = s.ConnectionMode   ?? "clientless";

        if (s.Player is { } p)
        {
            PlayerLevel          = p.Level            ?? 0;
            PlayerHealth         = p.Health           ?? 0;
            PlayerMaxHealth      = p.MaxHealth        ?? 0;
            PlayerHealthPercent  = p.HealthPercent    ?? 0;
            PlayerMana           = p.Mana             ?? 0;
            PlayerMaxMana        = p.MaxMana          ?? 0;
            PlayerManaPercent    = p.ManaPercent      ?? 0;
            PlayerExpPercent     = p.ExperiencePercent ?? 0;
            HasLiveStats         = AgentConnected && (PlayerLevel > 0 || PlayerMaxHealth > 0);
        }
        else
        {
            PlayerLevel = 0;
            PlayerHealth = 0;
            PlayerMaxHealth = 0;
            PlayerHealthPercent = 0;
            PlayerMana = 0;
            PlayerMaxMana = 0;
            PlayerManaPercent = 0;
            PlayerExpPercent = 0;
            HasLiveStats = false;
        }
    }
}

