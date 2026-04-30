using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Abstractions;
using UBot.Core.Network;
using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;
using UBot.Core.Objects;
using UBot.Protocol.Legacy;
using UBot.Core;

namespace UBot.Protocol.Handlers.Agent.Alchemy;

internal static class GenericAlchemyAckResponse
{
    public static void Invoke(Packet packet, AlchemyType type)
    {
        EventManager.FireEvent("OnAlchemy", type);

        var result = packet.ReadByte();

        //Error
        if (result == 2)
        {
            var errorCode = packet.ReadUShort();
            EventManager.FireEvent("OnAlchemyError", errorCode, type);
            AlchemyManager.MarkError(errorCode, type);

            return;
        }

        var action = (AlchemyAction)packet.ReadByte();
        if (action == AlchemyAction.Cancel)
        {
            EventManager.FireEvent("OnAlchemyCanceled", type);
            AlchemyManager.MarkCanceled(type);

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
                AlchemyManager.MarkDestroyed(oldItem, type);

                return;
            }
        }

        var newItem = packet.ReadInventoryItem(slot);

        CoreGame.Player.Inventory.RemoveAt(slot);
        CoreGame.Player.Inventory.Add(newItem);

        EventManager.FireEvent(isSuccess ? "OnAlchemySuccess" : "OnAlchemyFailed", oldItem, newItem, type);
        EventManager.FireEvent("OnInventoryUpdate");
        AlchemyManager.MarkResult(isSuccess, oldItem, newItem, type);
    }
}





