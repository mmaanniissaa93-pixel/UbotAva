using CoreGame = UBot.Protocol.Legacy.LegacyGame;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Protocol.Legacy;

namespace UBot.Protocol.Handlers.Agent.Job;

public class JobUpdateExperienceResponse : IPacketHandler 
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

        UBot.Protocol.ProtocolRuntime.LegacyRuntime.FireEvent("OnJobExperienceUpdate");
    }
}





