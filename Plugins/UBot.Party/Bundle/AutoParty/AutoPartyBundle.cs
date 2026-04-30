using System;
using System.Collections.Generic;
using System.Linq;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Objects.Party;
using UBot.Core.Objects.Spawn;
using UBot.Party.Bundle.PartyMatching.Objects;

namespace UBot.Party.Bundle.AutoParty;

internal class AutoPartyBundle
{
    private static readonly object EventOwner = new();

    /// <summary>
    ///     Last tick for checking party members
    /// </summary>
    private int _lastPartyListingCacheTick;

    /// <summary>
    ///     Last tick for checking party members
    /// </summary>
    private int _lastTick;

    /// <summary>
    ///     Party entiries cache
    /// </summary>
    private List<PartyEntry> _partyEntriesCache;

    /// <summary>
    ///     Initialize this instance
    /// </summary>
    public AutoPartyBundle()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnTick", OnTick, EventOwner);
    }

    /// <summary>
    ///     Unsubscribes all events.
    /// </summary>
    public void UnsubscribeAll()
    {
        UBot.Core.RuntimeAccess.Events.UnsubscribeOwner(EventOwner);
    }

    /// <summary>
    ///     Gets or sets the configuration.
    /// </summary>
    /// <value>
    ///     The configuration.
    /// </value>
    public AutoPartyConfig Config { get; set; }

    /// <summary>
    ///     Refreshes this instance.
    /// </summary>
    public void Refresh()
    {
        Config = new AutoPartyConfig
        {
            PlayerList = UBot.Core.RuntimeAccess.Player.GetArray<string>("UBot.Party.AutoPartyList"),
            InviteAll = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Party.InviteAll"),
            AcceptAll = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Party.AcceptAll"),
            AcceptFromList = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Party.AcceptList"),
            InviteFromList = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Party.InviteList"),
            OnlyAtTrainingPlace = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Party.AtTrainingPlace"),
            ExperienceAutoShare = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Party.EXPAutoShare", true),
            ItemAutoShare = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Party.ItemAutoShare", true),
            AllowInvitations = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Party.AllowInvitations", true),
            AcceptIfBotIsStopped = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Party.AcceptIfBotStopped"),
            LeaveIfMasterNot = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Party.LeaveIfMasterNot"),
            LeaveIfMasterNotName = UBot.Core.RuntimeAccess.Player.Get<string>("UBot.Party.LeaveIfMasterNotName"),
            CenterPosition = UBot.Core.RuntimeAccess.Core.Bot.Botbase.Area.Position,
            AutoJoinByName = UBot.Core.RuntimeAccess.Player.Get("UBot.Party.AutoJoin.ByName", false),
            AutoJoinByTitle = UBot.Core.RuntimeAccess.Player.Get("UBot.Party.AutoJoin.ByTitle", false),
            AutoJoinByNameContent = UBot.Core.RuntimeAccess.Player.Get("UBot.Party.AutoJoin.Name", string.Empty),
            AutoJoinByTitleContent = UBot.Core.RuntimeAccess.Player.Get("UBot.Party.AutoJoin.Title", string.Empty),
            AlwaysFollowThePartyMaster = UBot.Core.RuntimeAccess.Player.Get("UBot.Party.AlwaysFollowPartyMaster", false),
        };

        if (!UBot.Core.RuntimeAccess.Session.Party.IsInParty)
            UBot.Core.RuntimeAccess.Session.Party.Settings = new PartySettings(
                Config.ExperienceAutoShare,
                Config.ItemAutoShare,
                Config.AllowInvitations
            );
    }

    public void OnTick()
    {
        if (!UBot.Core.RuntimeAccess.Session.Ready)
            return;

        var elapsed = UBot.Core.RuntimeAccess.Core.TickCount - _lastTick;
        if (elapsed > 5000)
        {
            CheckForAutoPartyJoin();
            CheckForPlayers();

            _lastTick = UBot.Core.RuntimeAccess.Core.TickCount;
        }
    }

    /// <summary>
    ///     Checks for auto party join by condition
    /// </summary>
    private void CheckForAutoPartyJoin()
    {
        if (UBot.Core.RuntimeAccess.Session.Party.IsInParty || Config == null)
            return;

        if (!Config.AutoJoinByName && !Config.AutoJoinByTitle)
            return;

        var elapsed = UBot.Core.RuntimeAccess.Core.TickCount - _lastPartyListingCacheTick;

        // every one minute
        if (elapsed >= 60000)
        {
            _partyEntriesCache = new List<PartyEntry>(64);

            byte page = 0;
            while (true)
            {
                var currentPage = Container.PartyMatching.RequestPartyList(page);

                _partyEntriesCache.AddRange(currentPage.Parties);

                if (currentPage.Page == currentPage.PageCount - 1)
                    break;

                page++;
            }

            _lastPartyListingCacheTick = UBot.Core.RuntimeAccess.Core.TickCount;
        }

        if (Config.AutoJoinByName)
        {
            var partyEntry = _partyEntriesCache.Find(p => p.Leader == Config.AutoJoinByNameContent);
            if (partyEntry == null)
                return;

            if (Container.PartyMatching.Join(partyEntry.Id))
                return;
        }

        if (Config.AutoJoinByTitle)
        {
            var partyEntry = _partyEntriesCache.Find(p =>
                p.Title.Contains(Config.AutoJoinByTitleContent, StringComparison.CurrentCultureIgnoreCase)
            );
            if (partyEntry == null)
                return;

            Container.PartyMatching.Join(partyEntry.Id);
        }
    }

    /// <summary>
    ///     Checks for players that can be invited.
    /// </summary>
    public void CheckForPlayers()
    {
        if (
            UBot.Core.RuntimeAccess.Session.Party.IsInParty
            && !UBot.Core.RuntimeAccess.Session.Party.IsLeader
            && Config.LeaveIfMasterNot
            && !string.IsNullOrWhiteSpace(Config.LeaveIfMasterNotName)
        )
            if (Config.LeaveIfMasterNotName != UBot.Core.RuntimeAccess.Session.Party.Leader.Name)
                UBot.Core.RuntimeAccess.Session.Party.Leave();

        // Don't try to invite if we can't invite
        if (!UBot.Core.RuntimeAccess.Session.Party.CanInvite)
            return;

        // Don't invite if both InviteAll and InviteFromList are disabled
        if (!Config.InviteAll && !Config.InviteFromList)
            return;

        var limit = 8;
        if (!UBot.Core.RuntimeAccess.Session.Party.Settings.ExperienceAutoShare && !UBot.Core.RuntimeAccess.Session.Party.Settings.ItemAutoShare)
            limit = 4;

        // Only check party member count if already in a party
        if (UBot.Core.RuntimeAccess.Session.Party.IsInParty && UBot.Core.RuntimeAccess.Session.Party.Members?.Count >= limit)
            return;

        if (Config.OnlyAtTrainingPlace && UBot.Core.RuntimeAccess.Session.Player.Movement.Source.DistanceTo(Config.CenterPosition) > 50)
            return;

        if (!SpawnManager.TryGetEntities<SpawnedPlayer>(out var players))
            return;

        foreach (var player in players)
        {
            // Skip if player is already in our party
            if (UBot.Core.RuntimeAccess.Session.Party.IsInParty && UBot.Core.RuntimeAccess.Session.Party.GetMemberByName(player.Name) != null)
                continue;

            // Skip ourselves
            if (player.Name == UBot.Core.RuntimeAccess.Session.Player.Name)
                continue;

            if (Config.InviteAll)
            {
                UBot.Core.RuntimeAccess.Session.Party.Invite(player.UniqueId);
                continue;
            }

            if (Config.InviteFromList && Config.PlayerList.Contains(player.Name))
                UBot.Core.RuntimeAccess.Session.Party.Invite(player.UniqueId);
        }
    }
}
