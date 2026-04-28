using UBot.Core.Abstractions;
using UBot.Core.Client;

namespace UBot.GameData.ReferenceObjects;

public class RefEventRewardItems : UBot.Core.Client.IReference, UBot.Core.Abstractions.IReference
{
    uint UBot.Core.Abstractions.IReference.ID => EventId;
    public string CodeName => EventCodeName;

    public string GetName()
    {
        return EventCodeName;
    }

    public string GetRealName(bool displayRarity = false)
    {
        return GetName();
    }

    public bool Load(ReferenceParser parser)
    {
        parser.TryParse(0, out EventId);
        parser.TryParse(1, out EventCodeName);
        parser.TryParse(2, out ItemCodeName);
        parser.TryParse(3, out ItemAmount);
        parser.TryParse(7, out MinRequiredLevel);
        parser.TryParse(8, out MaxRequiredLevel);

        return true;
    }

    #region Fields

    public uint EventId;
    public string EventCodeName;
    public string ItemCodeName;
    public ushort ItemAmount;
    public ushort MinRequiredLevel;
    public ushort MaxRequiredLevel;

    public RefObjItem Item => ItemCodeName == "xxx" ? null : ReferenceProvider.Instance?.GetRefItem(ItemCodeName) as RefObjItem;

    #endregion Fields
}
