using System.Collections.Generic;
using UBot.Core.Abstractions;
using UBot.Core.Components;
using UBot.Core.Network;
using UBot.Core.Objects.Item;

namespace UBot.Core.Objects;

public static class InventoryItemPacketExtensions
{
    public static InventoryItem ReadInventoryItem(this Packet packet, byte destinationSlot = 0xFE)
    {
        var clientType = Game.ClientType;
        var item = new InventoryItem
        {
            MagicOptions = new List<MagicOptionInfo>(),
            BindingOptions = new List<BindingOption>(),
            Amount = 1,
            Slot = destinationSlot,
        };

        if (destinationSlot == 0xFE)
            item.Slot = packet.ReadByte();

        if (clientType > GameClientType.Thailand)
            item.Rental = packet.ReadRentInfo(clientType);

        item.ItemId = packet.ReadUInt();

        var record = item.Record;
        if (record == null)
        {
            Log.Notify("No item found for " + item.ItemId);
            return null;
        }

        if (record.IsEquip || record.IsFellowEquip || record.IsJobEquip)
        {
            item.OptLevel = packet.ReadByte();
            item.Attributes = new ItemAttributesInfo(packet.ReadULong());
            item.Durability = packet.ReadUInt();

            var magicOptionsAmount = packet.ReadByte();
            for (var iMagicOption = 0; iMagicOption < magicOptionsAmount; iMagicOption++)
                item.MagicOptions.Add(packet.ReadMagicOptionInfo());

            if (clientType > GameClientType.Thailand)
            {
                var bindingCount = GetBindingCount(clientType);
                for (var bindingIndex = 0; bindingIndex < bindingCount; bindingIndex++)
                {
                    var bindingType = (BindingOptionType)packet.ReadByte();
                    var bindingAmount = packet.ReadByte();
                    for (var iSocketAmount = 0; iSocketAmount < bindingAmount; iSocketAmount++)
                        item.BindingOptions.Add(packet.ReadBindingOption(bindingType));
                }
            }
        }
        else if (record.IsPet)
        {
            item.State = (InventoryItemState)packet.ReadByte();
            item.Amount = 1;

            if (item.State != InventoryItemState.Inactive)
            {
                item.Cos.Id = packet.ReadUInt();
                item.Cos.Name = packet.ReadString();

                if (record.TypeID4 == 2)
                    item.Cos.Rental = packet.ReadRentInfo(clientType);
                else if (clientType >= GameClientType.Chinese_Old)
                    item.Cos.Level = packet.ReadByte();

                var buffCount = packet.ReadByte();
                for (var i = 0; i < buffCount; i++)
                    SkipPetBuff(packet);
            }
        }
        else if (record.IsTransmonster)
        {
            item.Cos.Id = packet.ReadUInt();
        }
        else if (record.IsMagicCube)
        {
            item.Amount = (ushort)packet.ReadUInt();
        }
        else if (record.IsSpecialtyGoodBox)
        {
            item.Amount = (ushort)packet.ReadUInt();
        }
        else if (record.IsStackable)
        {
            item.Amount = packet.ReadUShort();

            if (record.TypeID3 == 11)
            {
                if (record.TypeID4 == 1 || record.TypeID4 == 2)
                    packet.ReadByte();
            }
            else if (record.TypeID3 == 14 && record.TypeID4 == 2)
            {
                var magParamCount = packet.ReadByte();
                for (var i = 0; i < magParamCount; i++)
                {
                    packet.ReadUInt();
                    packet.ReadUInt();
                }
            }

            if (record.IsTrading)
                packet.ReadString();
        }

        return item;
    }

    private static int GetBindingCount(GameClientType clientType)
    {
        return clientType switch
        {
            GameClientType.Chinese_Old
            or GameClientType.Chinese
            or GameClientType.Global
            or GameClientType.Turkey
            or GameClientType.Rigid
            or GameClientType.RuSro
            or GameClientType.VTC_Game
            or GameClientType.Japanese
            or GameClientType.Taiwan => 4,
            GameClientType.Korean => 3,
            _ => 2,
        };
    }

    private static void SkipPetBuff(Packet packet)
    {
        var buffType = packet.ReadByte();
        if (buffType == 0 || buffType == 20 || buffType == 6)
        {
            packet.ReadUInt();
            packet.ReadUInt();
        }

        if (buffType == 5)
        {
            packet.ReadUInt();
            packet.ReadUInt();
            packet.ReadUInt();
            packet.ReadByte();
        }
    }
}
