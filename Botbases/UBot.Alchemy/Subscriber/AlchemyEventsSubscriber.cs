using System;
using System.Linq;
using UBot.Core;
using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Alchemy.Subscriber;

internal class AlchemyEventsSubscriber
{
    public static void Subscribe()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnAlchemyError", new Action<ushort, AlchemyType>(OnAlchemyError));
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnAlchemyDestroyed", new Action<InventoryItem, AlchemyType>(OnAlchemyDestroyed));
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnFuseRequest", new Action<AlchemyAction, AlchemyType>(OnFuseRequest));
    }

    private static void OnAlchemyDestroyed(InventoryItem oldItem, AlchemyType type)
    {
        if (!Bootstrap.IsActive)
            return;

        Globals.Botbase.EnhanceBundleConfig = null;
        Globals.Botbase.MagicBundleConfig = null;

        Globals.View.SelectedItem = null;
        Globals.View.AddLog(
            oldItem.Record.GetRealName(),
            UBot.Core.RuntimeAccess.Session.ReferenceManager.GetTranslation("UIIT_MSG_REINFORCERR_BREAKDOWN")
        );
        Log.Warn("[Alchemy] The item has been destroyed, stopping now...");

        UBot.Core.RuntimeAccess.Core.Bot?.Stop();
    }

    private static void OnAlchemyError(ushort errorCode, AlchemyType type)
    {
        if (!Bootstrap.IsActive)
            return;

        if (errorCode is 0x5423)
            return;

        UBot.Core.RuntimeAccess.Core.Bot?.Stop();

        Log.Error($"[Alchemy] Alchemy fusion error: {errorCode:X}");
    }

    /// <summary>
    ///     Will be triggered if any fuse request (either elixir or magic stone..) was sent to the server. Adds a log message.
    /// </summary>
    /// <param name="action">The alchemy action</param>
    /// <param name="type">The type of alchemy</param>
    private static void OnFuseRequest(AlchemyAction action, AlchemyType type)
    {
        if (AlchemyManager.ActiveAlchemyItems == null)
            return;

        var ingredient = AlchemyManager.ActiveAlchemyItems.ElementAtOrDefault(1);
        var item = AlchemyManager.ActiveAlchemyItems.ElementAtOrDefault(0);

        switch (type)
        {
            case AlchemyType.Elixir:
                Globals.View.AddLog(item?.Record.GetRealName(), $"Fusing elixir [{ingredient.Record.GetRealName()}]");
                break;

            case AlchemyType.MagicStone:
                Globals.View.AddLog(
                    item?.Record.GetRealName(),
                    $"Fusing magic stone [{ingredient.Record.GetRealName()}]"
                );
                break;

            case AlchemyType.AttributeStone:
                Globals.View.AddLog(
                    item?.Record.GetRealName(),
                    $"Fusing attribute stone [{ingredient.Record.GetRealName()}]"
                );
                break;

            default:
                Globals.View.AddLog(item?.Record.GetRealName(), $"Fusing [{ingredient.Record.GetRealName()}]");
                break;
        }
    }
}
