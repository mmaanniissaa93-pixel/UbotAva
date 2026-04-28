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

        return Task.FromResult(new ConnectionOptions
        {
            Mode = normalized.mode,
            DivisionIndex = normalized.divisionIndex,
            GatewayIndex = normalized.gatewayIndex,
            Divisions = BuildDivisionOptions(),
            ClientType = GlobalConfig.GetEnum("UBot.Game.ClientType", Game.ClientType).ToString(),
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
                GlobalConfig.Set(ConnectionModeKey, normalizedMode);
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
            var currentClientType = GlobalConfig.GetEnum("UBot.Game.ClientType", Game.ClientType);
            if (requestedClientType != currentClientType && !Game.Ready && !_lifecycle.ReferenceLoading)
            {
                GlobalConfig.Set("UBot.Game.ClientType", requestedClientType);
                Game.ClientType = requestedClientType;
                _lifecycle.MarkReferenceDataDirty();
                changed = true;
            }
        }

        var normalized = NormalizeConnectionIndices(options.mode, options.divisionIndex, options.gatewayIndex);
        GlobalConfig.Set(ConnectionModeKey, normalized.mode);
        GlobalConfig.Set("UBot.DivisionIndex", normalized.divisionIndex);
        GlobalConfig.Set("UBot.GatewayIndex", normalized.gatewayIndex);
        if (changed)
            GlobalConfig.Save();

        return await GetConnectionOptionsAsync();
    }


    public Task<RuntimeStatus> StartBotAsync()
    {
        if (Kernel.Bot?.Botbase == null)
            return Task.FromResult(BuildStatusSnapshot());

        if (Kernel.Proxy == null || !Kernel.Proxy.IsConnectedToAgentserver)
            return Task.FromResult(BuildStatusSnapshot());

        if (Game.Player == null)
            return Task.FromResult(BuildStatusSnapshot());

        if (!Kernel.Bot.Running)
            Kernel.Bot.Start();

        return Task.FromResult(BuildStatusSnapshot());
    }

    public Task<RuntimeStatus> StopBotAsync()
    {
        if (Kernel.Bot != null && Kernel.Bot.Running)
            Kernel.Bot.Stop();

        return Task.FromResult(BuildStatusSnapshot());
    }

    public Task<RuntimeStatus> DisconnectAsync()
    {
        if (Kernel.Bot != null && Kernel.Bot.Running)
            Kernel.Bot.Stop();

        try
        {
            Kernel.Proxy?.Shutdown();
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

        Game.Started = false;
        return Task.FromResult(BuildStatusSnapshot());
    }

    public Task SaveConfigAsync()
    {
        GlobalConfig.Save();
        PlayerConfig.Save();
        return Task.CompletedTask;
    }

    public Task<bool> StartClientAsync()
    {
        return ConnectCoreAsync("client");
    }

    public Task<bool> GoClientlessAsync()
    {
        if (Game.Clientless)
            return Task.FromResult(true);

        if (!Game.Started || Kernel.Proxy == null || !Kernel.Proxy.IsConnectedToAgentserver)
            return Task.FromResult(false);

        GlobalConfig.Set("UBot.General.StayConnected", true);
        GlobalConfig.Save();

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
        GlobalConfig.Set(ConnectionModeKey, "clientless");
        GlobalConfig.Save();
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

        GlobalConfig.Set("UBot.SilkroadDirectory", directory);
        GlobalConfig.Set("UBot.SilkroadExecutable", executable);
        GlobalConfig.Save();

        _lifecycle.MarkReferenceDataDirty();
        _lifecycle.EnsureClientInfoLoaded();

        return Task.FromResult(true);
    }

    public Task<string> GetSroExecutablePathAsync()
    {
        var directory = GlobalConfig.Get("UBot.SilkroadDirectory", string.Empty) ?? string.Empty;
        var executable = GlobalConfig.Get("UBot.SilkroadExecutable", string.Empty) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(executable))
            return Task.FromResult(string.Empty);

        return Task.FromResult(Path.Combine(directory, executable));
    }


    private async Task<bool> ConnectCoreAsync(string mode)
    {
        var requestedMode = NormalizeConnectionMode(mode);
        GlobalConfig.Set(ConnectionModeKey, requestedMode);

        var options = ResolveConnectionOptions();
        var normalized = NormalizeConnectionIndices(requestedMode, options.divisionIndex, options.gatewayIndex);

        GlobalConfig.Set("UBot.DivisionIndex", normalized.divisionIndex);
        GlobalConfig.Set("UBot.GatewayIndex", normalized.gatewayIndex);
        GlobalConfig.Save();

        if (Kernel.Bot != null && Kernel.Bot.Running)
            Kernel.Bot.Stop();

        Kernel.Proxy?.Shutdown();
        Game.Clientless = requestedMode == "clientless";

        if (!await EnsureReferenceDataReadyAsync().ConfigureAwait(false))
            return false;

        if (Game.ReferenceManager?.DivisionInfo?.Divisions == null
            || Game.ReferenceManager.DivisionInfo.Divisions.Count == 0
            || Game.ReferenceManager.GatewayInfo == null)
        {
            return false;
        }

        try
        {
            Game.Start();
            if (!Game.Clientless)
            {
                return await ClientManager.Start();
            }

            _ = Task.Run(async () =>
            {
                await Task.Delay(1200).ConfigureAwait(false);
                ClientlessManager.RequestServerList();
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
            NormalizeConnectionMode(GlobalConfig.Get(ConnectionModeKey, "clientless")),
            GlobalConfig.Get("UBot.DivisionIndex", 0),
            GlobalConfig.Get("UBot.GatewayIndex", 0));

        return new RuntimeStatus
        {
            BotRunning = Kernel.Bot != null && Kernel.Bot.Running,
            Profile = ProfileManager.SelectedProfile,
            Server = ResolveServerName(normalized.divisionIndex, normalized.gatewayIndex),
            Character = ResolveCharacterName(),
            StatusText = _lifecycle.StatusText,
            ClientReady = Game.Ready,
            ClientStarted = Game.Started,
            ClientConnected = Kernel.Proxy != null && Kernel.Proxy.ClientConnected,
            GatewayConnected = Kernel.Proxy != null && Kernel.Proxy.IsConnectedToGatewayserver,
            AgentConnected = Kernel.Proxy != null && Kernel.Proxy.IsConnectedToAgentserver,
            ReferenceLoading = _lifecycle.ReferenceLoading,
            ReferenceLoaded = _lifecycle.ReferenceLoaded,
            SelectedBotbase = Kernel.Bot?.Botbase?.Name,
            ConnectionMode = normalized.mode,
            DivisionIndex = normalized.divisionIndex,
            GatewayIndex = normalized.gatewayIndex,
            Player = BuildPlayerSummary()
        };
    }

    private static PlayerStats BuildPlayerSummary()
    {
        var player = Game.Player;
        if (player == null)
            return new PlayerStats();

        var maxHealth = Math.Max(0, player.MaximumHealth);
        var maxMana = Math.Max(0, player.MaximumMana);
        var healthPercent = maxHealth > 0 ? Math.Clamp(player.Health * 100.0 / maxHealth, 0, 100) : 0;
        var manaPercent = maxMana > 0 ? Math.Clamp(player.Mana * 100.0 / maxMana, 0, 100) : 0;

        var expPercent = 0d;
        var refLevel = Game.ReferenceManager?.GetRefLevel(player.Level);
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
        var divisionInfo = Game.ReferenceManager?.DivisionInfo;
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
        var mode = NormalizeConnectionMode(GlobalConfig.Get(ConnectionModeKey, "clientless"));
        var divisionIndex = GlobalConfig.Get("UBot.DivisionIndex", 0);
        var gatewayIndex = GlobalConfig.Get("UBot.GatewayIndex", 0);
        return (mode, divisionIndex, gatewayIndex);
    }

    private static (string mode, int divisionIndex, int gatewayIndex) NormalizeConnectionIndices(string mode, int divisionIndex, int gatewayIndex)
    {
        var normalizedMode = NormalizeConnectionMode(mode);
        var divisions = Game.ReferenceManager?.DivisionInfo?.Divisions;
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
        if (Game.Player != null && !string.IsNullOrWhiteSpace(Game.Player.Name))
            return Game.Player.Name;
        if (!string.IsNullOrWhiteSpace(ProfileManager.SelectedCharacter))
            return ProfileManager.SelectedCharacter;
        return "-";
    }

    private static string ResolveServerName(int divisionIndex, int gatewayIndex)
    {
        var divisions = Game.ReferenceManager?.DivisionInfo?.Divisions;
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

