using UBot.Core;
using UBot.Core.Network;
using UBot.Protocol.Legacy;

namespace UBot.Core.Objects;

public class NpcTalk
{
    /// <summary>
    ///     Gets or sets the talk flag.
    /// </summary>
    /// <value>
    ///     The talk flag.
    /// </value>
    public byte Flag { get; set; }

    /// <summary>
    ///     Gets or sets the talk options.
    /// </summary>
    /// <value>
    ///     The talk options.
    /// </value>
    public byte[] Options { get; set; }

    /// <summary>
    ///     Deserialize from the packet
    /// </summary>
    /// <param name="packet">The packet</param>
    public void Deserialize(Packet packet)
    {
        Flag = packet.ReadByte();

        if ((Flag & 2) > 0)
        {
            var count = 4;
            if (LegacyGame.ClientType > GameClientType.Thailand)
                count = packet.ReadByte();

            if (
                LegacyGame.ClientType == GameClientType.Global
                || LegacyGame.ClientType == GameClientType.Turkey
                || LegacyGame.ClientType == GameClientType.VTC_Game
                || LegacyGame.ClientType == GameClientType.RuSro
                || LegacyGame.ClientType == GameClientType.Korean
                || LegacyGame.ClientType == GameClientType.Japanese
                || LegacyGame.ClientType == GameClientType.Taiwan
            )
                count = 7;

            Options = packet.ReadBytes(count);
        }

        // pandora box, after spawned mobs
        if (Flag == 6)
            if (packet.ReadByte() == 1) // maybe
                packet.ReadUInt();
    }
}
