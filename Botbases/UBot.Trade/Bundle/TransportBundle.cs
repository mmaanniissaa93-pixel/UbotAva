using System;
using System.Linq;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Objects;
using UBot.Core.Objects.Cos;
using UBot.Core.Objects.Spawn;
using UBot.Trade.Components;

namespace UBot.Trade.Bundle;

internal class TransportBundle
{
    /// <summary>
    ///     A value indicating if the bot is waiting for transport.
    /// </summary>
    public bool WaitingForTransport { get; private set; }

    /// <summary>
    ///     A value indicating if the transport is stuck.
    /// </summary>
    public bool TransportStuck { get; private set; }

    /// <summary>
    ///     A value indicating if the bundle is currently busy and should block further command execution.
    /// </summary>
    public bool Busy => WaitingForTransport || TransportStuck;

    /// <summary>
    ///     Initializes the bot base
    /// </summary>
    public void Initialize()
    {
        SubscribeEvents();
    }

    /// <summary>
    ///     Starts the bundle.
    /// </summary>
    public void Start()
    {
        WaitingForTransport = false;
        TransportStuck = false;
    }

    /// <summary>
    ///     Subscribes the events.
    /// </summary>
    private void SubscribeEvents()
    {
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnJobCosStuck", new Action<byte>(OnJobCosStuck));
    }

    /// <summary>
    ///     Triggered when the server sends the cos stuck packet.
    /// </summary>
    /// <param name="reason"></param>
    private void OnJobCosStuck(byte reason)
    {
        if (TransportStuck)
            return;

        //ToDO: Better unstack mechanic for trade transports.
        Log.Warn("[Trade] Your transport is stuck! Go back to your transport and try to unstuck it.");
        UBot.Core.RuntimeAccess.Session.ShowNotification("[UBot] Your transport is stuck! Go back to your transport and try to unstuck it.");

        //TransportStuck = true;
    }

    public void Tick()
    {
        // Summon new transport?
        if (!Bundles.RouteBundle.ScriptManaggerIsRunning && UBot.Core.RuntimeAccess.Session.Player.JobTransport == null)
        {
            if (UBot.Core.RuntimeAccess.Session.Player.State.BattleState == BattleState.InBattle || UBot.Core.RuntimeAccess.Session.Player.InAction)
                return;

            var jobTransportItem = Game
                .Player.Inventory.GetNormalPartItems(i => i.Record.CodeName.Contains("COS_T_") && i.Record.Tid == 4588)
                .FirstOrDefault();

            if (jobTransportItem != null)
            {
                Log.Notify($"[Trade] Summoning transport [{jobTransportItem.Record.GetRealName()}]");
                jobTransportItem.Use();

                return;
            }

            Log.Warn("[Trade] Can not summon transport: No transport scroll in player inventory.");

            UBot.Core.RuntimeAccess.Core.Bot.Stop();

            return;
        }

        //Wait for certain things?
        if (!CheckDistanceToTransport() || !CheckTransportIsUnderAttack())
            return;

        if (
            TradeConfig.MountTransport
            && !UBot.Core.RuntimeAccess.Session.Player.HasActiveVehicle
            && UBot.Core.RuntimeAccess.Session.Player.State.BattleState == BattleState.InPeace
            && !UBot.Core.RuntimeAccess.Session.Player.InAction
        )
        {
            Log.Notify("[Trade] Mounting transport");

            WaitingForTransport = true;

            UBot.Core.RuntimeAccess.Session.Player.MoveTo(UBot.Core.RuntimeAccess.Session.Player.JobTransport.Position);
            UBot.Core.RuntimeAccess.Session.Player.JobTransport?.Mount();
        }
        else if (
            !TradeConfig.MountTransport
            && UBot.Core.RuntimeAccess.Session.Player.HasActiveVehicle
            && UBot.Core.RuntimeAccess.Session.Player.Vehicle.UniqueId == UBot.Core.RuntimeAccess.Session.Player.JobTransport.UniqueId
        )
        {
            UBot.Core.RuntimeAccess.Session.Player.JobTransport?.Dismount();
        }
    }

    /// <summary>
    ///     Checks the vehicle distance to the player.
    /// </summary>
    /// s
    private bool CheckDistanceToTransport()
    {
        if (UBot.Core.RuntimeAccess.Session.Player.JobTransport == null)
        {
            WaitingForTransport = true;

            Log.Debug("[Trade] Waiting for job transport to spawn.");

            return false;
        }

        //Player is mounted
        if (UBot.Core.RuntimeAccess.Session.Player.HasActiveVehicle && UBot.Core.RuntimeAccess.Session.Player.Vehicle.UniqueId == UBot.Core.RuntimeAccess.Session.Player.JobTransport.UniqueId)
        {
            WaitingForTransport = false;
            TransportStuck = false;

            return true;
        }

        var currentDistance = UBot.Core.RuntimeAccess.Session.Player.JobTransport.Position.DistanceToPlayer();
        if (currentDistance > TradeConfig.MaxTransportDistance)
        {
            WaitingForTransport = true;

            //In some rare cases, the position of the transport is wrong (e.g. Knock-Back skills)
            // That makes the bot thinking that the cos is actually very far away. To re-run the distance calculation
            // we can move the player to the location were it thinks the COS is located. That way the distance is then re-calculated
            // and the script can continue.
            if (!UBot.Core.RuntimeAccess.Session.Player.JobTransport.Movement.HasDestination)
            {
                Log.Debug("[Trade] Lost track of the transport! Now trying to move to last known location.");
                UBot.Core.RuntimeAccess.Session.Player.MoveTo(UBot.Core.RuntimeAccess.Session.Player.JobTransport.Position);

                return false;
            }

            Log.Status($"Waiting for transport ({currentDistance:F1}m)");

            return false;
        }

        //ToDo: Check if job transport is stuck. Because the collision files are wrong at certain locations like Hotan
        //it can not be done until the collision has been fixed.
        WaitingForTransport = false;

        return true;
    }

    /// <summary>
    ///     Checks if the vehicle is under attack
    /// </summary>
    public bool CheckTransportIsUnderAttack()
    {
        if (!TradeConfig.ProtectTransport)
            return true;

        if (UBot.Core.RuntimeAccess.Session.Player.JobTransport == null)
            return true;

        if (UBot.Core.RuntimeAccess.Session.Player.JobTransport is not JobTransport jobTransport)
            return true;

        if (!SpawnManager.TryGetEntity<SpawnedBionic>(jobTransport.UniqueId, out var bionic))
            return true;

        return bionic.GetAttackers().Count == 0;
    }

    /// <summary>
    ///     Stops this instance.
    /// </summary>
    public void Stop()
    {
        WaitingForTransport = false;
        TransportStuck = false;
    }
}
