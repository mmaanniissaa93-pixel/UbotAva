using System;
using System.Reflection;
using UBot.Core.Event;
using UBot.Core.Network;
using Xunit;

namespace UBot.Core.Tests;

public class ProxyTests
{
    [Fact]
    public void GatewayConnected_ShouldSetGatewayStateAndFireBothGatewayEvents()
    {
        var proxy = new Proxy();
        var eventCounts = new GatewayEventCounts();
        Action connected = eventCounts.OnConnected;
        Action legacyConnected = eventCounts.OnLegacyConnected;

        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnGatewayServerConnected", connected);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnGatewayServerConntected", legacyConnected);

        try
        {
            SetPendingTarget(proxy, "Gateway");
            InvokeServerConnected(proxy);

            Assert.True(proxy.IsConnectedToGatewayserver);
            Assert.False(proxy.IsConnectedToAgentserver);
            Assert.Equal(1, eventCounts.Connected);
            Assert.Equal(1, eventCounts.LegacyConnected);
        }
        finally
        {
            UBot.Core.RuntimeAccess.Events.UnsubscribeEvent("OnGatewayServerConnected", connected);
            UBot.Core.RuntimeAccess.Events.UnsubscribeEvent("OnGatewayServerConntected", legacyConnected);
        }
    }

    [Fact]
    public void AgentConnected_ShouldSetAgentStateAndNotFireGatewayEvents()
    {
        var proxy = new Proxy();
        var eventCounts = new GatewayEventCounts();
        Action connected = eventCounts.OnConnected;
        Action legacyConnected = eventCounts.OnLegacyConnected;

        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnGatewayServerConnected", connected);
        UBot.Core.RuntimeAccess.Events.SubscribeEvent("OnGatewayServerConntected", legacyConnected);

        try
        {
            SetPendingTarget(proxy, "Agent");
            InvokeServerConnected(proxy);

            Assert.False(proxy.IsConnectedToGatewayserver);
            Assert.True(proxy.IsConnectedToAgentserver);
            Assert.Equal(0, eventCounts.Connected);
            Assert.Equal(0, eventCounts.LegacyConnected);
        }
        finally
        {
            UBot.Core.RuntimeAccess.Events.UnsubscribeEvent("OnGatewayServerConnected", connected);
            UBot.Core.RuntimeAccess.Events.UnsubscribeEvent("OnGatewayServerConntected", legacyConnected);
        }
    }

    [Fact]
    public void Shutdown_ShouldResetConnectionState()
    {
        var proxy = new Proxy();

        SetPendingTarget(proxy, "Gateway");
        InvokeServerConnected(proxy);
        Assert.True(proxy.IsConnectedToGatewayserver);

        proxy.Shutdown();

        Assert.False(proxy.ClientConnected);
        Assert.False(proxy.IsConnectedToGatewayserver);
        Assert.False(proxy.IsConnectedToAgentserver);
    }

    private static void SetPendingTarget(Proxy proxy, string targetName)
    {
        var targetType = typeof(Proxy).GetNestedType("ServerConnectionTarget", BindingFlags.NonPublic);
        var field = typeof(Proxy).GetField("_pendingServerConnectionTarget", BindingFlags.NonPublic | BindingFlags.Instance);
        field.SetValue(proxy, Enum.Parse(targetType, targetName));
    }

    private static void InvokeServerConnected(Proxy proxy)
    {
        typeof(Proxy)
            .GetMethod("Server_OnConnected", BindingFlags.NonPublic | BindingFlags.Instance)
            .Invoke(proxy, null);
    }

    private sealed class GatewayEventCounts
    {
        public int Connected { get; private set; }
        public int LegacyConnected { get; private set; }

        public void OnConnected()
        {
            Connected++;
        }

        public void OnLegacyConnected()
        {
            LegacyConnected++;
        }
    }
}
