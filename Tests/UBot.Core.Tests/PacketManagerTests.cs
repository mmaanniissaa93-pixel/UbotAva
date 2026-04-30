using System.Collections.Generic;
using UBot.Core;
using UBot.Core.Network;
using Xunit;

namespace UBot.Core.Tests;

public class PacketManagerTests
{
    [Fact]
    public void RegisterHandler_ShouldIgnoreSameInstanceDuplicate()
    {
        var state = PacketManagerState.CaptureAndClear();

        try
        {
            var handler = new CountingHandler(0x1234, PacketDestination.Client);

            UBot.Core.RuntimeAccess.Packets.RegisterHandler(handler);
            UBot.Core.RuntimeAccess.Packets.RegisterHandler(handler);

            var handlers = UBot.Core.RuntimeAccess.Packets.GetHandlers();
            Assert.Single(handlers);
            Assert.Same(handler, handlers[0]);
        }
        finally
        {
            state.Restore();
        }
    }

    [Fact]
    public void RegisterHook_ShouldIgnoreSameInstanceDuplicate()
    {
        var state = PacketManagerState.CaptureAndClear();

        try
        {
            var hook = new CountingHook(0x1234, PacketDestination.Client);

            UBot.Core.RuntimeAccess.Packets.RegisterHook(hook);
            UBot.Core.RuntimeAccess.Packets.RegisterHook(hook);

            var hooks = UBot.Core.RuntimeAccess.Packets.GetHooks();
            Assert.Single(hooks);
            Assert.Same(hook, hooks[0]);
        }
        finally
        {
            state.Restore();
        }
    }

    [Fact]
    public void CallHook_ShouldStopPipeline_WhenHookDropsPacket()
    {
        var state = PacketManagerState.CaptureAndClear();

        try
        {
            var dropHook = new DroppingHook(0x1234, PacketDestination.Client);
            var nextHook = new CountingHook(0x1234, PacketDestination.Client);

            UBot.Core.RuntimeAccess.Packets.RegisterHook(dropHook);
            UBot.Core.RuntimeAccess.Packets.RegisterHook(nextHook);

            var result = UBot.Core.RuntimeAccess.Packets.CallHook(new Packet(0x1234), PacketDestination.Client);

            Assert.Null(result);
            Assert.Equal(1, dropHook.InvocationCount);
            Assert.Equal(0, nextHook.InvocationCount);
        }
        finally
        {
            state.Restore();
        }
    }

    [Fact]
    public void AwaitResponse_ShouldRemoveTimedOutCallback()
    {
        var previousProxy = UBot.Core.RuntimeAccess.Core.Proxy;
        var callback = new AwaitCallback(null, 0xBEEF);
        var baselineCount = UBot.Core.RuntimeAccess.Packets.PendingCallbackCount;

        try
        {
            UBot.Core.RuntimeAccess.Core.Proxy = new Proxy();

            UBot.Core.RuntimeAccess.Packets.SendPacket(new Packet(0x1234), PacketDestination.Server, callback);
            Assert.Equal(baselineCount + 1, UBot.Core.RuntimeAccess.Packets.PendingCallbackCount);

            callback.AwaitResponse(1);

            Assert.True(callback.IsClosed);
            Assert.Equal(baselineCount, UBot.Core.RuntimeAccess.Packets.PendingCallbackCount);
        }
        finally
        {
            UBot.Core.RuntimeAccess.Packets.RemoveCallback(callback);
            UBot.Core.RuntimeAccess.Core.Proxy = previousProxy;
        }
    }

    [Fact]
    public void CallCallback_ShouldRemoveCompletedCallback()
    {
        var previousProxy = UBot.Core.RuntimeAccess.Core.Proxy;
        var callback = new AwaitCallback(null, 0xBEEF);
        var baselineCount = UBot.Core.RuntimeAccess.Packets.PendingCallbackCount;

        try
        {
            UBot.Core.RuntimeAccess.Core.Proxy = new Proxy();

            UBot.Core.RuntimeAccess.Packets.SendPacket(new Packet(0x1234), PacketDestination.Server, callback);
            Assert.Equal(baselineCount + 1, UBot.Core.RuntimeAccess.Packets.PendingCallbackCount);

            var response = new Packet(0xBEEF);
            response.Lock();

            UBot.Core.RuntimeAccess.Packets.CallCallback(response);

            Assert.True(callback.IsCompleted);
            Assert.Equal(baselineCount, UBot.Core.RuntimeAccess.Packets.PendingCallbackCount);
        }
        finally
        {
            UBot.Core.RuntimeAccess.Packets.RemoveCallback(callback);
            UBot.Core.RuntimeAccess.Core.Proxy = previousProxy;
        }
    }

    private sealed class PacketManagerState
    {
        private readonly List<IPacketHandler> _handlers;
        private readonly List<IPacketHook> _hooks;

        private PacketManagerState(List<IPacketHandler> handlers, List<IPacketHook> hooks)
        {
            _handlers = handlers;
            _hooks = hooks;
        }

        public static PacketManagerState CaptureAndClear()
        {
            var handlers = UBot.Core.RuntimeAccess.Packets.GetHandlers();
            var hooks = UBot.Core.RuntimeAccess.Packets.GetHooks();

            foreach (var handler in handlers)
                UBot.Core.RuntimeAccess.Packets.RemoveHandler(handler);

            foreach (var hook in hooks)
                UBot.Core.RuntimeAccess.Packets.RemoveHook(hook);

            return new PacketManagerState(handlers, hooks);
        }

        public void Restore()
        {
            foreach (var handler in UBot.Core.RuntimeAccess.Packets.GetHandlers())
                UBot.Core.RuntimeAccess.Packets.RemoveHandler(handler);

            foreach (var hook in UBot.Core.RuntimeAccess.Packets.GetHooks())
                UBot.Core.RuntimeAccess.Packets.RemoveHook(hook);

            foreach (var handler in _handlers)
                UBot.Core.RuntimeAccess.Packets.RegisterHandler(handler);

            foreach (var hook in _hooks)
                UBot.Core.RuntimeAccess.Packets.RegisterHook(hook);
        }
    }

    private sealed class CountingHandler : IPacketHandler
    {
        public CountingHandler(ushort opcode, PacketDestination destination)
        {
            Opcode = opcode;
            Destination = destination;
        }

        public ushort Opcode { get; }
        public PacketDestination Destination { get; }
        public int InvocationCount { get; private set; }

        public void Invoke(Packet packet)
        {
            InvocationCount++;
        }
    }

    private sealed class CountingHook : IPacketHook
    {
        public CountingHook(ushort opcode, PacketDestination destination)
        {
            Opcode = opcode;
            Destination = destination;
        }

        public ushort Opcode { get; }
        public PacketDestination Destination { get; }
        public int InvocationCount { get; private set; }

        public Packet ReplacePacket(Packet packet)
        {
            InvocationCount++;
            return packet;
        }
    }

    private sealed class DroppingHook : IPacketHook
    {
        public DroppingHook(ushort opcode, PacketDestination destination)
        {
            Opcode = opcode;
            Destination = destination;
        }

        public ushort Opcode { get; }
        public PacketDestination Destination { get; }
        public int InvocationCount { get; private set; }

        public Packet ReplacePacket(Packet packet)
        {
            InvocationCount++;
            return null;
        }
    }
}
