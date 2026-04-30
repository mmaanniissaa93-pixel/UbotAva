using UBot.Core;
using UBot.Core.Event;
using UBot.Training.Bundle;

namespace UBot.Training.Subscriber;

internal class TeleportSubscriber
{
    /// <summary>
    ///     Subscribes the events.
    /// </summary>
    public static void SubscribeEvents()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnTeleportStart", OnTeleportStart);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnTeleportComplete", OnTeleportComplete);
    }

    #region Event listeners

    /// <summary>
    ///     Will be triggered when an ingame teleportation was started
    /// </summary>
    private static void OnTeleportStart()
    {
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running)
            return;

        if (Bundles.Loop.Running)
            Bundles.Loop.Stop();
    }

    /// <summary>
    ///     Will be triggered when an ingame teleportation was complete
    /// </summary>
    private static void OnTeleportComplete()
    {
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running)
            return;
    }

    #endregion Event listeners
}
