using UBot.Core.Network;
using UBot.Core.Abstractions.Network;

namespace UBot.Core.ProtocolServices;

internal sealed class CorePacketDispatcher : IPacketDispatcher
{
    private readonly PacketDispatcher _dispatcher = new();

    public void RegisterHandler(object handler) => _dispatcher.RegisterHandler(handler);

    public void RemoveHandler(object handler) => _dispatcher.RemoveHandler(handler);

    public void RegisterHook(object hook) => _dispatcher.RegisterHook(hook);

    public void RemoveHook(object hook) => _dispatcher.RemoveHook(hook);

    public void SendPacket(object packet, PacketDestination destination, params object[] callbacks) =>
        _dispatcher.SendPacket(packet, destination, callbacks);

    public void HandlePacket(object packet, PacketDestination destination) =>
        _dispatcher.HandlePacket(packet, destination);

    public void Dispatch(object packet, PacketDestination destination)
    {
        _dispatcher.Dispatch(packet, destination);
    }
}
