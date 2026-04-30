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
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnGrowthHungerUpdate", OnGrowthHungerUpdate);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnFellowSatietyUpdate", OnFellowSatietyUpdate);
    }

    /// <summary>
    ///     <inheritdoc />
    /// </summary>
    private static void OnGrowthHungerUpdate()
    {
        if (UBot.Core.RuntimeAccess.Session.Player.Growth == null)
            return;

        var use = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.checkUseHGP");
        if (!use)
            return;

        var min = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.numPetMinHGP", 90);

        var percent = 100.0 * UBot.Core.RuntimeAccess.Session.Player.Growth.CurrentHungerPoints / UBot.Core.RuntimeAccess.Session.Player.Growth.MaxHungerPoints;
        if (percent < min)
            UBot.Core.RuntimeAccess.Session.Player.Growth.UseHungerPotion();
    }

    /// <summary>
    ///     <inheritdoc />
    /// </summary>
    private static void OnFellowSatietyUpdate()
    {
        if (UBot.Core.RuntimeAccess.Session.Player.Fellow == null)
            return;

        var use = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.checkUseHGP");
        if (!use)
            return;

        var min = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.numPetMinHGP", 90);

        var percent = 100.0 * UBot.Core.RuntimeAccess.Session.Player.Fellow.Satiety / 36000;
        if (percent < min)
            UBot.Core.RuntimeAccess.Session.Player.Fellow.UseSatietyPotion();
    }
}
