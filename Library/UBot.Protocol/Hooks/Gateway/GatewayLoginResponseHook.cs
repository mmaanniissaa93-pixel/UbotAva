using CoreKernel = UBot.Protocol.Legacy.LegacyKernel;
using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using Game = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Abstractions;
using UBot.Core.Network;
using System.Collections.Generic;
using UBot.Protocol.Legacy;
using UBot.Core;

namespace UBot.Protocol.Hooks.Gateway;

public class GatewayLoginResponseHook : IPacketHook 
{
    /// <summary>
    ///     Gets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0xA102;

    /// <summary>
    ///     Gets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Client;

    /// <summary>
    ///     Replaces the packet and returns a new packet.
    /// </summary>
    /// <param name="packet"></param>
    /// <returns></returns>
    public Packet ReplacePacket(Packet packet)
    {
        var result = packet.ReadByte();
        if (result == 0x01)
        {
            CoreKernel.Proxy.SetToken(packet.ReadUInt());
            if (CoreGame.ClientType == GameClientType.RuSro)
            {
                Dictionary<string, string> localPublicIP = new()
                {
                    { "10.96.4.66", "109.105.146.10" },
                    { "10.96.4.67", "109.105.146.11" },
                };

                CoreKernel.Proxy.SetAgentserverAddress(localPublicIP[packet.ReadString()], packet.ReadUShort());
            }
            else
                CoreKernel.Proxy.SetAgentserverAddress(packet.ReadString(), packet.ReadUShort());

            if (CoreGame.Clientless)
                return null;
        }
        else
        {
            return packet;
        }

        var resultPacket = new Packet(packet.Opcode, packet.Encrypted, packet.Massive);
        resultPacket.WriteByte(result);
        resultPacket.WriteUInt(CoreKernel.Proxy.Token);
        resultPacket.WriteString("127.0.0.1");
        resultPacket.WriteUShort(CoreKernel.Proxy.Port);

        if (packet.Remaining > 0)
            resultPacket.WriteBytes(packet.ReadBytes(packet.Remaining));
        /*
        //unknown value
        if (CoreGame.ClientType == GameClientType.Japanese_Old)
            resultPacket.WriteInt(packet.ReadInt());

        if (CoreGame.ClientType >= GameClientType.Chinese)
        {
            var unk1 = packet.ReadByte();
            resultPacket.WriteByte(unk1);

            if (unk1 == 2 && packet.ReaderRemain > 0)
                resultPacket.WriteString(packet.ReadString());
        }
        */
        return resultPacket;
    }
}





