using UBot.Core.Client;

namespace UBot.GameData.ReferenceObjects;

public class RefSkillByItemOptLevel : UBot.Core.Client.IReference, UBot.Core.Abstractions.IReference
{
    public int Link;
    public uint SkillId;

    uint UBot.Core.Abstractions.IReference.ID => SkillId;
    public string CodeName => $"{Link}:{SkillId}";

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
        parser.TryParse(0, out Link);
        parser.TryParse(1, out SkillId);

        return true;
    }
}
