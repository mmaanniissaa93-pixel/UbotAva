using CoreGame = global::UBot.Core.Game;
using UBot.Core.Network;
using UBot.Core.Event;
using UBot.Core.Objects;

namespace UBot.Core.ProtocolLegacy.Handler.Agent.Job;

internal class JobUpdateExperienceResponse 
{
    /// <summary>
    ///     Gets or sets the opcode.
    /// </summary>
    /// <value>
    ///     The opcode.
    /// </value>
    public ushort Opcode => 0x30E6;

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
        CoreGame.Player.JobInformation.Type = (JobType)packet.ReadByte();
        CoreGame.Player.JobInformation.Level = packet.ReadByte();
        CoreGame.Player.JobInformation.Experience = packet.ReadUInt();

        EventManager.FireEvent("OnJobExperienceUpdate");
    }
}





