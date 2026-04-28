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

            PacketManager.RegisterHandler(handler);
            PacketManager.RegisterHandler(handler);

            var handlers = PacketManager.GetHandlers();
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

            PacketManager.RegisterHook(hook);
            PacketManager.RegisterHook(hook);

            var hooks = PacketManager.GetHooks();
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

            PacketManager.RegisterHook(dropHook);
            PacketManager.RegisterHook(nextHook);

            var result = PacketManager.CallHook(new Packet(0x1234), PacketDestination.Client);

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
        var previousProxy = Kernel.Proxy;
        var callback = new AwaitCallback(null, 0xBEEF);
        var baselineCount = PacketManager.PendingCallbackCount;

        try
        {
            Kernel.Proxy = new Proxy();

            PacketManager.SendPacket(new Packet(0x1234), PacketDestination.Server, callback);
            Assert.Equal(baselineCount + 1, PacketManager.PendingCallbackCount);

            callback.AwaitResponse(1);

            Assert.True(callback.IsClosed);
            Assert.Equal(baselineCount, PacketManager.PendingCallbackCount);
        }
        finally
        {
            PacketManager.RemoveCallback(callback);
            Kernel.Proxy = previousProxy;
        }
    }

    [Fact]
    public void CallCallback_ShouldRemoveCompletedCallback()
    {
        var previousProxy = Kernel.Proxy;
        var callback = new AwaitCallback(null, 0xBEEF);
        var baselineCount = PacketManager.PendingCallbackCount;

        try
        {
            Kernel.Proxy = new Proxy();

            PacketManager.SendPacket(new Packet(0x1234), PacketDestination.Server, callback);
            Assert.Equal(baselineCount + 1, PacketManager.PendingCallbackCount);

            var response = new Packet(0xBEEF);
            response.Lock();

            PacketManager.CallCallback(response);

            Assert.True(callback.IsCompleted);
            Assert.Equal(baselineCount, PacketManager.PendingCallbackCount);
        }
        finally
        {
            PacketManager.RemoveCallback(callback);
            Kernel.Proxy = previousProxy;
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
            var handlers = PacketManager.GetHandlers();
            var hooks = PacketManager.GetHooks();

            foreach (var handler in handlers)
                PacketManager.RemoveHandler(handler);

            foreach (var hook in hooks)
                PacketManager.RemoveHook(hook);

            return new PacketManagerState(handlers, hooks);
        }

        public void Restore()
        {
            foreach (var handler in PacketManager.GetHandlers())
                PacketManager.RemoveHandler(handler);

            foreach (var hook in PacketManager.GetHooks())
                PacketManager.RemoveHook(hook);

            foreach (var handler in _handlers)
                PacketManager.RegisterHandler(handler);

            foreach (var hook in _hooks)
                PacketManager.RegisterHook(hook);
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
