using System.Collections.Generic;
using System.Linq;
using UBot.Core.Abstractions;

namespace UBot.Core.Objects.Party;

public class Party
{
    private readonly IGameStateRuntimeContext _context;

    public Party(IGameStateRuntimeContext context = null)
    {
        _context = context ?? GameStateRuntimeProvider.Instance;
    }

    /// <summary>
    ///     Gets a value indicating whether other party members can invite or not.
    /// </summary>
    /// <value>
    ///     <c>true</c> Indicating whether other party members can invite; otherwise, <c>false</c>.
    /// </value>
    public bool CanInvite => (Settings?.AllowInvitation == true) || IsLeader || !IsInParty;

    /// <summary>
    ///     Gets a value indicating whether this instance has pending request.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance has pending request; otherwise, <c>false</c>.
    /// </value>
    public bool HasPendingRequest => _context.HasPendingPartyRequest();

    /// <summary>
    ///     Gets a value indicating whether the current player is the party leader or not.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is in party; otherwise, <c>false</c>.
    /// </value>
    public bool IsInParty => Members != null && Members.Count > 0;

    /// <summary>
    ///     Gets a value indicating whether this instance is leader.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is leader; otherwise, <c>false</c>.
    /// </value>
    public bool IsLeader => Leader?.Name != null && _context.PlayerName != null && Leader.Name == _context.PlayerName;

    /// <summary>
    ///     Gets or sets the leader.
    /// </summary>
    /// <value>
    ///     The leader.
    /// </value>
    public PartyMember Leader { get; set; }

    /// <summary>
    ///     Gets or sets the members.
    /// </summary>
    /// <value>
    ///     The members.
    /// </value>
    public List<PartyMember> Members { get; set; }

    /// <summary>
    ///     Gets or sets the settings.
    /// </summary>
    /// <value>
    ///     The settings.
    /// </value>
    public PartySettings Settings { get; set; }

    /// <summary>
    ///     Gets the member by identifier.
    /// </summary>
    /// <param name="memberId">The member identifier.</param>
    /// <returns></returns>
    public PartyMember GetMemberById(uint memberId)
    {
        return Members.FirstOrDefault(m => m.MemberId == memberId);
    }

    /// <summary>
    ///     Gets the name of the member by.
    /// </summary>
    /// <param name="playerName">Name of the player.</param>
    /// <returns></returns>
    public PartyMember GetMemberByName(string playerName)
    {
        return Members?.FirstOrDefault(m => m.Name == playerName);
    }

    /// <summary>
    ///     Invites the specified player unique identifier.
    /// </summary>
    /// <param name="playerUniqueId">The player unique identifier.</param>
    public void Invite(uint playerUniqueId)
    {
        _context.SendPartyInvite(playerUniqueId, IsInParty, Settings.GetPartyType());
    }

    /// <summary>
    ///     Leaves the current party.
    /// </summary>
    public void Leave()
    {
        _context.SendPartyLeave();
    }

    /// <summary>
    ///     Clears this instance.
    /// </summary>
    internal void Clear()
    {
        Members = null;
        Leader = null;
    }
}
