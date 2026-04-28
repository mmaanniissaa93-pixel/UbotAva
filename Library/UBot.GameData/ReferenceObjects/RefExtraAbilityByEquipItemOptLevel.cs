using UBot.Core.Client;

namespace UBot.GameData.ReferenceObjects;

public class RefExtraAbilityByEquipItemOptLevel : UBot.Core.Client.IReference, UBot.Core.Abstractions.IReference
{
    private const int SKILL_COUNT = 5;

    public uint ItemId;
    public byte OptLevel;
    public uint[] Skills;

    uint UBot.Core.Abstractions.IReference.ID => ItemId;
    public string CodeName => $"{ItemId}:{OptLevel}";

    public string GetName()
    {
        return CodeName;
    }

    public string GetRealName(bool displayRarity = false)
    {
        return GetName();
    }

    public bool Load(ReferenceParser parser)
    {
        if (!parser.TryParse(0, out int service) || service == 0)
            return false;

        parser.TryParse(1, out ItemId);
        parser.TryParse(2, out OptLevel);

        Skills = new uint[SKILL_COUNT];
        for (var i = 0; i < SKILL_COUNT; i++)
        {
            if (!parser.TryParse(20 + i, out uint skillId))
                return false;

            Skills[i] = skillId;
        }

        return true;
    }
}
