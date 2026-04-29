using CoreKernel = global::UBot.Core.Kernel;
using UBot.Core.Network;
using UBot.Core.Event;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Logout;

internal class LogoutSuccessResponse 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x300A;

    /// <summary>
    ///     Gets or sets the destination.
    /// </summary>
    /// <value>
    ///     The destination.
    /// </value>
    public PacketDestination Destination => PacketDestination.Client;

    /// <summary>
    ///     Handles the packet.
    /// </summary>
    /// <param name="packet">The packet.</param>
    public void Invoke(Packet packet)
    {
        Log.Notify("The player has left the game!");
        CoreKernel.Proxy?.Shutdown(); //Forced disconnect because LogoutMode of 0x7005 is not yet supported.
        EventManager.FireEvent("OnLogout");
    }
}





