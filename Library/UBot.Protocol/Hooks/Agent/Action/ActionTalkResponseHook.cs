using CoreKernel = UBot.Protocol.Legacy.LegacyKernel;
using UBot.Core.Network;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Hooks.Agent.Action;

public class ActionTalkResponseHook : IPacketHook 
{
    /// <summary>
    ///     Gets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0xB046;

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
        //if (CoreKernel.Bot.Running || ShoppingManager.Running)
        //    return null;

        return packet;
    }
}





