using CoreKernel = global::UBot.Core.Kernel;
using UBot.Core.Network;
using UBot.Core.Components;

namespace UBot.Core.ProtocolLegacy.Hooks.Agent.Action;

internal class ActionSelectRequestHook 
{
    /// <summary>
    ///     Gets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x7045;

    /// <summary>
    ///     Gets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Server;

    /// <summary>
    ///     Replaces the packet and returns a new packet.
    /// </summary>
    /// <param name="packet"></param>
    /// <returns></returns>
    public Packet ReplacePacket(Packet packet)
    {
        if (CoreKernel.Bot.Running || ShoppingManager.Running)
            return null;

        return packet;
    }
}





