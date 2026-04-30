using System.Collections.Generic;
using System.Linq;
using UBot.Core.Abstractions;
using UBot.Core.Abstractions.Network;
using UBot.Core.Abstractions.Services;

namespace UBot.Protocol;

public static class ProtocolRuntime
{
    private static readonly object CallbackLock = new();
    private static readonly List<Legacy.AwaitCallback> Callbacks = new();

    public static IGameStateRuntimeContext GameState { get; set; }
    public static IPacketDispatcher PacketDispatcher { get; set; }
    public static Legacy.IProtocolLegacyRuntime LegacyRuntime { get; set; }
    public static ISpawnController SpawnController { get; set; }
    public static IScriptEventBus EventBus { get; set; }
    public static IUIFeedbackService Feedback { get; set; }
    public static IShoppingController Shopping { get; set; }
    public static ICosController Cos { get; set; }

    public static void Dispatch(UBot.Core.Network.Packet packet, UBot.Core.Network.PacketDestination destination)
    {
        PacketDispatcher?.Dispatch(packet, destination);
    }

    public static void SendPacket(
        UBot.Core.Network.Packet packet,
        UBot.Core.Network.PacketDestination destination,
        params Legacy.AwaitCallback[] callbacks)
    {
        if (callbacks != null && callbacks.Length > 0)
        {
            lock (CallbackLock)
            {
                Callbacks.RemoveAll(callback => callback == null || callback.IsClosed);
                foreach (var callback in callbacks.Where(callback => callback != null && !callback.IsClosed && !Callbacks.Contains(callback)))
                    Callbacks.Add(callback);
            }
        }

        Dispatch(packet, destination);
    }

    public static void CallCallback(UBot.Core.Network.Packet packet)
    {
        if (packet == null)
            return;

        Legacy.AwaitCallback[] callbacks;
        lock (CallbackLock)
        {
            Callbacks.RemoveAll(callback => callback == null || callback.IsClosed);
            callbacks = Callbacks.Where(callback => callback.ResponseOpcode == packet.Opcode).ToArray();
        }

        foreach (var callback in callbacks)
        {
            packet.SeekRead(0, System.IO.SeekOrigin.Begin);
            callback.Invoke(packet);
        }

        lock (CallbackLock)
        {
            Callbacks.RemoveAll(callback => callback == null || callback.IsClosed);
        }
    }

    internal static void RemoveCallback(Legacy.AwaitCallback callback)
    {
        lock (CallbackLock)
        {
            Callbacks.Remove(callback);
        }
    }
}
