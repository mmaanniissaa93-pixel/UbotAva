using CoreKernel = UBot.Protocol.Legacy.LegacyKernel;
using UBot.Core.Network;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Logout;

public class LogoutSuccessResponse : IPacketHandler 
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





