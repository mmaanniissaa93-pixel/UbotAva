using UBot.Core;
using UBot.Core.Objects.Party;

namespace UBot.Party.Bundle.PartyMatching.Objects;

internal struct MatchingConfig
{
    /// <summary>
    ///     Gets or sets a value indicating whether [experience automatic share].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [experience automatic share]; otherwise, <c>false</c>.
    /// </value>
    public bool ExperienceAutoShare
    {
        get => UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Party.EXPAutoShare");
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Party.EXPAutoShare", value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [item automatic share].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [item automatic share]; otherwise, <c>false</c>.
    /// </value>
    public bool ItemAutoShare
    {
        get => UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Party.ItemAutoShare");
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Party.ItemAutoShare", value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [allow invitations].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [allow invitations]; otherwise, <c>false</c>.
    /// </value>
    public bool AllowInvitations
    {
        get => UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Party.AllowInvitations");
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Party.AllowInvitations", value);
    }

    /// <summary>
    ///     Gets or sets the purpose.
    /// </summary>
    /// <value>The purpose.</value>
    public PartyPurpose Purpose
    {
        get => (PartyPurpose)UBot.Core.RuntimeAccess.Player.Get<byte>("UBot.Party.Matching.Purpose");
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Party.Matching.Purpose", (byte)value);
    }

    /// <summary>
    ///     Gets or sets the title.
    /// </summary>
    /// <value>The title.</value>
    public string Title
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Party.Matching.Title", "For opening hunting on the silkroad!");
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Party.Matching.Title", value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [automatic reform].
    /// </summary>
    /// <value><c>true</c> if [automatic reform]; otherwise, <c>false</c>.</value>
    public bool AutoReform
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Party.Matching.AutoReform", false);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Party.Matching.AutoReform", value);
    }

    /// <summary>
    ///     Gets or sets a value indicating whether [automatic accept from matching invite].
    /// </summary>
    /// <value><c>true</c> if [automatic accept  from matching invite]; otherwise, <c>false</c>.</value>
    public bool AutoAccept
    {
        get => UBot.Core.RuntimeAccess.Player.Get("UBot.Party.Matching.AutoAccept", true);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Party.Matching.AutoAccept", value);
    }

    /// <summary>
    ///     Gets or sets the level from.
    /// </summary>
    /// <value>The level from.</value>
    public byte LevelFrom
    {
        get => UBot.Core.RuntimeAccess.Player.Get<byte>("UBot.Party.Matching.LevelFrom", 1);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Party.Matching.LevelFrom", value);
    }

    /// <summary>
    ///     Gets or sets the level to.
    /// </summary>
    /// <value>The level to.</value>
    public byte LevelTo
    {
        get => UBot.Core.RuntimeAccess.Player.Get<byte>("UBot.Party.Matching.LevelTo", 140);
        set => UBot.Core.RuntimeAccess.Player.Set("UBot.Party.Matching.LevelTo", value);
    }
}
