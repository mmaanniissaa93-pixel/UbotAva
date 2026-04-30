using UBot.Core;
using UBot.Core.Extensions;
using UBot.Core.Network;
using UBot.Core.Objects;
using System.Collections.Generic;

namespace UBot.Chat.Bundle;

internal class Chat
{
    internal static bool IgnoreChatResponsePacket;

    internal static Dictionary<ulong, InventoryItem> LinkedItems = [];

    /// <summary>
    ///     Sends the chat packet.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <param name="message">The message.</param>
    /// <param name="receiver">The receiver.</param>
    internal static void SendChatPacket(ChatType type, string message, string receiver = null)
    {
        var chatPacket = new Packet(0x7025);

        chatPacket.WriteByte(type);
        chatPacket.WriteByte(1); //chatIndex

        if (UBot.Core.RuntimeAccess.Session.ClientType > GameClientType.Vietnam)
            chatPacket.WriteByte(0); // has linking

        if (UBot.Core.RuntimeAccess.Session.ClientType >= GameClientType.Chinese_Old)
            chatPacket.WriteByte(0);

        if (type == ChatType.Private)
            chatPacket.WriteString(receiver);

        chatPacket.WriteConditonalString(message);

        UBot.Core.RuntimeAccess.Packets.SendPacket(chatPacket, PacketDestination.Server);
        IgnoreChatResponsePacket = true;
    }

    internal static void SendGlobalChatPacket(string message)
    {
        var inventoryItem = UBot.Core.RuntimeAccess.Session.Player.Inventory.GetItem(new TypeIdFilter(3, 3, 3, 5)); //3, 3, 3, 22 for VIP global

        if (inventoryItem != null)
        {
            var globalChatPacket = new Packet(0x704C);

            globalChatPacket.WriteByte(inventoryItem.Slot);

            if (UBot.Core.RuntimeAccess.Session.ClientType > GameClientType.Vietnam)
            {
                globalChatPacket.WriteInt(inventoryItem.Record.Tid);
                globalChatPacket.WriteByte(0); //0-3 linked items. max 500 chars when 1-3
            }
            else
            {
                globalChatPacket.WriteUShort(inventoryItem.Record.Tid);
            }

            globalChatPacket.WriteConditonalString(message);

            UBot.Core.RuntimeAccess.Packets.SendPacket(globalChatPacket, PacketDestination.Server);
        }
    }
}
