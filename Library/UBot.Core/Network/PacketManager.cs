using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace UBot.Core.Network;

public class PacketManager
{
    /// <summary>
    ///     <inheritdoc />
    /// </summary>
    private static readonly object _lock = new();
    private static readonly object _handlersLock = new();
    private static readonly object _hooksLock = new();
    private static readonly Dictionary<PacketRouteKey, IPacketHandler[]> _handlerLookupCache = new();
    private static readonly Dictionary<PacketRouteKey, IPacketHook[]> _hookLookupCache = new();

    /// <summary>
    ///     Gets the handlers.
    /// </summary>
    /// <value>
    ///     The handlers.
    /// </value>
    internal static List<IPacketHandler> Handlers = new();

    /// <summary>
    ///     Gets the hooks.
    /// </summary>
    internal static List<IPacketHook> Hooks = new();

    /// <summary>
    ///     The callbacks
    /// </summary>
    private static readonly List<AwaitCallback> _callbacks = new();

    internal static int PendingCallbackCount
    {
        get
        {
            lock (_lock)
                return _callbacks.Count;
        }
    }

    internal static void RemoveCallback(AwaitCallback callback)
    {
        if (callback == null)
            return;

        lock (_lock)
        {
            _callbacks.Remove(callback);
        }
    }

    /// <summary>
    ///     Registers the handler.
    /// </summary>
    /// <param name="handler">The handler.</param>
    public static void RegisterHandler(IPacketHandler handler)
    {
        if (handler == null)
            return;

        lock (_handlersLock)
        {
            if (Handlers.Contains(handler))
                return;

            Handlers.Add(handler);
            _handlerLookupCache.Clear();
        }
    }

    /// <summary>
    ///     Removes the handler.
    /// </summary>
    /// <param name="handler">The handler.</param>
    public static void RemoveHandler(IPacketHandler handler)
    {
        if (handler == null)
            return;

        lock (_handlersLock)
        {
            Handlers.Remove(handler);
            _handlerLookupCache.Clear();
        }
    }

    /// <summary>
    ///     Registers the hook.
    /// </summary>
    /// <param name="hook">The hook.</param>
    public static void RegisterHook(IPacketHook hook)
    {
        if (hook == null)
            return;

        lock (_hooksLock)
        {
            if (Hooks.Contains(hook))
                return;

            Hooks.Add(hook);
            _hookLookupCache.Clear();
        }
    }

    /// <summary>
    ///     Removes the hook.
    /// </summary>
    /// <param name="hook">The hook.</param>
    public static void RemoveHook(IPacketHook hook)
    {
        if (hook == null)
            return;

        lock (_hooksLock)
        {
            Hooks.Remove(hook);
            _hookLookupCache.Clear();
        }
    }

    /// <summary>
    ///     Calls the specified packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    /// <param name="destination">The destination.</param>
    internal static void CallHandler(Packet packet, PacketDestination destination)
    {
        if (packet == null)
            return;

        IPacketHandler[] handlers;
        var key = new PacketRouteKey(packet.Opcode, destination);

        lock (_handlersLock)
        {
            if (Handlers.Count == 0)
                return;

            if (!_handlerLookupCache.TryGetValue(key, out handlers))
            {
                var count = Handlers.Count;
                var matches = new List<IPacketHandler>(count);
                var opcode = packet.Opcode;

                for (var i = 0; i < count; i++)
                {
                    var handler = Handlers[i];
                    if (handler != null && (handler.Opcode == opcode || handler.Opcode == 0) && handler.Destination == destination)
                        matches.Add(handler);
                }

                handlers = matches.ToArray();
                _handlerLookupCache[key] = handlers;
            }
        }

        var handlerCount = handlers.Length;
        for (var i = 0; i < handlerCount; i++)
        {
            handlers[i].Invoke(packet);
            packet.SeekRead(0, SeekOrigin.Begin);
        }
    }

    public static void HandlePacket(Packet packet, PacketDestination destination)
    {
        CallHandler(packet, destination);
    }

