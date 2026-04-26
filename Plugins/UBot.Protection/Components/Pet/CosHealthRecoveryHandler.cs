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
        EventManager.SubscribeEvent("OnGrowthHealthUpdate", OnGrowthHealthUpdate, Owner);
        EventManager.SubscribeEvent("OnFellowHealthUpdate", OnFellowHealthUpdate, Owner);
        EventManager.SubscribeEvent("OnUpdateTransportHealth", OnUpdateTransportHealth, Owner);
        EventManager.SubscribeEvent("OnUpdateJobTransportHealth", OnUpdateJobTransportHealth, Owner);
    }

    /// <summary>
    ///     Unsubscribes all events.
    /// </summary>
    public static void UnsubscribeAll()
    {
        EventManager.UnsubscribeOwner(Owner);
    }

    /// <summary>
    ///     Cores the on pet health update.
    /// </summary>
    private static void OnCosHealthUpdate(Cos cos)
    {
        var useHPPotions = PlayerConfig.Get<bool>("UBot.Protection.checkUsePetHP");
        if (!useHPPotions)
            return;

        var minHp = PlayerConfig.Get("UBot.Protection.numPetMinHP", 80);

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
        OnCosHealthUpdate(Game.Player.Growth);
    }

    /// <summary>
    ///     Cores the on pet health update.
    /// </summary>
    private static void OnFellowHealthUpdate()
    {
        OnCosHealthUpdate(Game.Player.Fellow);
    }

    /// <summary>
    ///     Cores the on pet health update.
    /// </summary>
    private static void OnUpdateTransportHealth()
    {
        OnCosHealthUpdate(Game.Player.Transport);
    }

    /// <summary>
    ///     Cores the on pet health update.
    /// </summary>
    private static void OnUpdateJobTransportHealth()
    {
        OnCosHealthUpdate(Game.Player.JobTransport);
    }
}
