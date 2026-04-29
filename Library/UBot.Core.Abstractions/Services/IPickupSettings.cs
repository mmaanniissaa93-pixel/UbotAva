namespace UBot.Core.Abstractions.Services;

public interface IPickupSettings
{
    bool PickupGold { get; }
    bool PickupRareItems { get; }
    bool PickupBlueItems { get; }
    bool PickupQuestItems { get; }
    bool PickupAnyEquips { get; }
    bool PickupEverything { get; }
    bool UseAbilityPet { get; }
    bool JustPickMyItems { get; }

    string[] LoadPickupFilter();
    void SavePickupFilter(string[] values);
}
