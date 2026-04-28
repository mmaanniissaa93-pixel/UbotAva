using System.Reflection;
using System.Threading.Tasks;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Network;
using Xunit;

namespace UBot.Core.Tests;

public class ClientlessManagerTests
{
    [Fact]
    public void GoClientless_ShouldStartOnlyOneKeepAliveWorker()
    {
        using var scope = ClientlessScope.Create(agentConnected: true);

        ClientlessManager.GoClientless();
        var firstTask = scope.GetKeepAliveTask();

        ClientlessManager.GoClientless();
        var secondTask = scope.GetKeepAliveTask();

        Assert.NotNull(firstTask);
        Assert.Same(firstTask, secondTask);
    }

    [Fact]
    public void AgentDisconnect_ShouldStopKeepAliveAndStartSingleReloginWorker()
    {
        using var scope = ClientlessScope.Create(agentConnected: true);
        ClientlessManager.Initialize();
        ClientlessManager.GoClientless();

        EventManager.FireEvent("OnAgentServerDisconnected");
        var firstReloginTask = scope.GetReloginTask();

        EventManager.FireEvent("OnAgentServerDisconnected");
        var secondReloginTask = scope.GetReloginTask();

        Assert.NotNull(firstReloginTask);
        Assert.Same(firstReloginTask, secondReloginTask);
    }

    private sealed class ClientlessScope : System.IDisposable
    {
        private static readonly FieldInfo KeepAliveTaskField =
            typeof(ClientlessManager).GetField("_keepAliveTask", BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly FieldInfo ReloginTaskField =
            typeof(ClientlessManager).GetField("_reloginTask", BindingFlags.NonPublic | BindingFlags.Static);

        private static readonly PropertyInfo AgentConnectedProperty =
            typeof(Proxy).GetProperty("IsConnectedToAgentserver", BindingFlags.Public | BindingFlags.Instance);

        private readonly Proxy _previousProxy;
        private readonly bool _previousClientless;

        private ClientlessScope(Proxy previousProxy, bool previousClientless)
        {
            _previousProxy = previousProxy;
            _previousClientless = previousClientless;
        }

        public static ClientlessScope Create(bool agentConnected)
        {
            ClientlessManager.Shutdown();

            var previousProxy = Kernel.Proxy;
            var previousClientless = Game.Clientless;
            var proxy = new Proxy();
            AgentConnectedProperty.SetValue(proxy, agentConnected);

            Kernel.Proxy = proxy;
            Game.Clientless = false;

            return new ClientlessScope(previousProxy, previousClientless);
        }

        public Task GetKeepAliveTask() => (Task)KeepAliveTaskField.GetValue(null);

        public Task GetReloginTask() => (Task)ReloginTaskField.GetValue(null);

        public void Dispose()
        {
            ClientlessManager.Shutdown();
            Kernel.Proxy = _previousProxy;
            Game.Clientless = _previousClientless;
        }
    }
}
