using System.Collections.Generic;

namespace UBot.General.Models;

public class Account
{
    /// <summary>
    ///     Gets or sets the username.
    /// </summary>
    public string Username { get; set; }

    /// <summary>
    ///     Gets or sets the password.
    /// </summary>
    public string Password { get; set; }

    /// <summary>
    ///     Gets or sets the secondary password.
    /// </summary>
    public string SecondaryPassword { get; set; }

    /// <summary>
    ///     Gets or sets the channel.
    /// </summary>
    public byte Channel { get; set; } = 1;

    /// <summary>
    ///     Gets or sets the account type (Joymax/Global).
    /// </summary>
    public string Type { get; set; } = "Joymax";

    /// <summary>
    ///     Gets or sets the server name.
    /// </summary>
    public string ServerName { get; set; }

    /// <summary>
    ///     Gets or sets the selected character.
    /// </summary>
    public string SelectedCharacter { get; set; }

    /// <summary>
    ///     Gets or sets the list of characters.
    /// </summary>
    public List<string> Characters { get; set; }

    /// <summary>
    ///     Return the username instead of the type name.
    /// </summary>
    public override string ToString()
    {
        return Username;
    }
}
