using System.Collections.Generic;
using UBot.Core.Network;
using UBot.Core.Objects.Skill;

namespace UBot.Core.Objects;

internal static class SkillsPacketExtensions
{
    internal static Skills ReadSkills(this Packet packet)
    {
        var result = new Skills { KnownSkills = new List<SkillInfo>(), Masteries = new List<MasteryInfo>() };

        packet.ReadByte();

        while (packet.ReadByte() == 0x01)
            result.Masteries.Add(packet.ReadMasteryInfo());

        packet.ReadByte();

        while (packet.ReadByte() == 0x01)
            result.KnownSkills.Add(packet.ReadSkillInfo());

        return result;
    }
}
