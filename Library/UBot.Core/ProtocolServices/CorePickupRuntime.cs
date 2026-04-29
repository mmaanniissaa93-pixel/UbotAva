using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UBot.Core.Abstractions.Services;
using UBot.Core.Components;
using UBot.Core.Objects;
using UBot.Core.Objects.Spawn;

namespace UBot.Core.ProtocolServices;

internal sealed class CorePickupRuntime : IPickupRuntime
{
    public bool PlayerHasActiveAbilityPet => Game.Player?.HasActiveAbilityPet == true;

    public bool PlayerInAction => Game.Player?.InAction == true;

    public bool PlayerSpecialtyBagFull => Game.Player?.Job2SpecialtyBag?.Full == true;

    public uint PlayerJid => Game.Player?.JID ?? 0;

    public bool IsItemAutoShareParty =>
        Game.Party?.IsInParty == true && Game.Party.Settings.GetPartyType() is 2 or 3 or 6 or 7;

    public object AbilityPetPosition => Game.Player?.AbilityPet?.Position;

    public IReadOnlyList<IPickupItem> GetItems(Func<IPickupItem, bool> predicate)
    {
        if (!SpawnManager.TryGetEntities<SpawnedItem>(out var spawnedItems))
            return Array.Empty<IPickupItem>();

        var result = new List<IPickupItem>();
        foreach (var spawnedItem in spawnedItems)
        {
            var item = new CorePickupItem(spawnedItem);
            if (predicate(item))
                result.Add(item);
        }

        return result;
    }

    public bool IsPartyMember(uint memberId) => Game.Party?.Members.Any(m => m.MemberId == memberId) == true;

    public double Distance(object source, object destination)
    {
        if (source is Position sourcePosition && destination is Position destinationPosition)
            return sourcePosition.DistanceTo(destinationPosition);

        return double.MaxValue;
    }

    public bool Pickup(IPickupItem item)
    {
        return item is CorePickupItem coreItem && coreItem.Item.Pickup();
    }

    public Task<bool> PickupWithAbilityPetAsync(IPickupItem item)
    {
        if (Game.Player?.AbilityPet == null)
            return Task.FromResult(false);

        return Game.Player.AbilityPet.PickupAsync(item.UniqueId);
    }
}

internal sealed class CorePickupItem : IPickupItem
{
    public CorePickupItem(SpawnedItem item)
    {
        Item = item;
    }

    public SpawnedItem Item { get; }

    public uint UniqueId => Item.UniqueId;

    public uint OwnerJid => Item.OwnerJID;

    public bool HasOwner => Item.HasOwner;

    public bool IsBehindObstacle => Item.IsBehindObstacle;

    public bool IsSpecialtyGoodBox => Item.Record.IsSpecialtyGoodBox;

    public bool IsGold => Item.Record.IsGold;

    public bool IsQuest => Item.Record.IsQuest;

    public bool IsEquip => Item.Record.IsEquip;

    public byte Rarity => (byte)Item.Rarity;

    public string CodeName => Item.Record.CodeName;

    public object SourcePosition => Item.Movement.Source;
}
