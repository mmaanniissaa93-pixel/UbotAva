using UBot.Core.Abstractions.Services;

namespace UBot.Core.ProtocolServices;

internal sealed class CorePickupSettings : IPickupSettings
{
    public bool PickupGold => PlayerConfig.Get("UBot.Items.Pickup.Gold", true);

    public bool PickupRareItems => PlayerConfig.Get("UBot.Items.Pickup.Rare", true);

    public bool PickupBlueItems => PlayerConfig.Get("UBot.Items.Pickup.Blue", true);

    public bool PickupQuestItems => PlayerConfig.Get("UBot.Items.Pickup.Quest", true);

    public bool PickupAnyEquips => PlayerConfig.Get("UBot.Items.Pickup.AnyEquips", true);

    public bool PickupEverything => PlayerConfig.Get("UBot.Items.Pickup.Everything", true);

    public bool UseAbilityPet => PlayerConfig.Get("UBot.Items.Pickup.EnableAbilityPet", true);

    public bool JustPickMyItems => PlayerConfig.Get("UBot.Items.Pickup.JustPickMyItems", false);

    public string[] LoadPickupFilter() => PlayerConfig.GetArray<string>("UBot.Shopping.Pickup");

    public void SavePickupFilter(string[] values) => PlayerConfig.SetArray("UBot.Shopping.Pickup", values);
}
