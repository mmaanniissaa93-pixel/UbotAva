#nullable enable annotations

using System.Collections.Generic;
using System.Linq;
using UBot.Core.Network;
using UBot.Core.Objects.Skill;

namespace UBot.Core.Objects;

public class Skills
{
    /// <summary>
    ///     Gets or sets the masteries.
    /// </summary>
    /// <value>
    ///     The masteries.
    /// </value>
    public List<MasteryInfo> Masteries { get; set; }

    /// <summary>
    ///     Gets or sets the learned skills.
    /// </summary>
    /// <value>
    ///     The learned skills.
    /// </value>
    public List<SkillInfo> KnownSkills { get; set; }

    /// <summary>
    ///     Gets or sets the pending withdraw skill.
    /// </summary>
    /// <value>
    ///     The pending withdraw skill.
    /// </value>
    internal uint PendingWithdrawSkill { get; set; }

    /// <summary>
    ///     Creates a new Skill object from the given packet
    /// </summary>
    /// <param name="packet">The packet.</param>
    /// <returns></returns>
    internal static Skills FromPacket(Packet packet)
    {
        var result = new Skills { KnownSkills = new List<SkillInfo>(), Masteries = new List<MasteryInfo>() };

        packet.ReadByte(); //unknown

        while (packet.ReadByte() == 0x01)
            result.Masteries.Add(packet.ReadMasteryInfo());

        packet.ReadByte(); //unknown

        while (packet.ReadByte() == 0x01)
            result.KnownSkills.Add(packet.ReadSkillInfo());

        return result;
    }

    /// <summary>
    ///     Gets the name of the skill by.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public SkillInfo GetSkillByName(string name)
    {
        return KnownSkills.Find(s => s.Record?.GetRealName() == name);
    }

    public SkillInfo GetSkillByCodeName(string codeName)
    {
        return KnownSkills.FirstOrDefault(s => s.Record?.Basic_Code == codeName);
    }

    /// <summary>
    ///     Gets the name of the skill by.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <returns></returns>
    public SkillInfo? GetSkillRecordByName(string name)
    {
        if (KnownSkills == null || name == null)
            return null;

        return KnownSkills.Find(s => s.Record.GetRealName() == name);
    }

    /// <summary>
    ///     Gets the skill information by identifier.
    /// </summary>
    /// <param name="skillId">The skill identifier.</param>
    /// <returns></returns>
    public SkillInfo? GetSkillInfoById(uint skillId)
    {
        var exact = KnownSkills.Find(s => s.Id == skillId);
        if (exact != null) return exact;

        // Auto-upgrade redirection: find the best known skill in the same group
        var refSkill = Game.ReferenceManager?.GetRefSkill(skillId);
        if (refSkill == null || refSkill.GroupID == 0)
            return null;

        return KnownSkills
            .Where(s => s.Record != null && s.Record.GroupID == refSkill.GroupID && s.Record.Basic_Group == refSkill.Basic_Group)
            .OrderByDescending(s => s.Record.Basic_Level)
            .FirstOrDefault();
    }

    /// <summary>
    ///     Gets the skill information by the group identifier.
    /// </summary>
    /// <param name="skillGroupId">The skill group identifier.</param>
    /// <returns></returns>
    public SkillInfo? GetSkillInfoByGroupId(int skillGroupId)
    {
        return KnownSkills.FirstOrDefault(s => s.Record.GroupID == skillGroupId);
    }

    /// <summary>
    ///     Gets the mastery information by identifier.
    /// </summary>
    /// <param name="masteryId">The mastery identifier.</param>
    /// <returns></returns>
    public MasteryInfo? GetMasteryInfoById(uint masteryId)
    {
        return Masteries.Find(m => m.Id == masteryId);
    }

    /// <summary>
    ///     Updates the mastery level.
    /// </summary>
    /// <param name="masteryId">The mastery identifier.</param>
    /// <param name="level">The level.</param>
    internal void UpdateMasteryLevel(uint masteryId, byte level)
    {
        var mastery = Masteries.FirstOrDefault(m => m.Id == masteryId);
        if (mastery != null)
            mastery.Level = level;
    }

    /// <summary>
    ///     Determines whether the specified skill exists.
    /// </summary>
    /// <param name="skillId">The skill identifier.</param>
    /// <returns></returns>
    public bool HasSkill(uint skillId)
    {
        return GetSkillInfoById(skillId) != null;
    }

    /// <summary>
    ///     Removes the skill by identifier.
    /// </summary>
    /// <param name="skillId">The skill identifier.</param>
    public void RemoveSkillById(uint skillId)
    {
        KnownSkills.RemoveAll(p => p.Id == skillId);
    }
}
