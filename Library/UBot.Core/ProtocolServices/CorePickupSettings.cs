using UBot.Core.Abstractions.Services;

namespace UBot.Core.ProtocolServices;

internal sealed class CorePickupSettings : IPickupSettings
{
    public bool PickupGold => UBot.Core.RuntimeAccess.Player.Get("UBot.Items.Pickup.Gold", true);

    public bool PickupRareItems => UBot.Core.RuntimeAccess.Player.Get("UBot.Items.Pickup.Rare", true);

    public bool PickupBlueItems => UBot.Core.RuntimeAccess.Player.Get("UBot.Items.Pickup.Blue", true);

    public bool PickupQuestItems => UBot.Core.RuntimeAccess.Player.Get("UBot.Items.Pickup.Quest", true);

    public bool PickupAnyEquips => UBot.Core.RuntimeAccess.Player.Get("UBot.Items.Pickup.AnyEquips", true);

    public bool PickupEverything => UBot.Core.RuntimeAccess.Player.Get("UBot.Items.Pickup.Everything", true);

    public bool UseAbilityPet => UBot.Core.RuntimeAccess.Player.Get("UBot.Items.Pickup.EnableAbilityPet", true);

    public bool JustPickMyItems => UBot.Core.RuntimeAccess.Player.Get("UBot.Items.Pickup.JustPickMyItems", false);

    public string[] LoadPickupFilter() => UBot.Core.RuntimeAccess.Player.GetArray<string>("UBot.Shopping.Pickup");

    public void SavePickupFilter(string[] values) => UBot.Core.RuntimeAccess.Player.SetArray("UBot.Shopping.Pickup", values);
}
