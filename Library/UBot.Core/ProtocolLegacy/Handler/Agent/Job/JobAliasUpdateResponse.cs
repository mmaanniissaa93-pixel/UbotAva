using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Event;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Job;

internal class JobAliasUpdateResponse 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0xB0E3;

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
        var result = packet.ReadByte();

        if (result != 1)
            return;

        packet.ReadByte(); //IsUpdate
        CoreGame.Player.JobInformation.Name = packet.ReadString();

        Log.Notify($"[Job] New job alias assigned: {CoreGame.Player.JobInformation.Name}");

        EventManager.FireEvent("OnJobAliasUpdate");
    }
}





