using UBot.Core;
using UBot.Core.Event;
using UBot.Core.Objects;
using UBot.Core.Objects.Cos;

namespace UBot.Protection.Components.Pet;

public static class CosHealthRecoveryHandler
{
    private static readonly object Owner = new object();

    /// <summary>
    ///     Initializes this instance.
    /// </summary>
    public static void Initialize()
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
        UBot.Core.RuntimeAccess.Events.UnsubscribeOwner(Owner);
    }

    /// <summary>
    ///     Subscribes the events.
    /// </summary>
    private static void SubscribeEvents()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnGrowthHealthUpdate", OnGrowthHealthUpdate, Owner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnFellowHealthUpdate", OnFellowHealthUpdate, Owner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnUpdateTransportHealth", OnUpdateTransportHealth, Owner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnUpdateJobTransportHealth", OnUpdateJobTransportHealth, Owner);
    }

    /// <summary>
    ///     Cores the on pet health update.
    /// </summary>
    private static void OnCosHealthUpdate(Cos cos)
    {
        var useHPPotions = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckUsePetHP");
        if (!useHPPotions)
            return;

        var minHp = UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.ThresholdPetMinHP", 80);

        if (cos == null)
            return;

        if ((cos.BadEffect & BadEffect.Zombie) == BadEffect.Zombie)
            return;

        var percent = 100.0 * cos.Health / cos.MaxHealth;
        if (percent < minHp)
            cos.UseHealthPotion();
    }

    /// <summary>
    ///     Cores the on pet health update.
    /// </summary>
    private static void OnGrowthHealthUpdate()
    {
        OnCosHealthUpdate(UBot.Core.RuntimeAccess.Session.Player.Growth);
    }

    /// <summary>
    ///     Cores the on pet health update.
    /// </summary>
    private static void OnFellowHealthUpdate()
    {
        OnCosHealthUpdate(UBot.Core.RuntimeAccess.Session.Player.Fellow);
    }

    /// <summary>
    ///     Cores the on pet health update.
    /// </summary>
    private static void OnUpdateTransportHealth()
    {
        OnCosHealthUpdate(UBot.Core.RuntimeAccess.Session.Player.Transport);
    }

    /// <summary>
    ///     Cores the on pet health update.
    /// </summary>
    private static void OnUpdateJobTransportHealth()
    {
        OnCosHealthUpdate(UBot.Core.RuntimeAccess.Session.Player.JobTransport);
    }
}
