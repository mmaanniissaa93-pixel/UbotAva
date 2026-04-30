using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Abstractions;
using UBot.Core.Network;
using System.Collections.Generic;
using UBot.Core.Objects;
using UBot.Core.Objects.Cos;
using UBot.Core.Objects.Inventory;
using UBot.Core.Objects.Item;
using UBot.Protocol.Legacy;
using UBot.Core;
using CosEntity = UBot.Core.Objects.Cos.Cos;

namespace UBot.Protocol.Handlers.Agent.Inventory;

public class InventoryOperationResponse : IPacketHandler 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0xB034;

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
        if (CoreGame.Player == null)
            return;

        var result = packet.ReadByte();
        if (result != 0x01)
        {
            var code = packet.ReadByte();

            Log.Debug($"ItemOperation error received:  [{result:X}] ({code:X})");
            return;
        }

        var type = (InventoryOperation)packet.ReadByte();
        switch (type)
        {
            case InventoryOperation.SP_UPDATE_SLOTS_INV:

                CoreGame.Player.Inventory.Move(packet);

                //e.g when equipping a Bow (see ammo)
                if (packet.ReadBool())
                    if (packet.ReadByte() == 0x00)
                        CoreGame.Player.Inventory.Move(packet);

                break;

            case InventoryOperation.SP_UPDATE_SLOTS_CHEST:
                CoreGame.Player.Storage.Move(packet);
                break;

            case InventoryOperation.SP_DEPOSIT_ITEM:
                CoreGame.Player.Inventory.MoveTo(CoreGame.Player.Storage, packet);
                break;

            case InventoryOperation.SP_WITHDRAW_ITEM:
                CoreGame.Player.Storage.MoveTo(CoreGame.Player.Inventory, packet);
                break;

            case InventoryOperation.SP_PICK_ITEM:
                ParseFloorToInventory(packet, CoreGame.Player.Inventory);
                break;

            case InventoryOperation.SP_DROP_ITEM:
                ParseDeleteItemByServer(packet);
                break;

            case InventoryOperation.SP_BUY_ITEM:
                ParseNpcToInventory(packet, CoreGame.Player.Inventory, CoreGame.Player.UniqueId);
                break;

            case InventoryOperation.SP_SELL_ITEM:
                ParseInventoryToNpc(packet, CoreGame.Player.Inventory);
                break;

            case InventoryOperation.SP_DROP_GOLD:
                ParseGoldToFloor(packet);
                break;

            case InventoryOperation.SP_DEPOSIT_GOLD:
                ParseGoldToStorage(packet, CoreGame.Player.Storage);
                break;

            case InventoryOperation.SP_WITHDRAW_GOLD:
                ParseStorageToGold(packet, CoreGame.Player.Storage);
                break;

            case InventoryOperation.SP_ADD_ITEM_BY_SERVER:
                ParseAddItemByServer(packet);
                break;

            case InventoryOperation.SP_DEL_ITEM_BY_SERVER:
                ParseDeleteItemByServer(packet);
                break;

            case InventoryOperation.SP_UPDATE_SLOTS_INV_COS:
                ParseCosInventoryMoving(packet);
                break;

            case InventoryOperation.SP_PICK_ITEM_COS:
                ParseFloorToCos(packet);
                break;

            case InventoryOperation.SP_DROP_ITEM_COS:
                ParseCosToFloor(packet);
                break;

            case InventoryOperation.SP_BUY_ITEM_COS:
                ParseNpcToCos(packet);
                break;

            case InventoryOperation.SP_SELL_ITEM_COS:
                ParseCosToNpc(packet);
                break;

            case InventoryOperation.SP_DEL_COSITEM_BY_SERVER:
                ParseDeleteCosItemByServer(packet);
                break;

            case InventoryOperation.SP_BUY_CASH_ITEM:
                ParseMallToPlayer(packet);
                break;

            case InventoryOperation.SP_MOVE_ITEM_PET_PC:
                ParseCosToInventory(packet);
                break;

            case InventoryOperation.SP_MOVE_ITEM_PC_PET:
                ParseInventoryToCos(packet);
                break;

            case InventoryOperation.SP_PICK_ITEM_BY_OTHER:
                ParseOtherToInventory(packet);
                break;

            case InventoryOperation.SP_GUILD_CHEST_UPDATE_SLOT:
                CoreGame.Player.GuildStorage.Move(packet);
                break;

            case InventoryOperation.SP_GUILD_CHEST_DEPOSIT_ITEM:
                CoreGame.Player.Inventory.MoveTo(CoreGame.Player.GuildStorage, packet);
                break;

            case InventoryOperation.SP_GUILD_CHEST_WITHDRAW_ITEM:
                CoreGame.Player.GuildStorage.MoveTo(CoreGame.Player.Inventory, packet);
                break;

            case InventoryOperation.SP_GUILD_CHEST_DEPOSIT_GOLD:
                ParseGoldToStorage(packet, CoreGame.Player.GuildStorage);
                break;

            case InventoryOperation.SP_GUILD_CHEST_WITHDRAW_GOLD:
                ParseStorageToGold(packet, CoreGame.Player.GuildStorage);
                break;

            case InventoryOperation.SP_RESTORE_SOLDITEM_INSHOP:
                ParseBuybackToInventory(packet);
                break;

            case InventoryOperation.SP_MOVE_ITEM_AVATAR_PC:

                CoreGame.Player.Avatars.MoveTo(CoreGame.Player.Inventory, packet);
                packet.ReadUShort(); // amount

                if (packet.ReadBool())
                    if (packet.ReadByte() == 0x23)
                        CoreGame.Player.Avatars.MoveTo(CoreGame.Player.Inventory, packet);

                break;

            case InventoryOperation.SP_MOVE_ITEM_PC_AVATAR:

                CoreGame.Player.Inventory.MoveTo(CoreGame.Player.Avatars, packet);
                packet.ReadUShort(); // amount

                if (packet.ReadBool())
                    if (packet.ReadByte() == 0x00)
                        CoreGame.Player.Inventory.MoveTo(CoreGame.Player.Avatars, packet);

                break;

            case InventoryOperation.SP_MOVE_ITEM_PC_JOB:

                CoreGame.Player.Inventory.MoveTo(CoreGame.Player.Job2, packet);
                packet.ReadUShort(); // amount

                break;

            case InventoryOperation.SP_MOVE_ITEM_JOB_PC:

                CoreGame.Player.Job2.MoveTo(CoreGame.Player.Inventory, packet);
                packet.ReadUShort(); // amount

                break;

            case InventoryOperation.SP_BUY_ITEM_WITH_TOKEN:
                ParseTokenNpcToInventory(packet);
                break;

            case InventoryOperation.SP_PICK_SPECIAL_GOODS:
                ParseFloorToSpecialtyBag(packet);
                break;

            case InventoryOperation.SP_MOVE_ITEM_BAG_TO_JOBTRANSPORT:

                var cosUniqueId = packet.ReadUInt();
                CosEntity cos = CoreGame.Player.JobTransport;
                if (cos == null || cosUniqueId != cos.UniqueId)
                    return;

                CoreGame.Player.Job2SpecialtyBag.MoveTo(cos.Inventory, packet);

                break;

            case InventoryOperation.SP_DELETE_BAG_ITEM_BY_SERVER:

                var sourceSlot = packet.ReadByte();
                CoreGame.Player.Job2SpecialtyBag.RemoveAt(sourceSlot);

                break;

            default:
                Log.Error(
                    $"If you see this message i, please open an issue by explaining your last inventory operation! InventoryOperationType: {type}"
                );
                break;
        }

        EventManager.FireEvent("OnInventoryUpdate");
    }

    /// <summary>
    ///     Parses the floor to inventory.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseFloorToSpecialtyBag(Packet packet)
    {
        var unknown1 = packet.ReadUInt();

        ParseFloorToInventory(packet, CoreGame.Player.Job2SpecialtyBag);
    }

    /// <summary>
    ///     Parses the floor to inventory.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseFloorToInventory(Packet packet, InventoryItemCollection inventory)
    {
        var destinationSlot = packet.ReadByte();

        if (destinationSlot == 0xFE) //gold
        {
            var goldAmount = packet.ReadUInt();
            CoreGame.Player.Gold += goldAmount;

            EventManager.FireEvent("OnPickupGold", goldAmount);

            Log.Notify($"Picked up [{goldAmount}] gold");
            return;
        }

        var item = packet.ReadInventoryItem(destinationSlot);
        var itemAtSlot = inventory.GetItemAt(item.Slot);

        if (itemAtSlot != null)
        {
            itemAtSlot.Amount = item.Amount;

            EventManager.FireEvent("OnUpdateInventoryItem", itemAtSlot.Slot);
            Log.Debug(
                $"[Floor->Inventory] Merge item {itemAtSlot.Record.GetRealName()} (slot={destinationSlot}, amount={item.Amount})"
            );
        }
        else
        {
            inventory.Add(item);

            Log.Debug(
                $"[Floor->Inventory] Add item {item.Record.GetRealName()} (slot={destinationSlot}, amount={item.Amount}"
            );
        }

        Log.Notify($"Picked up item [{item.Record.GetRealName(true)}]");

        EventManager.FireEvent("OnPickupItem", item);
    }

    /// <summary>
    ///     Parses the NPC to inventory.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseNpcToInventory(Packet packet, InventoryItemCollection inventory, uint entityUniqueId)
    {
        byte[] destinationSlots = null;
        ushort amount = 0;
        byte itemAmount = 0;

        var tabIndex = packet.ReadByte();
        var tabSlot = packet.ReadByte();

        if (CoreGame.ClientType >= GameClientType.Chinese && CoreGame.ClientType != GameClientType.Rigid)
        {
            amount = packet.ReadUShort();
            itemAmount = packet.ReadByte();
            destinationSlots = packet.ReadBytes(itemAmount);
        }
        else
        {
            itemAmount = packet.ReadByte();
            destinationSlots = packet.ReadBytes(itemAmount);
            amount = packet.ReadUShort();
        }

        var npc = CoreGame.SelectedEntity;
        if (npc == null)
        {
            Log.Debug("Could not determine which item was bought, since currently no entity is selected.");
            return;
        }

        var refShopGoodObj = CoreGame.ReferenceManager.GetRefPackageItem(npc.Record.CodeName, tabIndex, tabSlot);
        var item = CoreGame.ReferenceManager.GetRefItem(refShopGoodObj.RefItemCodeName);

        if (item.IsStackable && refShopGoodObj.Data > 0)
            amount = (ushort)refShopGoodObj.Data;

        Log.Notify($"Bought [{item.GetRealName(true)}] x {amount} from [{npc.Record.GetRealName()}]");

        //_ETC_
        if (itemAmount != destinationSlots.Length)
        {
            inventory.Add(
                new InventoryItem
                {
                    Slot = destinationSlots[0],
                    Amount = amount,
                    ItemId = item.ID,
                    Durability = (uint)refShopGoodObj.Data,
                    Attributes = new ItemAttributesInfo((ulong)refShopGoodObj.Variance),
                    OptLevel = refShopGoodObj.OptLevel,
                }
            );

            EventManager.FireEvent("OnBuyItem", destinationSlots[0], entityUniqueId);
        }
        else
        {
            foreach (var slot in destinationSlots)
            {
                inventory.Add(
                    new InventoryItem
                    {
                        Slot = slot,
                        Amount = amount,
                        ItemId = item.ID,
                        Durability = (uint)refShopGoodObj.Data,
                        Attributes = new ItemAttributesInfo((ulong)refShopGoodObj.Variance),
                        OptLevel = refShopGoodObj.OptLevel,
                    }
                );

                EventManager.FireEvent("OnBuyItem", slot, entityUniqueId);
            }
        }
    }

    /// <summary>
    ///     Parses the NPC to inventory.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseTokenNpcToInventory(Packet packet)
    {
        var inventory = CoreGame.Player.Inventory;
        ParseNpcToInventory(packet, inventory, CoreGame.Player.UniqueId);

        var unknown1 = packet.ReadByte();
        var tokenSlot = packet.ReadByte();
        var updatedTokenCount = packet.ReadUShort();
        var unknown2 = packet.ReadInt();

        inventory.UpdateItemAmount(tokenSlot, updatedTokenCount);
    }

    /// <summary>
    ///     Parses the inventory to NPC.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseInventoryToNpc(Packet packet, InventoryItemCollection inventory)
    {
        var sourceSlot = packet.ReadByte();
        var amount = packet.ReadUShort();
        var uniqueId = packet.ReadUInt();
        var buybackSlot = packet.ReadByte();

        var itemAtSlot = inventory.GetItemAt(sourceSlot);
        if (itemAtSlot == null)
            return;

        if (buybackSlot != byte.MaxValue)
        {
            buybackSlot -= 1;

            if (ShoppingManager.BuybackList.ContainsKey(buybackSlot))
                ShoppingManager.BuybackList[buybackSlot] = itemAtSlot;
            else
                ShoppingManager.BuybackList.Add(buybackSlot, itemAtSlot);
        }

        if (amount == itemAtSlot.Amount)
        {
            inventory.RemoveAt(sourceSlot);

            Log.Debug(
                $"[Inventory->NPC] Remove item {itemAtSlot.Record.GetRealName()} (slot={sourceSlot}, amount={amount})"
            );
        }
        else
        {
            inventory.UpdateItemAmount(sourceSlot, (ushort)(itemAtSlot.Amount - amount));

            Log.Debug(
                $"[Inventory->NPC] Update item {itemAtSlot.Record.GetRealName()} (slot={sourceSlot}, amount={amount})"
            );
        }

        Log.Notify($"Sold item [{itemAtSlot.Record.GetRealName()}] x {amount}");
    }

    /// <summary>
    ///     Parses the gold to floor.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseGoldToFloor(Packet packet)
    {
        CoreGame.Player.Gold -= packet.ReadULong();
    }

    /// <summary>
    ///     Parses the gold to storage.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseGoldToStorage(Packet packet, Storage storage)
    {
        var gold = packet.ReadULong();
        var userGold = CoreGame.Player.Gold - gold;
        CoreGame.Player.Gold = userGold;

        storage.Gold += userGold;

        EventManager.FireEvent("OnStorageGoldUpdated");
    }

    /// <summary>
    ///     Parses the storage to gold.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseStorageToGold(Packet packet, Storage storage)
    {
        var gold = packet.ReadULong();
        var userGold = CoreGame.Player.Gold + gold;

        CoreGame.Player.Gold = userGold;
        storage.Gold -= gold;

        EventManager.FireEvent("OnStorageGoldUpdated");
    }

    /// <summary>
    ///     Parses the add item by server.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseAddItemByServer(Packet packet)
    {
        var destinationSlot = packet.ReadByte();
        packet.ReadByte(); //Reason?

        CoreGame.Player.Inventory.Add(packet.ReadInventoryItem(destinationSlot));
    }

    /// <summary>
    ///     Parses the delete item by server.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseDeleteItemByServer(Packet packet)
    {
        var sourceSlot = packet.ReadByte();
        CoreGame.Player.Inventory.RemoveAt(sourceSlot);

        Log.Debug($"[Inventory->Delete] Remove item (slot={sourceSlot})");
    }

    /// <summary>
    ///     Parses the delete item by server.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseDeleteCosItemByServer(Packet packet)
    {
        var uniqueId = packet.ReadUInt();

        CosEntity cos = null;

        if (uniqueId == CoreGame.Player.AbilityPet?.UniqueId)
            cos = CoreGame.Player.AbilityPet;
        else if (uniqueId == CoreGame.Player.JobTransport?.UniqueId)
            cos = CoreGame.Player.JobTransport;

        if (cos == null)
            return;

        var sourceSlot = packet.ReadByte();
        var reason = packet.ReadByte();

        cos.Inventory.RemoveAt(sourceSlot);

        Log.Debug($"[Inventory->Delete] Remove cos item (slot={sourceSlot})");
    }

    /// <summary>
    ///     Parses the pet to pet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseCosInventoryMoving(Packet packet)
    {
        var uniqueId = packet.ReadUInt();

        CosEntity cos = null;

        if (uniqueId == CoreGame.Player.AbilityPet?.UniqueId)
            cos = CoreGame.Player.AbilityPet;
        else if (uniqueId == CoreGame.Player.JobTransport?.UniqueId)
            cos = CoreGame.Player.JobTransport;

        if (cos == null)
            return;

        cos.Inventory.Move(packet);
    }

    /// <summary>
    ///     Parses the floor to pet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseFloorToCos(Packet packet)
    {
        var uniqueId = packet.ReadUInt();

        CosEntity cos = null;

        if (uniqueId == CoreGame.Player.AbilityPet?.UniqueId)
            cos = CoreGame.Player.AbilityPet;
        else if (uniqueId == CoreGame.Player.JobTransport?.UniqueId)
            cos = CoreGame.Player.JobTransport;

        if (cos == null)
            return;

        ParseFloorToInventory(packet, cos.Inventory);
    }

    /// <summary>
    ///     Parses the pet to floor.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseCosToFloor(Packet packet)
    {
        var uniqueId = packet.ReadUInt();
        var sourceSlot = packet.ReadByte();

        CosEntity cos = null;

        if (uniqueId == CoreGame.Player.AbilityPet?.UniqueId)
            cos = CoreGame.Player.AbilityPet;
        else if (uniqueId == CoreGame.Player.JobTransport?.UniqueId)
            cos = CoreGame.Player.JobTransport;

        if (cos == null)
            return;

        cos.Inventory.RemoveAt(sourceSlot);
        Log.Debug($"[Pet->Floor] Remove item (slot={sourceSlot})");
    }

    /// <summary>
    ///     Parses the NPC to pet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseNpcToCos(Packet packet)
    {
        var uniqueId = packet.ReadUInt();

        CosEntity cos = null;

        if (uniqueId == CoreGame.Player.AbilityPet?.UniqueId)
            cos = CoreGame.Player.AbilityPet;
        else if (uniqueId == CoreGame.Player.JobTransport?.UniqueId)
            cos = CoreGame.Player.JobTransport;

        if (cos == null)
            return;

        ParseNpcToInventory(packet, cos.Inventory, cos.UniqueId);
    }

    /// <summary>
    ///     Parses the pet to NPC.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseCosToNpc(Packet packet)
    {
        var uniqueId = packet.ReadUInt();

        CosEntity cos = null;

        if (uniqueId == CoreGame.Player.AbilityPet?.UniqueId)
            cos = CoreGame.Player.AbilityPet;
        else if (uniqueId == CoreGame.Player.JobTransport?.UniqueId)
            cos = CoreGame.Player.JobTransport;

        if (cos == null)
            return;

        ParseInventoryToNpc(packet, cos.Inventory);
    }

    /// <summary>
    ///     Parses the mall to player.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseMallToPlayer(Packet packet)
    {
        var refShopGroupId = packet.ReadUShort();
        var groupIndex = packet.ReadByte();
        var tabIndex = packet.ReadByte();
        var slotIndex = packet.ReadByte();
        var itemCount = packet.ReadByte();

        var refShopGoodObj = CoreGame.ReferenceManager.GetRefPackageItemById(
            refShopGroupId,
            groupIndex,
            tabIndex,
            slotIndex
        );
        if (refShopGoodObj == null)
            return;

        var itemInfo = CoreGame.ReferenceManager.GetRefItem(refShopGoodObj.RefItemCodeName);

        if (itemInfo != null)
        {
            var itemSlots = packet.ReadBytes(itemCount);
            var quantity = packet.ReadUShort();

            var amount = refShopGoodObj.Data;

            if (itemCount != quantity)
                amount = quantity;

            if (amount == 0)
                amount = 1;

            foreach (var slot in itemSlots)
            {
                var item = new InventoryItem
                {
                    Slot = slot,
                    Amount = (ushort)amount,
                    ItemId = itemInfo.ID,
                    Durability = (uint)refShopGoodObj.Data,
                    Attributes = new ItemAttributesInfo((ulong)refShopGoodObj.Variance),
                    OptLevel = refShopGoodObj.OptLevel,
                };

                if (CoreGame.ClientType > GameClientType.Thailand)
                    item.Rental = packet.ReadRentInfo(CoreGame.ClientType);

                CoreGame.Player.Inventory.Add(item);
            }
        }
    }

    /// <summary>
    ///     Parses the pet to inventory.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseCosToInventory(Packet packet)
    {
        var petUniqueId = packet.ReadUInt();

        var cos = CoreGame.Player.AbilityPet;

        if (!CoreGame.Player.HasActiveAbilityPet || cos.UniqueId != petUniqueId)
            return;

        cos.Inventory.MoveTo(CoreGame.Player.Inventory, packet);
    }

    /// <summary>
    ///     Parses the inventory to pet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseInventoryToCos(Packet packet)
    {
        var petUniqueId = packet.ReadUInt();
        var cos = CoreGame.Player.AbilityPet;

        if (!CoreGame.Player.HasActiveAbilityPet || cos.UniqueId != petUniqueId)
            return;

        CoreGame.Player.Inventory.MoveTo((InventoryItemCollection)cos.Inventory, packet);
    }

    /// <summary>
    ///     Parses the other to inventory.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseOtherToInventory(Packet packet)
    {
        var uniqueId = packet.ReadUInt(); // picker uniqueId

        var destinationSlot = packet.ReadByte();
        if (destinationSlot == 0xFE)
        {
            CoreGame.Player.Gold += packet.ReadUInt();
        }
        else
        {
            var item = packet.ReadInventoryItem(destinationSlot);
            var itemAtSlot = CoreGame.Player.Inventory.GetItemAt(destinationSlot);

            if (itemAtSlot == null)
                CoreGame.Player.Inventory.Add(item);
            else
                itemAtSlot.Amount = item.Amount;

            EventManager.FireEvent("OnPartyPickItem", item);
        }
    }

    /// <summary>
    ///     Parses the buyback to inventory.
    /// </summary>
    /// <param name="packet">The packet.</param>
    private static void ParseBuybackToInventory(Packet packet)
    {
        var destinationSlot = packet.ReadByte();
        var sourceSlot = packet.ReadByte();
        var amount = packet.ReadUShort();

        var itemAtSource = ShoppingManager.BuybackList[sourceSlot];
        itemAtSource.Slot = destinationSlot;
        itemAtSource.Amount = amount;

        CoreGame.Player.Inventory.Add(itemAtSource);

        Log.Debug("Buyback: " + itemAtSource.Record.GetRealName());
        var newBuybackList = new Dictionary<byte, InventoryItem>();

        foreach (var item in ShoppingManager.BuybackList)
        {
            if (item.Key == sourceSlot)
                continue;

            newBuybackList.Add(item.Key > sourceSlot ? (byte)(item.Key - 1) : item.Key, item.Value);
        }

        ShoppingManager.BuybackList = newBuybackList;
    }
}





