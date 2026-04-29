using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace UBot.Core.Abstractions.Services;

public interface IPickupRuntime
{
    bool PlayerHasActiveAbilityPet { get; }
    bool PlayerInAction { get; }
    bool PlayerSpecialtyBagFull { get; }
    uint PlayerJid { get; }
    bool IsItemAutoShareParty { get; }
    object AbilityPetPosition { get; }

    IReadOnlyList<IPickupItem> GetItems(Func<IPickupItem, bool> predicate);
    bool IsPartyMember(uint memberId);
    double Distance(object source, object destination);
    bool Pickup(IPickupItem item);
    Task<bool> PickupWithAbilityPetAsync(IPickupItem item);
}
