using UBot.Core;
using UBot.Core.Event;

namespace UBot.Protection.Components.Pet;

public class CosHGPRecoveryHandler
{
    private static readonly object EventOwner = new();

    /// <summary>
    ///     Initiliazes this instance.
    /// </summary>
    public static void Initiliaze()
    {
        SubscribeEvents();
    }

    /// <summary>
    ///     Subscribes all events (idempotent - clears existing first).
    /// </summary>
    public static void SubscribeAll()
    {
        UnsubscribeAll();
        SubscribeEvents();
    }

    /// <summary>
    ///     Unsubscribes all events.
    /// </summary>
    public static void UnsubscribeAll()
    {
        UBot.Core.RuntimeAccess.Events.UnsubscribeOwner(EventOwner);
    }

    /// <summary>
    ///     Subscribes the events.
    /// </summary>
    private static void SubscribeEvents()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnGrowthHungerUpdate", OnGrowthHungerUpdate, EventOwner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnFellowSatietyUpdate", OnFellowSatietyUpdate, EventOwner);
    }

    /// <summary>
    ///     <inheritdoc />
    /// </summary>
    private static void OnGrowthHungerUpdate()
    {
        if (UBot.Core.RuntimeAccess.Session.Player.Growth == null)
            return;

        var use = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckUseHGP");
        if (!use)
            return;

        var min = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.ThresholdPetMinHGP", 90);

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

        var use = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckUseHGP");
        if (!use)
            return;

        var min = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.ThresholdPetMinHGP", 90);

        var percent = 100.0 * UBot.Core.RuntimeAccess.Session.Player.Fellow.Satiety / 36000;
        if (percent < min)
            UBot.Core.RuntimeAccess.Session.Player.Fellow.UseSatietyPotion();
    }
}
