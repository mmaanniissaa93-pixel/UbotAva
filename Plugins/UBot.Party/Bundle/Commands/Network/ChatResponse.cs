using System;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Extensions;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Objects.Spawn;
using UBot.Party.Bundle;

namespace UBot.Chat.Network;

internal class ChatResponse : IPacketHandler
{
    public ushort Opcode => 0x3026;

    public PacketDestination Destination => PacketDestination.Client;

    public void Invoke(Packet packet)
    {
        var type = (ChatType)packet.ReadByte();

        string senderName;
        SpawnedPlayer sender = null;
        string message;

        switch (type)
        {
            case ChatType.All:
            case ChatType.AllGM:
                var senderId = packet.ReadUInt();
                message = packet.ReadConditonalString();

                if (senderId == UBot.Core.RuntimeAccess.Session.Player.UniqueId)
                    return;

                if (!SpawnManager.TryGetEntity(senderId, out sender))
                    return;

                senderName = sender.Name;
                break;

            case ChatType.Private:
            case ChatType.Party:
            case ChatType.Guild:
            case ChatType.Union:
                senderName = packet.ReadString();
                message = packet.ReadConditonalString();

                if (type == ChatType.Union && message.Contains(": ", StringComparison.InvariantCulture))
                {
                    var split = message.Split(new[] { ": " }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (split.Length == 2)
                        message = split[1];
                }

                SpawnManager.TryGetEntity(
                    p => p.Name.Equals(senderName, StringComparison.InvariantCultureIgnoreCase),
                    out sender
                );
                break;

            default:
                return;
        }

        if (string.IsNullOrWhiteSpace(senderName) || string.IsNullOrWhiteSpace(message))
            return;

        Container.Commands.Handle(senderName, sender, type, message.Trim());
    }
}
