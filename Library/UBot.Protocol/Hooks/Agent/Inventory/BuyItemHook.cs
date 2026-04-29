using UBot.Core.Network;
using UBot.Protocol;

namespace UBot.Protocol.Hooks.Agent.Inventory;

public class BuyItemHook : IPacketHook
{
    public ushort Opcode => 0xB034;

    public PacketDestination Destination => PacketDestination.Client;

    public Packet ReplacePacket(Packet packet)
    {
        if (ProtocolRuntime.LegacyHandler == null)
            return packet;

        return (Packet)ProtocolRuntime.LegacyHandler.ReplacePacket(nameof(BuyItemHook), packet);
    }
}
