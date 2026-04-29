using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Job;

internal class JobJoinResponse 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0xB0E1;

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

        CoreGame.Player.JobInformation = new JobInfo
        {
            Type = (JobType)packet.ReadByte(),
            Level = packet.ReadByte(),
            Experience = packet.ReadUInt(),
        };

        Log.Notify($"[Job] Joined job guild {CoreGame.Player.JobInformation.Type}.");

        EventManager.FireEvent("OnJobJoin");
    }
}





