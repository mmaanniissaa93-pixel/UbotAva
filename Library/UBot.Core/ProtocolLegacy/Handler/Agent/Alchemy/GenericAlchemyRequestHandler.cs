using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using System.Collections.Generic;
using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Alchemy;

internal static class GenericAlchemyRequestHandler
{
    public static void Invoke(Packet packet)
    {
        var action = (AlchemyAction)packet.ReadByte();

        if (action != AlchemyAction.Fuse)
            return;

        var type = (AlchemyType)packet.ReadByte();
        if (type == AlchemyType.SocketInsert)
        {
            var item = CoreGame.Player.Inventory.GetItemAt(packet.ReadByte()); //Target item
            var socketItem = CoreGame.Player.Inventory.GetItemAt(packet.ReadByte()); //Target item

            if (item != null && socketItem != null)
                AlchemyManager.ActiveAlchemyItems = new List<InventoryItem> { item, socketItem };

            return;
        }

        var slots = packet.ReadBytes(packet.ReadByte());

        AlchemyManager.ActiveAlchemyItems = new List<InventoryItem>(slots.Length);

        foreach (var slot in slots)
        {
            var item = CoreGame.Player.Inventory.GetItemAt(slot);

            if (item != null)
                AlchemyManager.ActiveAlchemyItems.Add(item);
        }

        EventManager.FireEvent("OnFuseRequest", action, type);

        AlchemyManager.IsFusing = true;
        AlchemyManager.StartTimer();
    }
}





