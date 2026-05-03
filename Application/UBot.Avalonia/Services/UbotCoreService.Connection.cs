using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UBot.FileSystem;
using UBot.NavMeshApi;
using UBot.NavMeshApi.Dungeon;
using UBot.NavMeshApi.Edges;
using UBot.NavMeshApi.Extensions;
using UBot.NavMeshApi.Terrain;
using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Extensions;
using UBot.Core.Network;
using UBot.Core.Network.Protocol;
using UBot.Core.Objects;
using UBot.Core.Objects.Party;
using UBot.Core.Objects.Spawn;
using UBot.Core.Objects.Skill;
using UBot.Core.Plugins;
using Forms = System.Windows.Forms;
using CoreRegion = UBot.Core.Objects.Region;

namespace UBot.Avalonia.Services;

internal sealed class UbotConnectionService : UbotServiceBase
{
    private readonly UbotCoreLifecycleService _lifecycle;
    private static bool _clientVisible = true;

    internal UbotConnectionService(UbotCoreLifecycleService lifecycle)
    {
        _lifecycle = lifecycle;
    }

    public Task<RuntimeStatus> GetStatusAsync()
    {
        return Task.FromResult(BuildStatusSnapshot());
    }

    internal RuntimeStatus CreateStatusSnapshot()
    {
        return BuildStatusSnapshot();
    }

    public Task<ConnectionOptions> GetConnectionOptionsAsync()
    {
        _lifecycle.EnsureClientInfoLoaded();

        var options = ResolveConnectionOptions();
        var normalized = NormalizeConnectionIndices(options.mode, options.divisionIndex, options.gatewayIndex);

        var session = UBot.Core.RuntimeAccess.Session;
        var clientType = session != null
            ? UBot.Core.RuntimeAccess.Global.GetEnum("UBot.Game.ClientType", session.ClientType)
            : GameClientType.Global;

        return Task.FromResult(new ConnectionOptions
        {
            Mode = normalized.mode,
            DivisionIndex = normalized.divisionIndex,
            GatewayIndex = normalized.gatewayIndex,
            Divisions = BuildDivisionOptions(),
            ClientType = clientType.ToString(),
            ClientTypes = BuildClientTypeOptions(),
            ReferenceLoading = _lifecycle.ReferenceLoading,
            ReferenceLoaded = _lifecycle.ReferenceLoaded
        });
    }

    public async Task<ConnectionOptions> SetConnectionOptionsAsync(int divisionIndex, int gatewayIndex, string? mode = null, string? clientType = null)
    {
        var options = ResolveConnectionOptions();
        var changed = false;

        if (!string.IsNullOrWhiteSpace(mode))
        {
            var normalizedMode = NormalizeConnectionMode(mode);
            if (!string.Equals(options.mode, normalizedMode, StringComparison.OrdinalIgnoreCase))
            {
                options.mode = normalizedMode;
                UBot.Core.RuntimeAccess.Global.Set(ConnectionModeKey, normalizedMode);
                changed = true;
            }
        }

        if (divisionIndex < 0)
            divisionIndex = 0;
        if (gatewayIndex < 0)
            gatewayIndex = 0;

        if (divisionIndex != options.divisionIndex)
        {
            options.divisionIndex = divisionIndex;
            options.gatewayIndex = 0;
            changed = true;
        }

        if (gatewayIndex != options.gatewayIndex)
        {
            options.gatewayIndex = gatewayIndex;
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(clientType)
            && Enum.TryParse<GameClientType>(clientType, true, out var requestedClientType))
        {
            var session = UBot.Core.RuntimeAccess.Session;
            var currentClientType = UBot.Core.RuntimeAccess.Global.GetEnum("UBot.Game.ClientType", session?.ClientType ?? GameClientType.Global);
            if (requestedClientType != currentClientType && (session == null || !session.Ready) && !_lifecycle.ReferenceLoading)
            {
                UBot.Core.RuntimeAccess.Global.Set("UBot.Game.ClientType", requestedClientType);
                if (session != null)
                    session.ClientType = requestedClientType;
                _lifecycle.MarkReferenceDataDirty();
                changed = true;
            }
        }

        var normalized = NormalizeConnectionIndices(options.mode, options.divisionIndex, options.gatewayIndex);
        UBot.Core.RuntimeAccess.Global.Set(ConnectionModeKey, normalized.mode);
        UBot.Core.RuntimeAccess.Global.Set("UBot.DivisionIndex", normalized.divisionIndex);
        UBot.Core.RuntimeAccess.Global.Set("UBot.GatewayIndex", normalized.gatewayIndex);
        if (changed)
            UBot.Core.RuntimeAccess.Global.Save();

        return await GetConnectionOptionsAsync();
    }


