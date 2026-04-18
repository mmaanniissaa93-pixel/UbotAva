using UBot.Core;
using UBot.Core.Event;

namespace UBot.Protection.Components.Pet;

public class CosHGPRecoveryHandler
{
    /// <summary>
    ///     Initiliazes this instance.
    /// </summary>
    public static void Initiliaze()
    {
        SubscribeEvents();
    }

    /// <summary>
    ///     Subscribes the events.
    /// </summary>
    private static void SubscribeEvents()
    {
        EventManager.SubscribeEvent("OnGrowthHungerUpdate", OnGrowthHungerUpdate);
        EventManager.SubscribeEvent("OnFellowSatietyUpdate", OnFellowSatietyUpdate);
    }

    /// <summary>
    ///     <inheritdoc />
    /// </summary>
    private static void OnGrowthHungerUpdate()
    {
        if (Game.Player.Growth == null)
            return;

        var use = PlayerConfig.Get<bool>("UBot.Protection.checkUseHGP");
        if (!use)
            return;

        var min = PlayerConfig.Get("UBot.Protection.numPetMinHGP", 90);

        var percent = 100.0 * Game.Player.Growth.CurrentHungerPoints / Game.Player.Growth.MaxHungerPoints;
        if (percent < min)
            Game.Player.Growth.UseHungerPotion();
    }

    /// <summary>
    ///     <inheritdoc />
    /// </summary>
    private static void OnFellowSatietyUpdate()
    {
        if (Game.Player.Fellow == null)
            return;

        var use = PlayerConfig.Get<bool>("UBot.Protection.checkUseHGP");
        if (!use)
            return;

        var min = PlayerConfig.Get("UBot.Protection.numPetMinHGP", 90);

        var percent = 100.0 * Game.Player.Fellow.Satiety / 36000;
        if (percent < min)
            Game.Player.Fellow.UseSatietyPotion();
    }
}
