using UBot.Core.Event;
using UBot.Training.Bundle;

namespace UBot.Training.Subscriber;

internal class BundleSubscriber
{
    private static readonly object EventOwner = new();

    public static void SubscribeEvents()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Loop.Start", Bundles.Loop.Start, EventOwner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Loop.Stop", Bundles.Loop.Stop, EventOwner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Loop.Invoke", Bundles.Loop.Invoke, EventOwner);

        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Attack.Stop", Bundles.Attack.Stop, EventOwner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Attack.Invoke", Bundles.Attack.Invoke, EventOwner);

        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Buff.Stop", Bundles.Buff.Stop, EventOwner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Buff.Invoke", Bundles.Buff.Invoke, EventOwner);

        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Berzerk.Stop", Bundles.Berzerk.Stop, EventOwner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Berzerk.Invoke", Bundles.Berzerk.Invoke, EventOwner);

        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Target.Stop", Bundles.Target.Stop, EventOwner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Target.Invoke", Bundles.Target.Invoke, EventOwner);

        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Loot.Stop", Bundles.Loot.Stop, EventOwner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Loot.Invoke", Bundles.Loot.Invoke, EventOwner);

        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Movement.Stop", Bundles.Movement.Stop, EventOwner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Movement.Invoke", Bundles.Movement.Invoke, EventOwner);

        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.PartyBuffing.Stop", Bundles.PartyBuff.Stop, EventOwner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.PartyBuffing.Invoke", Bundles.PartyBuff.Invoke, EventOwner);

        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Resurrect.Stop", Bundles.Resurrect.Stop, EventOwner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Resurrect.Invoke", Bundles.Resurrect.Invoke, EventOwner);

        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Protection.Stop", Bundles.Protection.Stop, EventOwner);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("Bundle.Protection.Invoke", Bundles.Protection.Invoke, EventOwner);
    }

    public static void UnsubscribeAll()
    {
        UBot.Core.RuntimeAccess.Events.UnsubscribeOwner(EventOwner);
    }
}