    public Task<RuntimeStatus> StartBotAsync()
    {
        if (UBot.Core.RuntimeAccess.Core.Bot?.Botbase == null)
            return Task.FromResult(BuildStatusSnapshot());

        if (UBot.Core.RuntimeAccess.Core.Proxy == null || !UBot.Core.RuntimeAccess.Core.Proxy.IsConnectedToAgentserver)
            return Task.FromResult(BuildStatusSnapshot());

        if (UBot.Core.RuntimeAccess.Session.Player == null)
            return Task.FromResult(BuildStatusSnapshot());

        if (!UBot.Core.RuntimeAccess.Core.Bot.Running)
            UBot.Core.RuntimeAccess.Core.Bot.Start();

        return Task.FromResult(BuildStatusSnapshot());
    }

    public Task<RuntimeStatus> StopBotAsync()
    {
        if (UBot.Core.RuntimeAccess.Core.Bot != null && UBot.Core.RuntimeAccess.Core.Bot.Running)
            UBot.Core.RuntimeAccess.Core.Bot.Stop();

        return Task.FromResult(BuildStatusSnapshot());
    }

    public Task<RuntimeStatus> DisconnectAsync()
    {
        if (UBot.Core.RuntimeAccess.Core.Bot != null && UBot.Core.RuntimeAccess.Core.Bot.Running)
            UBot.Core.RuntimeAccess.Core.Bot.Stop();

        try
        {
            UBot.Core.RuntimeAccess.Core.Proxy?.Shutdown();
        }
        catch
        {
            // ignored
        }

        try
        {
            ClientManager.Kill();
            _clientVisible = false;
        }
        catch
        {
            // ignored
        }

        if (UBot.Core.RuntimeAccess.Session != null)
            UBot.Core.RuntimeAccess.Session.Started = false;
        return Task.FromResult(BuildStatusSnapshot());
    }

    public Task SaveConfigAsync()
    {
        UBot.Core.RuntimeAccess.Global.Save();
        UBot.Core.RuntimeAccess.Player?.Save();
        return Task.CompletedTask;
    }

    public Task<bool> StartClientAsync()
    {
        var session = UBot.Core.RuntimeAccess.Session;
        if (session?.Started == true || UBot.Core.RuntimeAccess.Core?.Proxy?.ClientConnected == true)
        {
            Log.Warn("[UbotConnectionService] Client is already running. Start Client ignored.");
            return Task.FromResult(false);
        }

        return ConnectCoreAsync("client");
    }

    public Task<bool> GoClientlessAsync()
    {
        var session = UBot.Core.RuntimeAccess.Session;
        if (session?.Clientless == true)
            return Task.FromResult(true);

        if (session == null || !session.Started || UBot.Core.RuntimeAccess.Core.Proxy == null || !UBot.Core.RuntimeAccess.Core.Proxy.IsConnectedToAgentserver)
            return Task.FromResult(false);

        UBot.Core.RuntimeAccess.Global.Set("UBot.General.StayConnected", true);
        UBot.Core.RuntimeAccess.Global.Save();

        ClientlessManager.GoClientless();
        try
        {
            ClientManager.Kill();
        }
        catch
        {
            // ignored
        }

        _clientVisible = false;
        UBot.Core.RuntimeAccess.Global.Set(ConnectionModeKey, "clientless");
        UBot.Core.RuntimeAccess.Global.Save();
        return Task.FromResult(true);
    }

