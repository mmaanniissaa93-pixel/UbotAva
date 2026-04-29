using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UBot.Core.Abstractions.Services;
using UBot.Core.Components;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Cos;
using UBot.Core.Objects.Inventory;
using UBot.Core.Objects.Spawn;
using UBot.GameData.ReferenceObjects;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreShoppingRuntime : IShoppingRuntime
{
    public bool Clientless => Game.Clientless;

    public GameClientType ClientType => Game.ClientType;

    public object SelectedEntity => Game.SelectedEntity;

    public string[] LoadSellFilter() => PlayerConfig.GetArray<string>("UBot.Shopping.Sell");

    public string[] LoadStoreFilter() => PlayerConfig.GetArray<string>("UBot.Shopping.Store");

    public void SaveSellFilter(IEnumerable<string> values) => PlayerConfig.SetArray("UBot.Shopping.Sell", values);

    public void SaveStoreFilter(IEnumerable<string> values) => PlayerConfig.SetArray("UBot.Shopping.Store", values);

    public object GetShopGroup(string npcCodeName) => Game.ReferenceManager.GetRefShopGroup(npcCodeName);

    public IEnumerable<object> GetShopGoods(object shopGroup)
    {
        return shopGroup is RefShopGroup group
            ? Game.ReferenceManager.GetRefShopGoods(group).Cast<object>()
            : [];
    }

    public byte GetShopGoodTabIndex(string npcCodeName, object shopGood)
    {
        return shopGood is RefShopGood typedShopGood
            ? Game.ReferenceManager.GetRefShopGoodTabIndex(npcCodeName, typedShopGood)
            : byte.MaxValue;
    }

    public object GetPackageItem(string packageItemCodeName)
    {
        return Game.ReferenceManager.GetRefPackageItem(packageItemCodeName);
    }

    public object GetRefItem(string itemCodeName) => Game.ReferenceManager.GetRefItem(itemCodeName);

    public IEnumerable<object> GetEventRewardItems(uint questId)
    {
        return Game.ReferenceManager.GetEventRewardItems(questId).Cast<object>();
    }

    public void SelectNPC(string npcCodeName)
    {
        if (Game.SelectedEntity != null && Game.SelectedEntity.Record.CodeName == npcCodeName)
            return;

        if (!SpawnManager.TryGetEntity<SpawnedNpcNpc>(p => p.Record.CodeName == npcCodeName, out var entity))
        {
            Log.Warn("Cannot access the NPC [" + npcCodeName + "] because it does not exist nearby.");
            return;
        }

        entity.TrySelect();
    }

    public void SellItem(object item, object cos = null)
    {
        if (Game.SelectedEntity == null || item is not InventoryItem inventoryItem)
            return;

        var bionic = cos as SpawnedBionic;
        var packet = new Packet(0x7034);
        packet.WriteByte(bionic == null ? InventoryOperation.SP_SELL_ITEM : InventoryOperation.SP_SELL_ITEM_COS);

        if (bionic != null)
            packet.WriteUInt(bionic.UniqueId);

        packet.WriteByte(inventoryItem.Slot);
        packet.WriteUShort(inventoryItem.Amount);
        packet.WriteUInt(Game.SelectedEntity.UniqueId);

        var awaitResult = new AwaitCallback(null, 0xB034);
        PacketManager.SendPacket(packet, PacketDestination.Server, awaitResult);
        awaitResult.AwaitResponse();

        Log.Debug("[Shopping manager] - Sold item: " + inventoryItem.Record.GetRealName());
    }

    public void PurchaseItem(int tab, int slot, ushort amount)
    {
        if (Game.SelectedEntity == null)
        {
            Log.Debug("Cannot buy items, because no shop is selected!");
            return;
        }

        var packet = new Packet(0x7034);
        packet.WriteByte(InventoryOperation.SP_BUY_ITEM);
        packet.WriteByte(tab);
        packet.WriteByte(slot);
        packet.WriteUShort(amount);
        packet.WriteUInt(Game.SelectedEntity.UniqueId);

        var awaitResult = new AwaitCallback(null, 0xB034);
        PacketManager.SendPacket(packet, PacketDestination.Server, awaitResult);
        awaitResult.AwaitResponse();
    }

    public void PurchaseItem(object transport, int tab, int slot, ushort amount)
    {
        if (Game.SelectedEntity == null)
        {
            Log.Debug("Cannot buy items, because no shop is selected!");
            return;
        }

        var packet = new Packet(0x7034);
        packet.WriteByte(InventoryOperation.SP_BUY_ITEM_COS);
        packet.WriteUInt(0);
        packet.WriteByte(tab);
        packet.WriteByte(slot);
        packet.WriteUShort(amount);
        packet.WriteUInt(Game.SelectedEntity.UniqueId);

        var awaitResult = new AwaitCallback(null, 0xB034);
        PacketManager.SendPacket(packet, PacketDestination.Server, awaitResult);
        awaitResult.AwaitResponse();
    }

    public uint GetQuestId(string npcCodeName)
    {
        ChooseTalkOption(npcCodeName, TalkOption.Quest);

        var packet = new Packet(0x30D4);
        packet.WriteByte(5);

        uint questId = 0;
        var awaitCallback = new AwaitCallback(
            response =>
            {
                questId = response.ReadUInt();
                return AwaitCallbackResult.Success;
            },
            0x3514
        );

        PacketManager.SendPacket(packet, PacketDestination.Server, awaitCallback);
        awaitCallback.AwaitResponse();

        return questId;
    }

    public void ReceiveQuestReward(string npcCodeName, uint questId, uint rewardId)
    {
        GetQuestId(npcCodeName);

        var packet = new Packet(0x7515);
        packet.WriteUInt(questId);
        packet.WriteByte(1);
        packet.WriteUInt(rewardId);
        PacketManager.SendPacket(packet, PacketDestination.Server);

        CloseShop();
    }

    public void RepairItems(string npcCodeName, bool repairGear)
    {
        if (!repairGear)
            return;

        SelectNPC(npcCodeName);

        if (Game.SelectedEntity == null)
        {
            Log.Debug("Cannot repair items because there is no smith selected!");
            return;
        }

        var packet = new Packet(0x703E);
        packet.WriteUInt(Game.SelectedEntity.UniqueId);
        packet.WriteByte(2);

        var awaitCallback = new AwaitCallback(
            response =>
            {
                var result = response.ReadByte();

                if (result == 2)
                {
                    var errorCode = response.ReadUShort();
                    Log.Debug($"Repair of items at NPC {npcCodeName} failed [code={errorCode}]");
                    return AwaitCallbackResult.Fail;
                }

                return AwaitCallbackResult.Success;
            },
            0xB03E
        );

        PacketManager.SendPacket(packet, PacketDestination.Server, awaitCallback);
        awaitCallback.AwaitResponse();

        CloseShop();
    }

    public void StoreItem(object item, object npc)
    {
        if (item is not InventoryItem inventoryItem || npc is not SpawnedBionic bionic)
            return;

        var isWarehouse = IsWarehouseNpc(bionic);
        var destinationSlot = isWarehouse
            ? Game.Player.Storage.GetFreeSlot()
            : Game.Player.GuildStorage.GetFreeSlot();

        var packet = new Packet(0x7034);
        packet.WriteByte(isWarehouse ? 0x02 : 0x1E);
        packet.WriteByte(inventoryItem.Slot);
        packet.WriteByte(destinationSlot);
        packet.WriteUInt(bionic.UniqueId);

        var awaitResult = new AwaitCallback(null, 0xB034);
        PacketManager.SendPacket(packet, PacketDestination.Server, awaitResult);
        awaitResult.AwaitResponse();
    }

    public void OpenStorage(uint uniqueId)
    {
        if (Game.Player.Storage != null)
            return;

        var packet = new Packet(0x703C);
        packet.WriteInt(uniqueId);
        packet.WriteByte(0);

        var awaitResult = new AwaitCallback(null, 0x3049);
        PacketManager.SendPacket(packet, PacketDestination.Server, awaitResult);
        awaitResult.AwaitResponse();

        packet = new Packet(0x7046);
        packet.WriteUInt(uniqueId);
        packet.WriteUInt(0x04);

        awaitResult = new AwaitCallback(null, 0xB046);

        PacketManager.SendPacket(packet, PacketDestination.Server, awaitResult);
        awaitResult.AwaitResponse();
    }

    public void OpenGuildStorage(uint uniqueId)
    {
        var packet = new Packet(0x7046);
        packet.WriteUInt(uniqueId);
        packet.WriteByte(0x0D);
        var awaitResult = new AwaitCallback(null, 0xB046);
        PacketManager.SendPacket(packet, PacketDestination.Server, awaitResult);
        awaitResult.AwaitResponse();

        Thread.Sleep(2000);

        if (!Game.Clientless)
            return;

        packet = new Packet(0x7250);
        packet.WriteInt(uniqueId);
        awaitResult = new AwaitCallback(null, 0xB250);
        PacketManager.SendPacket(packet, PacketDestination.Server, awaitResult);
        awaitResult.AwaitResponse();

        packet = new Packet(0x7252);
        packet.WriteInt(uniqueId);
        PacketManager.SendPacket(packet, PacketDestination.Server);
    }

    public void CloseShop()
    {
        if (Game.SelectedEntity != null && Game.SelectedEntity.TryDeselect())
            Game.SelectedEntity = null;
    }

    public void CloseGuildShop()
    {
        if (Game.SelectedEntity != null)
            Game.SelectedEntity = null;
    }

    public void CloseGuildStorage(uint uniqueId)
    {
        var packet = new Packet(0x7251);
        packet.WriteUInt(uniqueId);
        var awaitResult = new AwaitCallback(null, 0xB251);
        PacketManager.SendPacket(packet, PacketDestination.Server, awaitResult);
        awaitResult.AwaitResponse();

        Thread.Sleep(2000);
    }

    public void ChooseTalkOption(string npcCodeName, object option)
    {
        var talkOption = option is TalkOption typedOption ? typedOption : (TalkOption)option;
        if (!SpawnManager.TryGetEntity<SpawnedNpcNpc>(p => p.Record.CodeName == npcCodeName, out var entity))
        {
            Log.Debug("Cannot access the NPC [" + npcCodeName + "] because it does not exist nearby.");
            return;
        }

        SelectNPC(npcCodeName);

        var packet = new Packet(0x7046);
        packet.WriteUInt(entity.UniqueId);
        packet.WriteByte(talkOption);

        var awaitResult = new AwaitCallback(
            response =>
            {
                return response.ReadByte() == 0x01 && response.ReadByte() == (byte)talkOption
                    ? AwaitCallbackResult.Success
                    : AwaitCallbackResult.ConditionFailed;
            },
            0xB046
        );
        PacketManager.SendPacket(packet, PacketDestination.Server, awaitResult);
        awaitResult.AwaitResponse(1000);
    }

    public object GetSelectedNpc() => Game.SelectedEntity;

    public uint GetNpcUniqueId(object npc) => npc is SpawnedBionic bionic ? bionic.UniqueId : 0;

    public string GetNpcCodeName(object npc) => npc is SpawnedBionic bionic ? bionic.Record.CodeName : string.Empty;

    public bool IsWarehouseNpc(object npc)
    {
        return GetNpcCodeName(npc).Contains("WAREHOUSE");
    }

    public bool HasPlayerStorage(bool guildStorage)
    {
        return guildStorage ? Game.Player.GuildStorage != null : Game.Player.Storage != null;
    }
}