    /// <summary>
    ///     Calls the registered hooks and returns a replaced packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    /// <param name="destination">The destination.</param>
    /// <returns></returns>
    internal static Packet CallHook(Packet packet, PacketDestination destination)
    {
        if (packet == null)
            return null;

        IPacketHook[] hooks;
        var key = new PacketRouteKey(packet.Opcode, destination);

        lock (_hooksLock)
        {
            if (!_hookLookupCache.TryGetValue(key, out hooks))
            {
                var count = Hooks.Count;
                var matches = new List<IPacketHook>(count);
                var opcode = packet.Opcode;

                for (var i = 0; i < count; i++)
                {
                    var hook = Hooks[i];
                    if (hook != null && (hook.Opcode == opcode || hook.Opcode == 0) && hook.Destination == destination)
                        matches.Add(hook);
                }

                hooks = matches.ToArray();
                _hookLookupCache[key] = hooks;
            }
        }

        var hookCount = hooks.Length;
        for (var i = 0; i < hookCount; i++)
        {
            if (packet == null)
                break;

            packet = hooks[i].ReplacePacket(packet);
        }

        return packet;
    }

    /// <summary>
    ///     Calls the callback.
    /// </summary>
    /// <param name="packet">The packet.</param>
    internal static void CallCallback(Packet packet)
    {
        if (packet == null)
            return;

        ushort opcode = packet.Opcode;
        AwaitCallback[] toInvoke;

        lock (_lock)
        {
            _callbacks.RemoveAll(c => c == null || c.IsClosed);
            toInvoke = _callbacks.Where(c => c.ResponseOpcode == opcode).ToArray();
        }

        foreach (var callback in toInvoke)
        {
            try
            {
                packet.SeekRead(0, SeekOrigin.Begin);
                callback.Invoke(packet);
            }
            catch (Exception e)
            {
                Log.Error($"UBot.Core.RuntimeAccess.Packets.CallCallback: opcode=0x{opcode:X4}, callback={callback.GetType().Name} threw: {e.Message}");
            }
        }

        lock (_lock)
        {
            _callbacks.RemoveAll(c => c == null || c.IsClosed);
        }

        UBot.Protocol.ProtocolRuntime.CallCallback(packet);
    }

    /// <summary>
    ///     Sends the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    /// <param name="destination">The destination.</param>
    public static void SendPacket(Packet packet, PacketDestination destination)
    {
        if (UBot.Core.RuntimeAccess.Core.Proxy == null)
            return;

        if (!packet.Locked)
            packet.Lock();

        try
        {
            switch (destination)
            {
                case PacketDestination.Client:
                    if (!UBot.Core.RuntimeAccess.Session.Clientless)
                        UBot.Core.RuntimeAccess.Core.Proxy.Client?.Send(packet);
                    break;

                case PacketDestination.Server:
                    UBot.Core.RuntimeAccess.Core.Proxy.Server?.Send(packet);
                    break;
            }
        }
        catch (Exception e)
        {
            Log.Fatal(e);
        }
    }

    /// <summary>
    ///     Sends the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    /// <param name="destination">The destination.</param>
    /// <param name="callback">The callback.</param>
    public static void SendPacket(Packet packet, PacketDestination destination, params AwaitCallback[] callbacks)
    {
        if (UBot.Core.RuntimeAccess.Core.Proxy == null)
            return;

        if (callbacks != null && callbacks.Length > 0)
        {
            lock (_lock)
            {
                _callbacks.RemoveAll(c => c == null || c.IsClosed);

                foreach (var callback in callbacks)
                {
                    if (callback != null && !callback.IsClosed && !_callbacks.Contains(callback))
                        _callbacks.Add(callback);
                }
            }
        }

        SendPacket(packet, destination);
    }

    /// <summary>
    ///     Gets the handlers by the specified opcode. If none specified, all handlers will be returned.
    /// </summary>
    /// <param name="opcode">The opcode.</param>
    /// <returns></returns>
    public static List<IPacketHandler> GetHandlers(ushort? opcode = null)
    {
        lock (_handlersLock)
            return opcode == null
                ? Handlers.ToList()
                : Handlers.Where(h => h.Opcode == opcode).ToList();
    }

    /// <summary>
    ///     Gets the hooks by the specified opcode. If none specified, all hooks will be returned.
    /// </summary>
    /// <param name="opcode">The opcode.</param>
    /// <returns></returns>
    public static List<IPacketHook> GetHooks(ushort? opcode = null)
    {
        lock (_hooksLock)
            return opcode == null ? Hooks.ToList() : Hooks.Where(h => h.Opcode == opcode).ToList();
    }

    private readonly struct PacketRouteKey : IEquatable<PacketRouteKey>
    {
        public PacketRouteKey(ushort opcode, PacketDestination destination)
        {
            Opcode = opcode;
            Destination = destination;
        }

        private ushort Opcode { get; }
        private PacketDestination Destination { get; }

        public bool Equals(PacketRouteKey other) => Opcode == other.Opcode && Destination == other.Destination;

        public override bool Equals(object obj) => obj is PacketRouteKey other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Opcode, Destination);
    }
}
