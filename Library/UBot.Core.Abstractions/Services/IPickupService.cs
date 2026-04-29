using System.Collections.Generic;

namespace UBot.Core.Abstractions.Services;

public interface IPickupService
{
    bool RunningPlayerPickup { get; }
    bool RunningAbilityPetPickup { get; }
    List<(string CodeName, bool PickOnlyChar)> PickupFilter { get; }

    void RunPlayer(object playerPosition, object centerPosition, int radius = 50);
    void RunAbilityPet(object centerPosition, int radius = 50);
    void AddFilter(string codeName, bool pickOnlyChar = false);
    void RemoveFilter(string codeName);
    void LoadFilter();
    void SaveFilter();
    void Stop();
}
