using System.Threading;
using System.Threading.Tasks;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;

namespace UBot.General.Components;

internal static class AutoLoginRuntimeFeatures
{
    private static readonly object Owner = new object();
    private static int _sessionSequence;
    private static int _actionSequence;
    private static int _reconnectSequence;
    private static int _initialized;

    public static void Initialize()
    {
        if (Interlocked.Exchange(ref _initialized, 1) == 1)
            return;

        EventManager.SubscribeEvent("OnEnterGame", new System.Action(OnEnterGame), Owner);
        EventManager.SubscribeEvent("OnLoadCharacter", new System.Action(OnLoadCharacter), Owner);
        EventManager.SubscribeEvent("OnStartClient", new System.Action(OnStartClient), Owner);
        EventManager.SubscribeEvent("OnAgentServerDisconnected", new System.Action(OnAgentServerDisconnected), Owner);
    }

    public static void UnsubscribeAll()
    {
        EventManager.UnsubscribeOwner(Owner);
    }

    private static void OnEnterGame()
    {
        Interlocked.Increment(ref _sessionSequence);
    }

    private static void OnStartClient()
    {
        if (!GlobalConfig.Get("UBot.General.EnableAutomatedLogin", false))
            return;

        if (!GlobalConfig.Get("UBot.General.HideOnStartClient", false))
            return;

        _ = Task.Run(async () =>
        {
            await Task.Delay(900);
            ClientManager.SetVisible(false);
        });
    }

    private static void OnLoadCharacter()
    {
        var trackedSession = Volatile.Read(ref _sessionSequence);
        var actionSequence = Interlocked.Increment(ref _actionSequence);

        _ = Task.Run(async () =>
        {
            // Wait for state/inventory synchronization.
            await Task.Delay(500);

            if (actionSequence != Volatile.Read(ref _actionSequence))
                return;

            if (!GlobalConfig.Get("UBot.General.EnableAutomatedLogin", false))
                return;

            if (GlobalConfig.Get("UBot.General.HideOnStartClient", false))
                ClientManager.SetVisible(false);

            if (GlobalConfig.Get("UBot.General.UseReturnScroll", false))
                await TryUseReturnScrollAsync(actionSequence);

            if (GlobalConfig.Get("UBot.General.StartBot", false))
                await TryStartBotAsync(actionSequence, trackedSession);
        });
    }

    private static void OnAgentServerDisconnected()
    {
        if (Game.Clientless)
            return;

        if (!GlobalConfig.Get("UBot.General.EnableAutomatedLogin", false))
            return;

        var reconnectSequence = Interlocked.Increment(ref _reconnectSequence);

        _ = Task.Run(async () =>
        {
            var delay = 10000;
            if (GlobalConfig.Get("UBot.General.EnableWaitAfterDC", false))
                delay = GlobalConfig.Get("UBot.General.WaitAfterDC", 3) * 60 * 1000;

            Log.Warn($"Agent disconnected. Reconnecting in {delay / 1000} seconds...");
            await Task.Delay(delay);

            if (reconnectSequence != Volatile.Read(ref _reconnectSequence))
                return;

            if (Kernel.Proxy != null && (Kernel.Proxy.IsConnectedToGatewayserver || Kernel.Proxy.IsConnectedToAgentserver))
                return;

            try
            {
                if (ClientManager.IsRunning)
                    ClientManager.Kill();
            }
            catch
            {
                // ignore and continue with reconnect
            }

            Game.Start();
            await Task.Delay(800);

            var started = await ClientManager.Start();
            if (!started)
                Log.Error("Auto reconnect after DC failed: client could not be started.");
        });
    }

    private static async Task TryUseReturnScrollAsync(int actionSequence)
    {
        const int maxAttempts = 12;
        for (var i = 0; i < maxAttempts; i++)
        {
            if (actionSequence != Volatile.Read(ref _actionSequence))
                return;

            var player = Game.Player;
            if (player != null && player.UseReturnScroll())
                return;

            await Task.Delay(650);
        }
    }

    private static async Task TryStartBotAsync(int actionSequence, int sessionSequence)
    {
        const int maxAttempts = 20;

        for (var i = 0; i < maxAttempts; i++)
        {
            if (actionSequence != Volatile.Read(ref _actionSequence))
                return;

            if (sessionSequence != Volatile.Read(ref _sessionSequence))
                return;

            if (Kernel.Bot != null && Kernel.Bot.Botbase != null)
            {
                if (!Kernel.Bot.Running)
                    Kernel.Bot.Start();

                return;
            }

            await Task.Delay(500);
        }
    }
}
