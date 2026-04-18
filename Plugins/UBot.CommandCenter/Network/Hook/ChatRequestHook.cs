using UBot.CommandCenter.Components;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Extensions;
using UBot.Core.Network;
using UBot.Core.Objects;

namespace UBot.CommandCenter.Network.Hook;

internal class ChatRequestHook : IPacketHook
{
    public ushort Opcode => 0x7025;

    public PacketDestination Destination => PacketDestination.Server;

    public Packet ReplacePacket(Packet packet)
    {
        if (!PluginConfig.Enabled)
            return packet;

        var type = (ChatType)packet.ReadByte();
        if (type == ChatType.Private)
            return packet;

        packet.ReadByte(); // chatIndex

        if (Game.ClientType > GameClientType.Vietnam)
            packet.ReadByte(); // has linking

        if (Game.ClientType >= GameClientType.Chinese_Old)
            packet.ReadByte();

        var message = packet.ReadConditonalString();
        if (!message.StartsWith("\\"))
            return packet;

        var commandName = message.Split('\\')[1];

        CommandManager.Execute(commandName);

        return null;
    }
}
