using CoreGame = global::UBot.Core.Game;
using UBot.Core.Abstractions;
using UBot.Core.Network;
using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Alchemy;

internal static class GenericAlchemyAckResponse
{
    public static void Invoke(Packet packet, AlchemyType type)
    {
        AlchemyManager.IsFusing = false;

        EventManager.FireEvent("OnAlchemy", type);

        var result = packet.ReadByte();

        //Error
        if (result == 2)
        {
            EventManager.FireEvent("OnAlchemyError", packet.ReadUShort(), type);
            AlchemyManager.ActiveAlchemyItems = null;

            return;
        }

        var action = (AlchemyAction)packet.ReadByte();
        if (action == AlchemyAction.Cancel)
        {
            EventManager.FireEvent("OnAlchemyCanceled", type);
            AlchemyManager.ActiveAlchemyItems = null;

            return;
        }

        var isSuccess = packet.ReadBool();

        if (CoreGame.ClientType >= GameClientType.Chinese)
            packet.ReadByte(); //???

        var slot = packet.ReadByte();

        var oldItem = CoreGame.Player.Inventory.GetItemAt(slot);

        if (!isSuccess)
        {
            var isDestroyed = packet.ReadBool();

            if (isDestroyed)
            {
                EventManager.FireEvent("OnAlchemyDestroyed", oldItem, type);
                CoreGame.Player.Inventory.RemoveAt(slot);
                AlchemyManager.ActiveAlchemyItems = null;

                return;
            }
        }

        var newItem = packet.ReadInventoryItem(slot);

        CoreGame.Player.Inventory.RemoveAt(slot);
        CoreGame.Player.Inventory.Add(newItem);

        EventManager.FireEvent(isSuccess ? "OnAlchemySuccess" : "OnAlchemyFailed", oldItem, newItem, type);
        EventManager.FireEvent("OnInventoryUpdate");
        AlchemyManager.ActiveAlchemyItems = null;
    }
}





