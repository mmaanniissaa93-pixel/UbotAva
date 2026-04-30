using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using System.Collections.Generic;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Job;

public class JobUpdatePriceResponse : IPacketHandler 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x30E0;

    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Client;

    /// <summary>
    ///     Handles the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    public void Invoke(Packet packet)
    {
        var itemCount = packet.ReadByte();

        CoreGame.Player.TradeInfo.Prices = new Dictionary<uint, uint>(itemCount);

        for (var i = 0; i < itemCount; i++)
        {
            var itemId = packet.ReadUInt();
            var sellPrice = packet.ReadUInt();

            CoreGame.Player.TradeInfo.Prices.Add(itemId, sellPrice);
        }

        UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnUpdateJobPrices");
    }
}





