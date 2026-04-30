using System;
using System.Threading;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Objects;
using UBot.Training.Bundle;

namespace UBot.Training.Bot;

internal class Botbase
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Botbase" /> class.
    /// </summary>
    public Botbase()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnSetTrainingArea", Reload);
    }

    /// <summary>
    ///     Gets the area.
    /// </summary>
    /// <value>
    ///     The area.
    /// </value>
    public Area Area { get; private set; }

    /// <summary>
    ///     Reloads this instance by re-reading the configuration.
    /// </summary>
    public void Reload()
    {
        Area = new Area
        {
            Position = new Position(
                UBot.Core.RuntimeAccess.Player.Get<ushort>("UBot.Area.Region"),
                UBot.Core.RuntimeAccess.Player.Get<float>("UBot.Area.X"),
                UBot.Core.RuntimeAccess.Player.Get<float>("UBot.Area.Y"),
                UBot.Core.RuntimeAccess.Player.Get<float>("UBot.Area.Z")
            ),
            Radius = Math.Clamp(UBot.Core.RuntimeAccess.Player.Get("UBot.Area.Radius", 50), 5, 100),
        };
    }

    /// <summary>
    ///     Ticks this instance.
    /// </summary>
    public void Tick()
    {
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running)
            return;

        if (UBot.Core.RuntimeAccess.Session.Player.HasActiveVehicle)
        {
            UBot.Core.RuntimeAccess.Session.Player.Vehicle.Dismount();
            Thread.Sleep(1000);
        }

        //Wait for the pickup manager to finish
        if (PickupManager.RunningPlayerPickup)
            return;

        if (
            Bundles.Loop.Config.UseSpeedDrug
            && UBot.Core.RuntimeAccess.Session.Player.State.ActiveBuffs.FindIndex(p => p.Record.Params.Contains(1752396901)) < 0
        )
        {
            var item = UBot.Core.RuntimeAccess.Session.Player.Inventory.GetItem(
                new TypeIdFilter(3, 3, 13, 1),
                p => p.Record.Desc1.Contains("_SPEED_")
            );
            item?.Use();
        }

        var noAttack = UBot.Core.RuntimeAccess.Player.Get("UBot.Skills.checkBoxNoAttack", false);

        //Check for protection
        Bundles.Protection.Invoke();

        //Resurrect party members if needed
        Bundles.Resurrect.Invoke();

        //Cast buffs
        Bundles.Buff.Invoke();

        // Buff the configured party members if needed
        Bundles.PartyBuff.Invoke();

        //Loot items
        Bundles.Loot.Invoke();

        //Select next target
        if (!noAttack)
            Bundles.Target.Invoke();

        //Check for berzerk
        Bundles.Berzerk.Invoke();

        //Cast skill against enemy
        if (!noAttack)
            Bundles.Attack.Invoke();

        //Move around (maybe)
        Bundles.Movement.Invoke();
    }
}