    public Task<bool> ToggleClientVisibilityAsync()
    {
        _clientVisible = !_clientVisible;
        ClientManager.SetVisible(_clientVisible);
        return Task.FromResult(true);
    }

    public Task<bool> SetSroExecutableAsync(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return Task.FromResult(false);

        var cleanedPath = path.Trim().Trim('"');
        if (!File.Exists(cleanedPath))
            return Task.FromResult(false);

        var directory = Path.GetDirectoryName(cleanedPath);
        var executable = Path.GetFileName(cleanedPath);
        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(executable))
            return Task.FromResult(false);

        UBot.Core.RuntimeAccess.Global.Set("UBot.SilkroadDirectory", directory);
        UBot.Core.RuntimeAccess.Global.Set("UBot.SilkroadExecutable", executable);
        UBot.Core.RuntimeAccess.Global.Save();

        _lifecycle.MarkReferenceDataDirty();
        _lifecycle.EnsureClientInfoLoaded();

        return Task.FromResult(true);
    }

    public Task<string> GetSroExecutablePathAsync()
    {
        var directory = UBot.Core.RuntimeAccess.Global.Get("UBot.SilkroadDirectory", string.Empty) ?? string.Empty;
        var executable = UBot.Core.RuntimeAccess.Global.Get("UBot.SilkroadExecutable", string.Empty) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(executable))
            return Task.FromResult(string.Empty);

        return Task.FromResult(Path.Combine(directory, executable));
    }


    private async Task<bool> ConnectCoreAsync(string mode)
    {
        var requestedMode = NormalizeConnectionMode(mode);
        UBot.Core.RuntimeAccess.Global.Set(ConnectionModeKey, requestedMode);

        var options = ResolveConnectionOptions();
        var normalized = NormalizeConnectionIndices(requestedMode, options.divisionIndex, options.gatewayIndex);

        UBot.Core.RuntimeAccess.Global.Set("UBot.DivisionIndex", normalized.divisionIndex);
        UBot.Core.RuntimeAccess.Global.Set("UBot.GatewayIndex", normalized.gatewayIndex);
        UBot.Core.RuntimeAccess.Global.Save();

        if (UBot.Core.RuntimeAccess.Core.Bot != null && UBot.Core.RuntimeAccess.Core.Bot.Running)
            UBot.Core.RuntimeAccess.Core.Bot.Stop();

        UBot.Core.RuntimeAccess.Core.Proxy?.Shutdown();

        var session = UBot.Core.RuntimeAccess.Session;
        if (session != null)
            session.Clientless = requestedMode == "clientless";

        if (!await EnsureReferenceDataReadyAsync().ConfigureAwait(false))
            return false;

        if (session?.ReferenceManager?.DivisionInfo?.Divisions == null
            || session.ReferenceManager.DivisionInfo.Divisions.Count == 0
            || session.ReferenceManager.GatewayInfo == null)
        {
            return false;
        }

        try
        {
            session?.Start();
            if (session?.Clientless != true)
            {
                return await ClientManager.Start();
            }

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(1200).ConfigureAwait(false);
                    ClientlessManager.RequestServerList();
                }
                catch
                {
                }
            });
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async Task<bool> EnsureReferenceDataReadyAsync(int timeoutMs = 45_000)
    {
        if (_lifecycle.ReferenceLoaded)
            return true;

        if (!_lifecycle.EnsureClientInfoLoaded())
            return false;

        if (!_lifecycle.ReferenceLoading)
            _lifecycle.BeginReferenceDataLoad();

        if (_lifecycle.ReferenceLoaded)
            return true;

        var deadline = Environment.TickCount64 + timeoutMs;
        while (Environment.TickCount64 < deadline)
        {
            if (_lifecycle.ReferenceLoaded)
                return true;

            if (!_lifecycle.ReferenceLoading)
                return false;

            await Task.Delay(200).ConfigureAwait(false);
        }

        return _lifecycle.ReferenceLoaded;
    }

    private RuntimeStatus BuildStatusSnapshot()
    {
        var normalized = NormalizeConnectionIndices(
            NormalizeConnectionMode(UBot.Core.RuntimeAccess.Global.Get(ConnectionModeKey, "clientless")),
            UBot.Core.RuntimeAccess.Global.Get("UBot.DivisionIndex", 0),
            UBot.Core.RuntimeAccess.Global.Get("UBot.GatewayIndex", 0));

        var session = UBot.Core.RuntimeAccess.Session;

        return new RuntimeStatus
        {
            BotRunning = UBot.Core.RuntimeAccess.Core.Bot != null && UBot.Core.RuntimeAccess.Core.Bot.Running,
            Profile = ProfileManager.SelectedProfile,
            Server = ResolveServerName(normalized.divisionIndex, normalized.gatewayIndex),
            Character = ResolveCharacterName(),
            StatusText = _lifecycle.StatusText,
            ClientReady = session?.Ready ?? false,
            ClientStarted = session?.Started ?? false,
            ClientConnected = UBot.Core.RuntimeAccess.Core.Proxy != null && UBot.Core.RuntimeAccess.Core.Proxy.ClientConnected,
            GatewayConnected = UBot.Core.RuntimeAccess.Core.Proxy != null && UBot.Core.RuntimeAccess.Core.Proxy.IsConnectedToGatewayserver,
            AgentConnected = UBot.Core.RuntimeAccess.Core.Proxy != null && UBot.Core.RuntimeAccess.Core.Proxy.IsConnectedToAgentserver,
            ReferenceLoading = _lifecycle.ReferenceLoading,
            ReferenceLoaded = _lifecycle.ReferenceLoaded,
            SelectedBotbase = UBot.Core.RuntimeAccess.Core.Bot?.Botbase?.Name,
            ConnectionMode = normalized.mode,
            DivisionIndex = normalized.divisionIndex,
            GatewayIndex = normalized.gatewayIndex,
            Player = BuildPlayerSummary()
        };
    }

    private static PlayerStats BuildPlayerSummary()
    {
        var player = UBot.Core.RuntimeAccess.Session.Player;
        if (player == null)
            return new PlayerStats();

        var maxHealth = Math.Max(0, player.MaximumHealth);
        var maxMana = Math.Max(0, player.MaximumMana);
        var healthPercent = maxHealth > 0 ? Math.Clamp(player.Health * 100.0 / maxHealth, 0, 100) : 0;
        var manaPercent = maxMana > 0 ? Math.Clamp(player.Mana * 100.0 / maxMana, 0, 100) : 0;

        var expPercent = 0d;
        var refLevel = UBot.Core.RuntimeAccess.Session.ReferenceManager?.GetRefLevel(player.Level);
        if (refLevel != null && refLevel.Exp_C > 0)
            expPercent = Math.Clamp(player.Experience * 100.0 / refLevel.Exp_C, 0, 100);

        return new PlayerStats
        {
            Name = player.Name,
            Level = player.Level,
            Health = player.Health,
            MaxHealth = maxHealth,
            HealthPercent = Math.Round(healthPercent, 2),
            Mana = player.Mana,
            MaxMana = maxMana,
            ManaPercent = Math.Round(manaPercent, 2),
            ExperiencePercent = Math.Round(expPercent, 2),
            Gold = (long)player.Gold,
            SkillPoints = (int)player.SkillPoints,
            StatPoints = (int)player.StatPoints,
            InCombat = player.InCombat,
            XOffset = Math.Round(player.Position.XOffset, 2),
            YOffset = Math.Round(player.Position.YOffset, 2)
        };
    }

    private static List<ConnectionDivisionDto> BuildDivisionOptions()
    {
        var divisionInfo = UBot.Core.RuntimeAccess.Session.ReferenceManager?.DivisionInfo;
        if (divisionInfo?.Divisions == null)
            return new List<ConnectionDivisionDto>();

        var result = new List<ConnectionDivisionDto>(divisionInfo.Divisions.Count);
        for (var i = 0; i < divisionInfo.Divisions.Count; i++)
        {
            var division = divisionInfo.Divisions[i];
            var dto = new ConnectionDivisionDto { Index = i, Name = division.Name };
            for (var j = 0; j < (division.GatewayServers?.Count ?? 0); j++)
            {
                dto.Servers.Add(new ConnectionServerDto
                {
                    Index = j,
                    Name = division.GatewayServers[j]
                });
            }

            result.Add(dto);
        }

        return result;
    }

    private static List<ConnectionClientTypeDto> BuildClientTypeOptions()
    {
        return Enum.GetValues<GameClientType>()
            .Select(value => new ConnectionClientTypeDto
            {
                Id = value.ToString(),
                Name = value.ToString(),
                Value = (int)value
            })
            .ToList();
    }

    private static (string mode, int divisionIndex, int gatewayIndex) ResolveConnectionOptions()
    {
        var mode = NormalizeConnectionMode(UBot.Core.RuntimeAccess.Global.Get(ConnectionModeKey, "clientless"));
        var divisionIndex = UBot.Core.RuntimeAccess.Global.Get("UBot.DivisionIndex", 0);
        var gatewayIndex = UBot.Core.RuntimeAccess.Global.Get("UBot.GatewayIndex", 0);
        return (mode, divisionIndex, gatewayIndex);
    }

    private static (string mode, int divisionIndex, int gatewayIndex) NormalizeConnectionIndices(string mode, int divisionIndex, int gatewayIndex)
    {
        var normalizedMode = NormalizeConnectionMode(mode);
        var divisions = UBot.Core.RuntimeAccess.Session.ReferenceManager?.DivisionInfo?.Divisions;
        if (divisions == null || divisions.Count == 0)
            return (normalizedMode, Math.Max(divisionIndex, 0), Math.Max(gatewayIndex, 0));

        if (divisionIndex < 0)
            divisionIndex = 0;
        if (divisionIndex >= divisions.Count)
            divisionIndex = divisions.Count - 1;

        var servers = divisions[divisionIndex].GatewayServers;
        if (servers == null || servers.Count == 0)
            return (normalizedMode, divisionIndex, 0);

        if (gatewayIndex < 0)
            gatewayIndex = 0;
        if (gatewayIndex >= servers.Count)
            gatewayIndex = servers.Count - 1;

        return (normalizedMode, divisionIndex, gatewayIndex);
    }

    private static string NormalizeConnectionMode(string? mode)
    {
        if (string.Equals(mode, "client", StringComparison.OrdinalIgnoreCase))
            return "client";
        return "clientless";
    }

    private static string ResolveCharacterName()
    {
        if (UBot.Core.RuntimeAccess.Session.Player != null && !string.IsNullOrWhiteSpace(UBot.Core.RuntimeAccess.Session.Player.Name))
            return UBot.Core.RuntimeAccess.Session.Player.Name;
        if (!string.IsNullOrWhiteSpace(ProfileManager.SelectedCharacter))
            return ProfileManager.SelectedCharacter;
        return "-";
    }

    private static string ResolveServerName(int divisionIndex, int gatewayIndex)
    {
        var divisions = UBot.Core.RuntimeAccess.Session.ReferenceManager?.DivisionInfo?.Divisions;
        if (divisions == null || divisions.Count == 0 || divisionIndex < 0 || divisionIndex >= divisions.Count)
            return "Unknown";

        var division = divisions[divisionIndex];
        if (division.GatewayServers == null || division.GatewayServers.Count == 0)
            return division.Name;
        if (gatewayIndex >= 0 && gatewayIndex < division.GatewayServers.Count)
            return division.GatewayServers[gatewayIndex];
        return division.GatewayServers[0];
    }
}

