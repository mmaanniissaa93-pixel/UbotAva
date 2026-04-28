using System.Collections.Generic;
using UBot.Core.Abstractions;
using UBot.Core.Client;

namespace UBot.GameData.ReferenceObjects;

public class RefAbilityByItemOptLevel : UBot.Core.Client.IReference<int>, UBot.Core.Abstractions.IReference
{
    public int Id;
    public uint ItemId;
    public byte OptLevel;

    public int PrimaryKey => Id;

    uint UBot.Core.Abstractions.IReference.ID => (uint)Id;
    public string CodeName => Id.ToString();

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

        if (!parser.TryParse(1, out Id))
            return false;

        parser.TryParse(2, out ItemId);
        parser.TryParse(3, out OptLevel);

        return true;
    }

    /// <summary>
    ///     Gets the links.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<uint> GetLinks()
    {
        return ReferenceProvider.Instance?.GetSkillLinks(Id) ?? System.Linq.Enumerable.Empty<uint>();
    }
}
