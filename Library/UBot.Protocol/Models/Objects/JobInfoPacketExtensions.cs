using UBot.Core.Network;
using UBot.Protocol.Legacy;
using UBot.Core;

namespace UBot.Core.Objects;

public static class JobInfoPacketExtensions
{
    public static JobInfo ReadJobInfo(this Packet packet)
    {
        if (LegacyGame.ClientType <= GameClientType.Vietnam)
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
