using UBot.Core.Network;

namespace UBot.Protocol.Commands.Agent;

public static class ChatCommands
{
    public static Packet BuildChatMessage(byte type, string receiver, string message)
    {
        var packet = new Packet(0x7025);
        packet.WriteByte(type);
        packet.WriteString(receiver ?? string.Empty);
        packet.WriteString(message ?? string.Empty);
        return packet;
    }
}
