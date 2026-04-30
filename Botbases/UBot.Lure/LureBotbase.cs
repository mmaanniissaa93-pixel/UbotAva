using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Objects;
using UBot.Core.Plugins;
using UBot.Lure.Bundle;
using UBot.Lure.Components;
using System;
using System.Windows.Forms;

namespace UBot.Lure;

public class LureBotbase : IBotbase
{
    private bool _interrupted;

    /// <inheritdoc />
    public string Author => "UBot Team";

    /// <inheritdoc />
    public string Description => "Botbase focused on luring mobs in the best areas of the game.";

    /// <inheritdoc />
    public string Name => "UBot.Lure";

    /// <inheritdoc />
    public string Title => "Lure";

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <inheritdoc />
    public Area Area => LureConfig.Area;

    /// <inheritdoc />
    public void Tick()
    {
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running)
            return;

        if (PickupManager.RunningPlayerPickup)
            return;

        if (Area.Position.DistanceToPlayer() > 80)
        {
            if (!ScriptManager.Running)
                UBot.Core.RuntimeAccess.Events.FireEvent("Bundle.Loop.Start");

            UBot.Core.RuntimeAccess.Events.FireEvent("Bundle.Loop.Invoke");

            return;
        }

        UBot.Core.RuntimeAccess.Events.FireEvent("Bundle.Resurrect.Invoke");
        UBot.Core.RuntimeAccess.Events.FireEvent("Bundle.Buff.Invoke");
        UBot.Core.RuntimeAccess.Events.FireEvent("Bundle.PartyBuffing.Invoke");

        var interruptMessage = LoopConditionValidator.CheckLoopConditions();
        if (interruptMessage != null)
        {
            ScriptManager.Stop();

            if (LureConfig.Area.Position.DistanceToPlayer() > 2)
                UBot.Core.RuntimeAccess.Session.Player.MoveTo(LureConfig.Area.Position);

            if (!_interrupted)
                Log.Warn(interruptMessage);

            _interrupted = true;

            return;
        }

        _interrupted = false;

        if (UBot.Core.RuntimeAccess.Session.Player.HasActiveVehicle)
            UBot.Core.RuntimeAccess.Session.Player.Vehicle.Dismount();

        if (LureConfig.UseHowlingShout)
            HowlingShoutBundle.Tick();

        TargetBundle.Tick();
        AttackBundle.Tick();
        MovementBundle.Tick();

        if (!PickupManager.RunningPlayerPickup && !PickupManager.RunningAbilityPetPickup)
            UBot.Core.RuntimeAccess.Events.FireEvent("Bundle.Loot.Invoke");
    }

    /// <inheritdoc />
    public Control View => Views.View.Main;

    /// <inheritdoc />
    public void Start()
    {
        Log.Notify("[Lure] bot started!");
    }

    /// <inheritdoc />
    public void Stop()
    {
        UBot.Core.RuntimeAccess.Events.FireEvent("Bundle.Loop.Stop");
        UBot.Core.RuntimeAccess.Events.FireEvent("Bundle.Loot.Stop");
        UBot.Core.RuntimeAccess.Events.FireEvent("Bundle.PartyBuffing.Stop");
        UBot.Core.RuntimeAccess.Events.FireEvent("Bundle.Buff.Stop");

        Log.Notify("[Lure] bot stopped!");
    }

    /// <inheritdoc />
    public void Translate()
    {
        LanguageManager.Translate(View, UBot.Core.RuntimeAccess.Core.Language);
    }

    /// <inheritdoc />
    public void Initialize()
    {
        Log.Debug("[Lure] Botbase registered to the kernel!");
    }

    /// <inheritdoc />
    public void Enable()
    {
        if (View != null)
            View.Enabled = true;
    }

    /// <inheritdoc />
    public void Disable()
    {
        if (View != null)
            View.Enabled = false;
    }
}

