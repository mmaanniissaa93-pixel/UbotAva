using UBot.Alchemy.Bot;
using UBot.Alchemy.Views;

namespace UBot.Alchemy;

internal class Globals
{
    #region Properties

    /// <summary>
    ///     Gets or sets the view
    /// </summary>
    /// <value>
    ///     The view.
    /// </value>
    public static Main View { get; } = new();

    /// <summary>
    ///     Gets or sets the bot base
    /// </summary>
    public static Botbase Botbase { get; set; }

    #endregion Properties
}
