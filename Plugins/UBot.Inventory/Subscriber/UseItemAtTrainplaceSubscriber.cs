using System;
using System.Collections.Generic;
using UBot.Core;
using UBot.Core.Event;

namespace UBot.Inventory.Subscriber;

internal class UseItemAtTrainplaceSubscriber
{
    private static readonly List<string> _blacklistedItems = new();
    private static long _lastTick;

    public static void SubscribeEvents()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnTick", OnTick);
    }

    private static void OnTick()
    {
        //Retry blacklisted items after 5 minutes
        if (TimeSpan.FromMilliseconds(UBot.Core.RuntimeAccess.Core.TickCount - _lastTick).Minutes >= 5)
            _blacklistedItems.Clear();

        _lastTick = UBot.Core.RuntimeAccess.Core.TickCount;

        if (!UBot.Core.RuntimeAccess.Core.Bot.Running || UBot.Core.RuntimeAccess.Core.Bot.Botbase.Area.Position.Region == 0)
            return;

        //Only at training place
        if (UBot.Core.RuntimeAccess.Core.Bot.Botbase.Area.Position.DistanceToPlayer() > 100)
            return;

        var itemsToUse = UBot.Core.RuntimeAccess.Player.GetArray<string>("UBot.Inventory.ItemsAtTrainplace");

        foreach (var item in itemsToUse)
        {
            if (_blacklistedItems.Contains(item))
                continue;

            var invItem = UBot.Core.RuntimeAccess.Session.Player.Inventory.GetItem(item);
            if (invItem == null)
                continue;

            if (invItem.ItemSkillInUse)
                continue;

            Log.Notify($"Use [{invItem.Record.GetRealName()}] at training place");

            if (invItem.Use())
                continue;

            //e.g. overlapping with another buff
            _blacklistedItems.Add(invItem.Record.CodeName);

            Log.Warn(
                $"Can not use item [{invItem.Record.GetRealName()}] at training place. Blacklisting it for 5 minutes before next try."
            );
        }
    }
}
