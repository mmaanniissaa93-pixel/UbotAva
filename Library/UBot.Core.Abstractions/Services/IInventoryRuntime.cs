using System;
using System.Collections.Generic;

namespace UBot.Core.Abstractions.Services;

public interface IInventoryRuntime
{
    bool PlayerInventoryFull { get; }
    bool PlayerHasActiveAbilityPet { get; }
    int PlayerLevel { get; }

    object CurrentWeapon { get; }
    IList<object> GetPlayerNormalPartItems(Func<object, bool> predicate);
    IList<object> GetPlayerInventoryItems(Func<object, bool> predicate);
    object GetPlayerInventoryItemAt(byte slot);
    int GetPlayerInventorySumAmount(string recordCodeName);
    void MovePlayerInventoryItem(byte sourceSlot, byte destinationSlot, ushort amount);

    IList<object> GetAbilityPetItems(Func<object, bool> predicate);
    byte MoveAbilityPetItemToPlayer(byte slot);

    IList<object> GetStorageItems(bool guildStorage, Func<object, bool> predicate);
    object GetStorageItemAt(bool guildStorage, byte slot);
    void MoveStorageItem(bool guildStorage, byte sourceSlot, byte destinationSlot, ushort amount, object npc);
    byte GetStorageFreeSlot(bool guildStorage);
}
