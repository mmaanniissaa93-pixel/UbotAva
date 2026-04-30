using System.Collections.Generic;
using UBot.Core.Network;
using UBot.Protocol.Legacy;

namespace UBot.Core.Objects.Exchange;

public static class ExchangeInstancePacketExtensions
{
    public static void UpdateItems(this ExchangeInstance exchange, Packet packet, uint playerUniqueId)
    {
        var ownerUniqueId = packet.ReadUInt();
        var playerIsSender = ownerUniqueId == playerUniqueId;
        var items = new List<ExchangeItem>(12);

        var itemCount = packet.ReadByte();
        for (var i = 0; i < itemCount; i++)
        {
            var item = packet.ReadExchangeItem(playerIsSender);

            if (item.Item == null)
            {
                Log.Debug($"Could not detect item at exchange slot #{item.ExchangeSlot}");
                continue;
            }

            items.Add(item);
        }

        exchange.SetItems(playerIsSender, items);
    }

    private static ExchangeItem ReadExchangeItem(this Packet packet, bool hasSource = false)
    {
        return new ExchangeItem
        {
            SourceSlot = hasSource ? packet.ReadByte() : (byte)0,
            ExchangeSlot = packet.ReadByte(),
            Item = packet.ReadInventoryItem(0xFF),
        };
    }
}
