using System.Threading.Tasks;

namespace UBot.Core.Abstractions.Services;

public interface ISkillService
{
    uint LastCastedSkillId { get; set; }
    bool UseSkillsInOrder { get; }
    bool IsLastCastedBasic { get; }

    void Initialize();
    void ResetBaseSkills();
    void NotifySkillCasted(uint skillId);
    void SetSkills(object monsterRarity, object skills);
    object GetNextSkill();
    bool CheckSkillRequired(object skillRecord);

    bool CastSkill(object skill, uint targetId = 0);
    Task<bool> CastSkillAsync(object skill, uint targetId = 0);
    bool CastSkillOld(object skill, uint targetId = 0);
    Task<bool> CastSkillOldAsync(object skill, uint targetId = 0);
    void CastBuff(object skill, uint target = 0, bool awaitBuffResponse = true);
    Task CastBuffAsync(object skill, uint target = 0, bool awaitBuffResponse = true);
    void CastSkillAt(object skill, object target);
    Task CastSkillAtAsync(object skill, object target);
    bool CastAutoAttack();
    Task<bool> CastAutoAttackAsync();
    void CastSkillAt(uint skillId, object position);
    void CancelBuff(uint skillId);
    bool CancelAction();
    Task<bool> CancelActionAsync();
}
