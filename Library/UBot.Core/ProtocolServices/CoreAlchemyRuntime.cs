using UBot.Core.Abstractions.Services;
using UBot.Core.Network;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreAlchemyRuntime : IAlchemyRuntime
{
    public object GetInventoryItemAt(byte slot) => Game.Player?.Inventory?.GetItemAt(slot);

    public void SendToServer(object packet)
    {
        if (packet is Packet networkPacket)
            PacketManager.SendPacket(networkPacket, PacketDestination.Server);
    }

    public void FireEvent(string eventName, params object[] args)
    {
        Event.EventManager.FireEvent(eventName, args);
    }
}
