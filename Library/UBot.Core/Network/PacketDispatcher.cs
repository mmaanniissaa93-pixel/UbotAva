using System;
using UBot.Core.Abstractions.Network;

namespace UBot.Core.Network;

public sealed class PacketDispatcher : IPacketDispatcher, IDisposable
{
    public void RegisterHandler(object handler)
    {
        if (handler is IPacketHandler packetHandler)
            PacketManager.RegisterHandler(packetHandler);
    }

    public void RemoveHandler(object handler)
    {
        if (handler is IPacketHandler packetHandler)
            PacketManager.RemoveHandler(packetHandler);
    }

    public void RegisterHook(object hook)
    {
        if (hook is IPacketHook packetHook)
            PacketManager.RegisterHook(packetHook);
    }

    public void RemoveHook(object hook)
    {
        if (hook is IPacketHook packetHook)
            PacketManager.RemoveHook(packetHook);
    }

    public void SendPacket(object packet, PacketDestination destination, params object[] callbacks)
    {
        if (packet is not Packet networkPacket)
            return;

        var typedCallbacks = Array.Empty<AwaitCallback>();
        if (callbacks != null && callbacks.Length > 0)
        {
            var callbackList = new System.Collections.Generic.List<AwaitCallback>(callbacks.Length);
            foreach (var callback in callbacks)
            {
                if (callback is AwaitCallback awaitCallback)
                    callbackList.Add(awaitCallback);
            }

            typedCallbacks = callbackList.Count > 0 ? callbackList.ToArray() : Array.Empty<AwaitCallback>();
        }

        if (typedCallbacks.Length > 0)
            PacketManager.SendPacket(networkPacket, destination, typedCallbacks);
        else
            PacketManager.SendPacket(networkPacket, destination);
    }

    public void HandlePacket(object packet, PacketDestination destination)
    {
        if (packet is Packet networkPacket)
            PacketManager.HandlePacket(networkPacket, destination);
    }

    public void Dispatch(object packet, PacketDestination destination) => SendPacket(packet, destination);

    public void Dispose()
    {
        foreach (var handler in PacketManager.GetHandlers())
            PacketManager.RemoveHandler(handler);

        foreach (var hook in PacketManager.GetHooks())
            PacketManager.RemoveHook(hook);
    }
}
