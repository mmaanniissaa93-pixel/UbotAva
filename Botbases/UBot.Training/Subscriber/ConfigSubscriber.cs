using UBot.Core.Event;
using UBot.Training.Bundle;

namespace UBot.Training.Subscriber;

internal class ConfigSubscriber
{
    /// <summary>
    ///     Subscribes the events.
    /// </summary>
    public static void SubscribeEvents()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnEnterGame", ReloadSettings);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnSavePlayerConfig", ReloadSettings);
    }

    /// <summary>
    ///     Configurations the subscriber on save player settings.
    /// </summary>
    private static void ReloadSettings()
    {
        if (Container.Lock == null || Container.Bot == null)
            return;

        lock (Container.Lock)
        {
            //Reload the botbase config
            Container.Bot.Reload();

            //Reload all bundles to apply the new configuration
            Bundles.Reload();
        }
    }
}
