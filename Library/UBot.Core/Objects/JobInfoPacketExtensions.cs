using UBot.Core.Network;

namespace UBot.Core.Objects;

internal static class JobInfoPacketExtensions
{
    internal static JobInfo ReadJobInfo(this Packet packet)
    {
        if (Game.ClientType <= GameClientType.Vietnam)
            return new JobInfo
            {
                Name = packet.ReadString(),
                Type = (JobType)packet.ReadByte(),
                Level = packet.ReadByte(),
                Experience = packet.ReadUInt(),
                Contribution = packet.ReadUInt(),
                Reward = packet.ReadUInt(),
            };

        return new JobInfo
        {
            Name = packet.ReadString(),
            Title = packet.ReadByte(),
            Rank = packet.ReadByte(),
            Type = (JobType)packet.ReadByte(),
            Level = packet.ReadByte(),
            Experience = packet.ReadLong(),
        };
    }
}
