using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Spawn;

namespace UBot.TargetAssist;

internal static class TargetAssistRuntime
{
    private const int EffectTransferParam = 1701213281; // efta
    private const int RetargetCooldownMs = 900;
    private static readonly string[] BloodyStormCodeTokens = { "FANSTORM", "FAN_STORM" };

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private static bool _initialized;
    private static bool _active;
    private static bool _targetCycleKeyWasDown;

    private static int _lastRetargetTick;

    public static void Initialize()
    {
        if (_initialized)
            return;

        EventManager.SubscribeEvent("OnTick", OnTick);
        _initialized = true;
    }

    public static void SetActive(bool active)
    {
        _active = active;
    }

    public static void Reset()
    {
        _lastRetargetTick = 0;
        _targetCycleKeyWasDown = false;
    }

    private static void OnTick()
    {
        if (!_active)
            return;

        if (!Game.Ready || Game.Player == null)
            return;

        if (Game.Player.State.LifeState != LifeState.Alive)
            return;

        var settings = TargetAssistConfig.Load();
        if (!settings.Enabled)
            return;

        HandleTargetCycleHotkey(settings);
    }

    private static void HandleTargetCycleHotkey(TargetAssistSettings settings)
    {
        var state = GetAsyncKeyState((int)settings.TargetCycleKey);
        var targetKeyDown = (state & 0x8000) != 0;

        if (!targetKeyDown)
        {
            _targetCycleKeyWasDown = false;
            return;
        }

        if (_targetCycleKeyWasDown)
            return;

        _targetCycleKeyWasDown = true;

        if (!CanRetargetNow())
            return;

        var candidates = GetCandidatesOrderedByDistance(settings);
        if (candidates.Count == 0)
            return;

        var nextTarget = ResolveNextTarget(candidates);
        if (nextTarget == null)
            return;

        TrySelectTarget(nextTarget);
    }

    private static bool CanRetargetNow()
    {
        return Kernel.TickCount - _lastRetargetTick >= RetargetCooldownMs;
    }

    private static List<SpawnedPlayer> GetCandidatesOrderedByDistance(TargetAssistSettings settings)
    {
        if (!SpawnManager.TryGetEntities<SpawnedPlayer>(out var players))
            return new List<SpawnedPlayer>();

        return players
            .Where(player => IsValidTarget(player, settings))
            .OrderBy(player => player.DistanceToPlayer)
            .ToList();
    }

    private static SpawnedPlayer ResolveNextTarget(List<SpawnedPlayer> orderedCandidates)
    {
        if (orderedCandidates == null || orderedCandidates.Count == 0)
            return null;

        var currentSelectedId = Game.SelectedEntity?.UniqueId ?? 0;
        if (currentSelectedId == 0)
            return orderedCandidates[0];

        var selectedIndex = orderedCandidates.FindIndex(candidate => candidate.UniqueId == currentSelectedId);
        if (selectedIndex < 0)
            return orderedCandidates[0];

        var nextIndex = (selectedIndex + 1) % orderedCandidates.Count;
        return orderedCandidates[nextIndex];
    }

    private static bool TrySelectTarget(SpawnedPlayer target)
    {
        if (target == null)
            return false;

        if (Game.SelectedEntity?.UniqueId == target.UniqueId)
            return true;

        var selected = target.TrySelect();
        if (!selected)
        {
            // Fallback: some runtimes may miss/timeout the select callback.
            var packet = new Packet(0x7045);
            packet.WriteUInt(target.UniqueId);
            PacketManager.SendPacket(packet, PacketDestination.Server);
            Game.SelectedEntity = target;
            selected = true;
        }

        if (selected)
            _lastRetargetTick = Kernel.TickCount;

        return selected;
    }

    private static bool IsValidTarget(SpawnedPlayer player, TargetAssistSettings settings)
    {
        if (player == null || Game.Player == null)
            return false;

        if (player.UniqueId == Game.Player.UniqueId)
            return false;

        if (string.IsNullOrWhiteSpace(player.Name))
            return false;

        if (!settings.IncludeDeadTargets && player.State.LifeState != LifeState.Alive)
            return false;

        if (player.DistanceToPlayer > settings.MaxRange)
            return false;

        if (settings.IgnoreSnowShieldTargets && HasSnowShieldBuff(player))
            return false;

        if (settings.IgnoreBloodyStormTargets && HasAnyBuffCodeToken(player, BloodyStormCodeTokens))
            return false;

        if (IsIgnoredGuild(player, settings))
            return false;

        if (settings.OnlyCustomPlayers && !IsCustomPlayer(player, settings))
            return false;

        if (!MatchesRoleMode(player, settings.RoleMode))
            return false;

        return true;
    }

    private static bool IsIgnoredGuild(SpawnedPlayer player, TargetAssistSettings settings)
    {
        var guildName = player.Guild?.Name;
        if (string.IsNullOrWhiteSpace(guildName))
            return false;

        return settings.IgnoredGuildSet.Contains(guildName.Trim());
    }

    private static bool IsCustomPlayer(SpawnedPlayer player, TargetAssistSettings settings)
    {
        if (player == null || string.IsNullOrWhiteSpace(player.Name))
            return false;

        return settings.CustomPlayerSet.Contains(player.Name.Trim());
    }

    private static bool MatchesRoleMode(SpawnedPlayer player, TargetAssistRoleMode roleMode)
    {
        return roleMode switch
        {
            TargetAssistRoleMode.Thief => player.WearsJobSuite
                && (player.Job == JobType.Hunter || player.Job == JobType.Trade),
            TargetAssistRoleMode.HunterTrader => player.WearsJobSuite && player.Job == JobType.Thief,
            _ => true
        };
    }

    private static bool HasSnowShieldBuff(SpawnedPlayer player)
    {
        if (player?.State?.ActiveBuffs == null)
            return false;

        foreach (var buff in player.State.ActiveBuffs)
        {
            var record = buff?.Record;
            if (record == null)
                continue;

            var code = record.Basic_Code;
            if (!string.IsNullOrWhiteSpace(code) && code.IndexOf("COLD_SHIELD", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (record.Params.Contains(EffectTransferParam))
                return true;
        }

        return false;
    }

    private static bool HasAnyBuffCodeToken(SpawnedPlayer player, IEnumerable<string> tokens)
    {
        if (player?.State?.ActiveBuffs == null || tokens == null)
            return false;

        foreach (var buff in player.State.ActiveBuffs)
        {
            var code = buff?.Record?.Basic_Code;
            if (string.IsNullOrWhiteSpace(code))
                continue;

            foreach (var token in tokens)
            {
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                if (code.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
        }

        return false;
    }
}
