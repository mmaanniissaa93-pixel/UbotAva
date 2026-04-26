using UBot.Core.Event;
using UBot.Training.Bundle;

namespace UBot.Training.Subscriber;

internal class BundleSubscriber
{
    private static readonly object EventOwner = new();

    public static void SubscribeEvents()
    {
        EventManager.SubscribeEvent("Bundle.Loop.Start", Bundles.Loop.Start, EventOwner);
        EventManager.SubscribeEvent("Bundle.Loop.Stop", Bundles.Loop.Stop, EventOwner);
        EventManager.SubscribeEvent("Bundle.Loop.Invoke", Bundles.Loop.Invoke, EventOwner);

        EventManager.SubscribeEvent("Bundle.Attack.Stop", Bundles.Attack.Stop, EventOwner);
        EventManager.SubscribeEvent("Bundle.Attack.Invoke", Bundles.Attack.Invoke, EventOwner);

        EventManager.SubscribeEvent("Bundle.Buff.Stop", Bundles.Buff.Stop, EventOwner);
        EventManager.SubscribeEvent("Bundle.Buff.Invoke", Bundles.Buff.Invoke, EventOwner);

        EventManager.SubscribeEvent("Bundle.Berzerk.Stop", Bundles.Berzerk.Stop, EventOwner);
        EventManager.SubscribeEvent("Bundle.Berzerk.Invoke", Bundles.Berzerk.Invoke, EventOwner);

        EventManager.SubscribeEvent("Bundle.Target.Stop", Bundles.Target.Stop, EventOwner);
        EventManager.SubscribeEvent("Bundle.Target.Invoke", Bundles.Target.Invoke, EventOwner);

        EventManager.SubscribeEvent("Bundle.Loot.Stop", Bundles.Loot.Stop, EventOwner);
        EventManager.SubscribeEvent("Bundle.Loot.Invoke", Bundles.Loot.Invoke, EventOwner);

        EventManager.SubscribeEvent("Bundle.Movement.Stop", Bundles.Movement.Stop, EventOwner);
        EventManager.SubscribeEvent("Bundle.Movement.Invoke", Bundles.Movement.Invoke, EventOwner);

        EventManager.SubscribeEvent("Bundle.PartyBuffing.Stop", Bundles.PartyBuff.Stop, EventOwner);
        EventManager.SubscribeEvent("Bundle.PartyBuffing.Invoke", Bundles.PartyBuff.Invoke, EventOwner);

        EventManager.SubscribeEvent("Bundle.Resurrect.Stop", Bundles.Resurrect.Stop, EventOwner);
        EventManager.SubscribeEvent("Bundle.Resurrect.Invoke", Bundles.Resurrect.Invoke, EventOwner);

        EventManager.SubscribeEvent("Bundle.Protection.Stop", Bundles.Protection.Stop, EventOwner);
        EventManager.SubscribeEvent("Bundle.Protection.Invoke", Bundles.Protection.Invoke, EventOwner);
    }

    public static void UnsubscribeAll()
    {
        EventManager.UnsubscribeOwner(EventOwner);
    }
}
