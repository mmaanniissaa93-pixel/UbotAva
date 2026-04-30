using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Objects;
using UBot.Core.Objects.Spawn;

namespace UBot.Training.Bundle.Loot;

internal class LootBundle : IBundle
{
    /// <summary>
    ///     Gets the configuration.
    /// </summary>
    /// <value>
    ///     The configuration.
    /// </value>
    public LootConfig Config { get; private set; }

    /// <summary>
    ///     Invokes this instance.
    /// </summary>
    public void Invoke()
    {
        if (Bundles.Loot.Config.DontPickupWhileBotting)
            return;

        //If we use the ability pet, we can attack during the work of the Pickup manager
        if (Config.UseAbilityPet && UBot.Core.RuntimeAccess.Session.Player.HasActiveAbilityPet && !PickupManager.RunningAbilityPetPickup)
        {
            PickupManager.RunAbilityPet(Container.Bot.Area.Position, Container.Bot.Area.Radius);
            return;
        }

        if ((Bundles.Loot.Config.DontPickupInBerzerk && UBot.Core.RuntimeAccess.Session.Player.Berzerking) || ScriptManager.Running)
            return;

        //Don't pickup if a mob is selected
        if (UBot.Core.RuntimeAccess.Session.SelectedEntity is SpawnedMonster monster && monster.State.LifeState == LifeState.Alive)
            return;

        PickupManager.RunPlayer(UBot.Core.RuntimeAccess.Session.Player.Position, Container.Bot.Area.Position, Container.Bot.Area.Radius);
    }

    /// <summary>
    ///     Refreshes this instance.
    /// </summary>
    public void Refresh()
    {
        Config = new LootConfig
        {
            UseAbilityPet = UBot.Core.RuntimeAccess.Player.Get("UBot.Items.Pickup.EnableAbilityPet", true),
            DontPickupWhileBotting = UBot.Core.RuntimeAccess.Player.Get("UBot.Items.Pickup.DontPickupWhileBotting", false),
            DontPickupInBerzerk = UBot.Core.RuntimeAccess.Player.Get("UBot.Items.Pickup.DontPickupInBerzerk", true),
        };
    }

    public void Stop()
    {
        PickupManager.Stop();
    }
}
