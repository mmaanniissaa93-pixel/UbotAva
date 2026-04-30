using System;
using System.Collections.Generic;
using System.Linq;
using UBot.Core.Abstractions.Services;
using UBot.Core.Objects;
using UBot.Core.Objects.Cos;
using UBot.Core.Objects.Spawn;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreInventoryRuntime : IInventoryRuntime
{
    public bool PlayerInventoryFull => UBot.Core.RuntimeAccess.Session.Player?.Inventory?.Full == true;

    public bool PlayerHasActiveAbilityPet => UBot.Core.RuntimeAccess.Session.Player?.HasActiveAbilityPet == true;

    public int PlayerLevel => UBot.Core.RuntimeAccess.Session.Player?.Level ?? 0;

    public object CurrentWeapon => UBot.Core.RuntimeAccess.Session.Player?.Weapon;

    public IList<object> GetPlayerNormalPartItems(Func<object, bool> predicate)
    {
        return UBot.Core.RuntimeAccess.Session.Player?.Inventory?.GetNormalPartItems(item => predicate(item)).Cast<object>().ToList() ?? new List<object>();
    }

    public IList<object> GetPlayerInventoryItems(Func<object, bool> predicate)
    {
        return UBot.Core.RuntimeAccess.Session.Player?.Inventory?.GetItems(item => predicate(item)).Cast<object>().ToList() ?? new List<object>();
    }

    public object GetPlayerInventoryItemAt(byte slot)
    {
        return UBot.Core.RuntimeAccess.Session.Player?.Inventory?.GetItemAt(slot);
    }

    public int GetPlayerInventorySumAmount(string recordCodeName)
    {
        return UBot.Core.RuntimeAccess.Session.Player?.Inventory?.GetSumAmount(recordCodeName) ?? 0;
    }

    public void MovePlayerInventoryItem(byte sourceSlot, byte destinationSlot, ushort amount)
    {
        UBot.Core.RuntimeAccess.Session.Player?.Inventory?.MoveItem(sourceSlot, destinationSlot, amount);
    }

    public IList<object> GetAbilityPetItems(Func<object, bool> predicate)
    {
        return UBot.Core.RuntimeAccess.Session.Player?.AbilityPet is Ability ability
            ? ability.Inventory.GetItems(item => predicate(item)).Cast<object>().ToList()
            : new List<object>();
    }

    public byte MoveAbilityPetItemToPlayer(byte slot)
    {
        return UBot.Core.RuntimeAccess.Session.Player?.AbilityPet is Ability ability ? ability.MoveItemToPlayer(slot) : byte.MaxValue;
    }

    public IList<object> GetStorageItems(bool guildStorage, Func<object, bool> predicate)
    {
        var storage = guildStorage ? UBot.Core.RuntimeAccess.Session.Player?.GuildStorage : UBot.Core.RuntimeAccess.Session.Player?.Storage;
        return storage?.GetItems(item => predicate(item)).Cast<object>().ToList() ?? new List<object>();
    }

    public object GetStorageItemAt(bool guildStorage, byte slot)
    {
        var storage = guildStorage ? UBot.Core.RuntimeAccess.Session.Player?.GuildStorage : UBot.Core.RuntimeAccess.Session.Player?.Storage;
        return storage?.GetItemAt(slot);
    }

    public void MoveStorageItem(bool guildStorage, byte sourceSlot, byte destinationSlot, ushort amount, object npc)
    {
        if (npc is not SpawnedBionic bionic)
            return;

        if (guildStorage)
            UBot.Core.RuntimeAccess.Session.Player?.GuildStorage?.MoveItem(sourceSlot, destinationSlot, amount, bionic);
        else
            UBot.Core.RuntimeAccess.Session.Player?.Storage?.MoveItem(sourceSlot, destinationSlot, amount, bionic);
    }

    public byte GetStorageFreeSlot(bool guildStorage)
    {
        var storage = guildStorage ? UBot.Core.RuntimeAccess.Session.Player?.GuildStorage : UBot.Core.RuntimeAccess.Session.Player?.Storage;
        return storage?.GetFreeSlot() ?? byte.MaxValue;
    }
}
