using System;
using System.Threading;
using System.Threading.Tasks;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Plugins;

namespace UBot.Core;

public class Bot
{
    private static readonly PluginFaultIsolationManager _faultIsolation = new();

    private volatile bool _isStopping;
    private string _lastCriticalFailure;

    public static event Action<string, string, Exception> OnCriticalPluginFailure;

    /// <summary>
    ///     Gets or sets a value indicating whether this <see cref="Bot" /> is running.
    /// </summary>
    /// <value>
    ///     <c>true</c> if running; otherwise, <c>false</c>.
    /// </value>
    public volatile bool Running;

    /// <summary>
    ///     Gets or sets to the <see cref="CancellationToken" />
    /// </summary>
    public CancellationTokenSource TokenSource;

    /// <summary>
    ///     Gets the base.
    /// </summary>
    /// <value>
    ///     The base.
    /// </value>
    public IBotbase Botbase { get; private set; }

    /// <summary>
    ///     Sets the botbase.
    /// </summary>
    /// <param name="botBase">The bot base.</param>
    public void SetBotbase(IBotbase botBase)
    {
        Botbase = botBase;
        botBase.Initialize();

        UBot.Core.RuntimeAccess.Events.FireEvent("OnSetBotbase", botBase);
    }

    /// <summary>
    ///     Starts this instance.
    /// </summary>
    public void Start()
    {
        if (Running || Botbase == null)
            return;

        _isStopping = false;
        _lastCriticalFailure = null;

        TokenSource = new CancellationTokenSource();

        Task.Factory.StartNew(
            async _ =>
            {
                Running = true;

                UBot.Core.RuntimeAccess.Events.FireEvent("OnStartBot");
                Botbase.Start();

                while (!TokenSource.IsCancellationRequested)
                {
                    if (!UBot.Core.RuntimeAccess.Session.Ready)
                    {
                        await Task.Delay(100);
                        continue;
                    }

                    ExecuteBotbaseTickSafely();

                    await Task.Delay(100);
                }
            },
            TokenSource.Token,
            TaskCreationOptions.LongRunning
        );
    }

    private void ExecuteBotbaseTickSafely()
    {
        if (Botbase == null)
            return;

        var botbaseName = Botbase.Name ?? Botbase.GetType().Name;
        var tier = ResolveBotbaseTier(botbaseName);
        var policy = ResolveBotbaseRestartPolicy(botbaseName);

        if (tier == "critical")
        {
            ExecuteCriticalTierTick(botbaseName);
        }
        else
        {
            ExecuteStandardTierTick(botbaseName, policy);
        }
    }

    private string ResolveBotbaseTier(string botbaseName)
    {
        if (!ExtensionManager.HasPluginContract(botbaseName))
        {
            Log.Warn($"Botbase [{botbaseName}] has no manifest; treating Tick failure as critical.");
            return "critical";
        }

        return ExtensionManager.GetPluginTier(botbaseName);
    }

    private PluginRestartPolicyManifest ResolveBotbaseRestartPolicy(string botbaseName)
    {
        var policy = ExtensionManager.GetRestartPolicy(botbaseName);

        if (!policy.Enabled || policy.MaxRestarts == 0)
        {
            return new PluginRestartPolicyManifest
            {
                Enabled = false,
                MaxRestarts = 0,
                WindowSeconds = 60,
                BaseDelayMs = 250,
                MaxDelayMs = 3000
            };
        }

        return policy;
    }

    private void ExecuteCriticalTierTick(string botbaseName)
    {
        if (_isStopping || !Running)
            return;

        if (_lastCriticalFailure == botbaseName)
            return;

        if (!_faultIsolation.TryExecute(botbaseName, "tick", new PluginRestartPolicyManifest { Enabled = false }, Botbase.Tick, out var failure))
        {
            _lastCriticalFailure = botbaseName;
            Log.Error($"CRITICAL: Botbase [{botbaseName}] failed in critical tier. Stopping bot to prevent further damage.");
            OnCriticalPluginFailure?.Invoke(botbaseName, "tick", failure);

            try
            {
                Stop();
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to stop bot after critical failure: {ex.Message}");
            }
        }
    }

    private void ExecuteStandardTierTick(string botbaseName, PluginRestartPolicyManifest policy)
    {
        if (_isStopping || !Running)
            return;

        if (!_faultIsolation.TryExecute(botbaseName, "tick", policy, Botbase.Tick, out var failure))
        {
            Log.Warn($"Botbase [{botbaseName}] Tick() failed after {policy.MaxRestarts} restart attempts. Continuing loop.");
        }
    }

    /// <summary>
    ///     Stops this instance.
    /// </summary>
    public void Stop()
    {
        if (_isStopping)
            return;

        _isStopping = true;

        ScriptManager.Stop();
        ShoppingManager.Stop();
        PickupManager.Stop();

        if (Botbase == null)
            return;

        if (!Running)
            return;

        if (!TokenSource.IsCancellationRequested)
            TokenSource.Cancel();

        UBot.Core.RuntimeAccess.Events.FireEvent("OnStopBot");
        Log.Notify($"Stopping bot {Botbase.Title}");

        UBot.Core.RuntimeAccess.Session.SelectedEntity = null;
        Botbase.Stop();
        Running = false;

        Log.Notify($"Stoped bot {Botbase.Title}");
        Log.Status("Bot stopped");
    }
}
