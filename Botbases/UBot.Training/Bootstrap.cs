using System;
using System.Windows.Forms;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Objects;
using UBot.Core.Plugins;
using UBot.Training.Bot;
using UBot.Training.Bundle;
using UBot.Training.Components;
using UBot.Training.Subscriber;

namespace UBot.Training;

public class Bootstrap : IBotbase
{
    /// <inheritdoc />
    public string Author => "UBot Team";

    /// <inheritdoc />
    public string Description => "Botbase focused on training in the best areas of the game.";

    /// <inheritdoc />
    public string Name => "UBot.Training";

    /// <inheritdoc />
    public string Title => "Training";

    /// <inheritdoc />
    public string Version => "1.0.0";

    /// <inheritdoc />
    public bool Enabled { get; set; }

    /// <inheritdoc />
    public Area Area => Container.Bot.Area;

    /// <inheritdoc />
    public void Tick()
    {
        if (!UBot.Core.RuntimeAccess.Core.Bot.Running)
            return;

        if (UBot.Core.RuntimeAccess.Session.Player.Exchanging)
            return;

        if (UBot.Core.RuntimeAccess.Session.Player.Untouchable)
            return;

        if (UBot.Core.RuntimeAccess.Session.Player.State.LifeState == LifeState.Dead)
            return;

        //Begin the loopback if needed
        if (Container.Bot.Area.Position.DistanceToPlayer() > 80)
            Bundles.Loop.Start();

        if (Bundles.Loop.Running)
            return;

        //Nothing if in scroll state!
        if (
            UBot.Core.RuntimeAccess.Session.Player.State.ScrollState == ScrollState.NormalScroll
            || UBot.Core.RuntimeAccess.Session.Player.State.ScrollState == ScrollState.ThiefScroll
        )
            return;

        try
        {
            Container.Bot.Tick();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex);
        }
    }

    /// <inheritdoc />
    public Control View => Container.View;

    /// <inheritdoc />
    public void Start()
    {
        if (UBot.Core.RuntimeAccess.Core.Bot.Botbase.Area.Position.X == 0)
        {
            Log.WarnLang("ConfigureTrainingAreaBeforeStartBot");
            UBot.Core.RuntimeAccess.Core.Bot.Stop();
        }

        //Already reloading when config saved via ConfigSubscriber
        //Bundles.Reload();
        //Container.Bot.Reload();
    }

    /// <inheritdoc />
    public void Stop()
    {
        lock (Container.Lock)
        {
            if (UBot.Core.RuntimeAccess.Session.Player.InAction)
                SkillManager.CancelAction();

            Bundles.Stop();
        }
    }

    /// <inheritdoc />
    public void Translate()
    {
        LanguageManager.Translate(View, UBot.Core.RuntimeAccess.Core.Language);
    }

    /// <inheritdoc />
    public void Initialize()
    {
        Container.Lock = new object();
        Container.Bot = new Botbase();

        //Bundles.Reload();

        BundleSubscriber.SubscribeEvents();
        ConfigSubscriber.SubscribeEvents();
        TeleportSubscriber.SubscribeEvents();

        ScriptManager.CommandHandlers.Add(new TrainingAreaScriptCommand());
        Log.Debug("[Training] Botbase registered to the kernel!");
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
        BundleSubscriber.UnsubscribeAll();

        if (View != null)
            View.Enabled = false;
    }
}

