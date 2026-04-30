using System.Reflection;
using System.Threading.Tasks;
using UBot.Core.Abstractions.Services;
using UBot.Core.Components;
using UBot.Core.Services;
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

        ClientlessManager.OnAgentServerDisconnected();
        var firstReloginTask = scope.GetReloginTask();

        ClientlessManager.OnAgentServerDisconnected();
        var secondReloginTask = scope.GetReloginTask();

        Assert.NotNull(firstReloginTask);
        Assert.Same(firstReloginTask, secondReloginTask);
    }

    private sealed class ClientlessScope : System.IDisposable
    {
        private static readonly FieldInfo KeepAliveTaskField =
            typeof(ClientlessService).GetField("_keepAliveTask", BindingFlags.NonPublic | BindingFlags.Instance);

        private static readonly FieldInfo ReloginTaskField =
            typeof(ClientlessService).GetField("_reloginTask", BindingFlags.NonPublic | BindingFlags.Instance);

        private readonly IClientConnectionRuntime _previousRuntime;
        private readonly IClientlessService _previousService;
        private readonly ClientlessService _service;

        private ClientlessScope(
            IClientConnectionRuntime previousRuntime,
            IClientlessService previousService,
            ClientlessService service
        )
        {
            _previousRuntime = previousRuntime;
            _previousService = previousService;
            _service = service;
        }

        public static ClientlessScope Create(bool agentConnected)
        {
            ClientlessManager.Shutdown();

            var previousRuntime = UBot.Core.RuntimeAccess.Services.ClientConnectionRuntime;
            var previousService = UBot.Core.RuntimeAccess.Services.Clientless;
            var service = new ClientlessService();

            UBot.Core.RuntimeAccess.Services.ClientConnectionRuntime = new FakeClientConnectionRuntime(agentConnected);
            ClientlessManager.Initialize(service);

            return new ClientlessScope(previousRuntime, previousService, service);
        }

        public Task GetKeepAliveTask() => (Task)KeepAliveTaskField.GetValue(_service);

        public Task GetReloginTask() => (Task)ReloginTaskField.GetValue(_service);

        public void Dispose()
        {
            ClientlessManager.Shutdown();
            UBot.Core.RuntimeAccess.Services.ClientConnectionRuntime = _previousRuntime;
            UBot.Core.RuntimeAccess.Services.Clientless = _previousService;
        }
    }

    private sealed class FakeClientConnectionRuntime : IClientConnectionRuntime
    {
        public FakeClientConnectionRuntime(bool agentConnected)
        {
            IsConnectedToAgentServer = agentConnected;
        }

        public bool IsClientless { get; set; }
        public bool IsConnectedToGatewayServer => true;
        public bool IsConnectedToAgentServer { get; }

        public int GetReloginDelayMilliseconds() => 10000;
        public void ShutdownClientConnection() { }
        public void StartGame() { }
        public void SendServerListRequest() { }
        public void SendKeepAlive() { }
    }
}
