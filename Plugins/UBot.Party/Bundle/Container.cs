using UBot.Party.Bundle.AutoParty;
using UBot.Party.Bundle.Commands;
using UBot.Party.Bundle.PartyMatching;

namespace UBot.Party.Bundle;

internal static class Container
{
    /// <summary>
    ///     Gets or sets the automatic party.
    /// </summary>
    /// <value>
    ///     The automatic party.
    /// </value>
    public static AutoPartyBundle AutoParty { get; set; }

    /// <summary>
    ///     Gets or sets the party matching.
    /// </summary>
    /// <value>
    ///     The party matching.
    /// </value>
    public static PartyMatchingBundle PartyMatching { get; set; }

    /// <summary>
    ///     Gets or sets the party matching.
    /// </summary>
    /// <value>
    ///     The party matching.
    /// </value>
    public static CommandsBundle Commands { get; set; }

    /// <summary>
    ///     Refreshes this instance.
    /// </summary>
    public static void Refresh()
    {
        AutoParty ??= new AutoPartyBundle();
        PartyMatching ??= new PartyMatchingBundle();
        Commands ??= new CommandsBundle();

        AutoParty.Refresh();
        Commands.Refresh();
    }
}
