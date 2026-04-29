using System.Collections.Generic;
using UBot.Core.Components;
using UBot.Core.Network;

namespace UBot.Core.Objects.Exchange;

internal static class ExchangeInstancePacketExtensions
{
    internal static void UpdateItems(this ExchangeInstance exchange, Packet packet, uint playerUniqueId)
    {
        var ownerUniqueId = packet.ReadUInt();
        var playerIsSender = ownerUniqueId == playerUniqueId;
        var items = new List<ExchangeItem>(12);

        var itemCount = packet.ReadByte();
        for (var i = 0; i < itemCount; i++)
        {
            var item = ExchangeItem.FromPacket(packet, playerIsSender);

            if (item.Item == null)
            {
                Log.Debug($"Could not detect item at exchange slot #{item.ExchangeSlot}");
                continue;
            }

            items.Add(item);
        }

        exchange.SetItems(playerIsSender, items);
    }
}
