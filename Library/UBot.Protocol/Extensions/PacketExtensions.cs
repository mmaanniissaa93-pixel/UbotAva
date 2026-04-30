using UBot.Core;
using UBot.Core.Network;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Extensions;

public static class PacketExtensions
{
    public static string ReadConditonalString(this Packet packet)
    {
        switch (LegacyGame.ClientType)
        {
            case GameClientType.Thailand:
            case GameClientType.Korean:
            case GameClientType.Rigid:
            case GameClientType.RuSro:
            case GameClientType.Chinese:
            case GameClientType.Japanese:
            case GameClientType.Taiwan:
                return packet.ReadUnicode();

            default:
                return packet.ReadString();
        }
    }

    public static void WriteConditonalString(this Packet packet, string str)
    {
        switch (LegacyGame.ClientType)
        {
            case GameClientType.Thailand:
            case GameClientType.Korean:
            case GameClientType.Rigid:
            case GameClientType.RuSro:
            case GameClientType.Chinese:
            case GameClientType.Japanese:
            case GameClientType.Taiwan:
                packet.WriteUnicode(str);
                break;

            default:
                packet.WriteString(str);
                break;
        }
    }
}
