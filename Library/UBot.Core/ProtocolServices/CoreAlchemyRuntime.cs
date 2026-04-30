using UBot.Core.Abstractions.Services;
using UBot.Core.Network;

namespace UBot.Core.ProtocolServices;

internal sealed class CoreAlchemyRuntime : IAlchemyRuntime
{
    public object GetInventoryItemAt(byte slot) => UBot.Core.RuntimeAccess.Session.Player?.Inventory?.GetItemAt(slot);

    public void SendToServer(object packet)
    {
        if (packet is Packet networkPacket)
            UBot.Core.RuntimeAccess.Packets.SendPacket(networkPacket, PacketDestination.Server);
    }

    public void FireEvent(string eventName, params object[] args)
    {
        UBot.Core.RuntimeAccess.Events.FireEvent(eventName, args);
    }
}
