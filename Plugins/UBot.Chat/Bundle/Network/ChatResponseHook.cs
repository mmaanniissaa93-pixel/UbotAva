using UBot.Core;
using UBot.Core.Network;

namespace UBot.Chat.Network;

internal class ChatResponseHook : IPacketHook
{
    public ushort Opcode => 0xB025;

    public PacketDestination Destination => PacketDestination.Client;

    public Packet ReplacePacket(Packet packet)
    {
        if (!Game.Clientless && Bundle.Chat.IgnoreChatResponsePacket)
        {
            Bundle.Chat.IgnoreChatResponsePacket = false;
            return null;
        }

        return packet;
    }
}
