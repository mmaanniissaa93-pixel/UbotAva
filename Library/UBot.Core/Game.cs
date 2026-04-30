#nullable enable annotations

using System;
using System.IO;
using UBot.Core.Client;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Extensions;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Party;
using UBot.Core.Objects.Spawn;
using UBot.Core.Services;
using UBot.FileSystem;

namespace UBot.Core;

public class Game
{
    /// <summary>
    ///     The acceptance request
    /// </summary>
    public static AcceptanceRequest AcceptanceRequest;

    /// <summary>
    /// Gets or sets the MAC address.
    /// </summary>
    /// <value>
    ///     The MAC address.
    /// </value>
    public static byte[] MacAddress { get; set; }

    /// <summary>
    ///     Gets or sets the port.
    /// </summary>
    /// <value>
    ///     The port.
    /// </value>
    public static ushort Port { get; internal set; }

    /// <summary>
    ///     Gets or sets the Media.pk2 reader.
    /// </summary>
    /// <value>
    ///     The PK2 reader.
    /// </value>
    public static IFileSystem MediaPk2 { get; set; }

    /// <summary>
    ///     Gets or sets the Data.pk2 reader.
    /// </summary>
    /// <value>
    ///     The PK2 reader.
    /// </value>
    public static IFileSystem DataPk2 { get; set; }

    /// <summary>
    ///     Gets or sets the reference manager
    /// </summary>
    /// <value>
    ///     The reference manager
    /// </value>
    public static ReferenceManager ReferenceManager { get; set; }

    /// <summary>
    ///     Gets the character.
    /// </summary>
    /// <value>
    ///     The character.
    /// </value>
    public static Player Player { get; internal set; }

    /// <summary>
    ///     Gets or sets the selected entity.
    /// </summary>
    /// <value>
    ///     The selected entity.
    /// </value>
    public static SpawnedBionic? SelectedEntity { get; set; }

    /// <summary>
    ///     Gets or sets the spawn information.
    /// </summary>
    /// <value>
    ///     The spawn information.
    /// </value>
    internal static SpawnPacketInfo SpawnInfo { get; set; }

    /// <summary>
    ///     Gets or sets the chunked packet.
    /// </summary>
    /// <value>
    ///     The chunked packet.
    /// </value>
    internal static Packet ChunkedPacket { get; set; }

    /// <summary>
    ///     Gets or sets the party.
    /// </summary>
    /// <value>
    ///     The party.
    /// </value>
    public static Party Party { get; internal set; }

    /// <summary>
    ///     Gets a value indicating whether this <see cref="Game" /> is clientless.
    /// </summary>
    /// <value>
    ///     <c>true</c> if clientless; otherwise, <c>false</c>.
    /// </value>
    public static bool Clientless { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this <see cref="Game" /> is started.
    /// </summary>
    /// <value><c>true</c> if started; otherwise, <c>false</c>.</value>
    public static bool Started { get; set; }

    /// <summary>
    ///     Gets a value indicating whether this <see cref="Game" /> is ready.
    /// </summary>
    /// <value>
    ///     <c>true</c> if ready; otherwise, <c>false</c>.
    /// </value>
    public static bool Ready { get; internal set; }

    /// <summary>
    ///     The game client type
    /// </summary>
    public static GameClientType ClientType { get; set; }

    /// <summary>
    ///     Starts the game.
    /// </summary>
    public static void Start()
    {
        Runtime.GameSession.Shared.Start();
    }

    /// <summary>
    ///     Initialize game archive files
    /// </summary>
    /// <returns></returns>
    public static bool InitializeArchiveFiles()
    {
        return Runtime.GameSession.Shared.InitializeArchiveFiles();
    }

    /// <summary>
    ///     Initializes this instance.
    /// </summary>
    public static void Initialize()
    {
        Runtime.GameSession.Shared.Initialize();
    }

    /// <summary>
    ///     Shows a notification in the game client using the notice chat type.
    /// </summary>
    /// <param name="message"></param>
    public static void ShowNotification(string message)
    {
        Runtime.GameSession.Shared.ShowNotification(message);
    }
}
