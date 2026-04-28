using UBot.Core.Abstractions;
using UBot.Core.Client;

namespace UBot.GameData.ReferenceObjects;

public class RefQuestRewardItem : UBot.Core.Client.IReference, UBot.Core.Abstractions.IReference
{
    uint UBot.Core.Abstractions.IReference.ID => QuestId;
    public string CodeName => ItemCodeName;

    public string GetName()
    {
        return ItemCodeName;
    }

    public string GetRealName(bool displayRarity = false)
    {
        return GetName();
    }

    public bool Load(ReferenceParser parser)
    {
        parser.TryParse(0, out QuestId);
        parser.TryParse(1, out QuestCodeName);
        parser.TryParse(2, out RewardType);
        parser.TryParse(3, out ItemCodeName);
        parser.TryParse(4, out OptionalItemCode);
        parser.TryParse(5, out OptionalItemCount);
        parser.TryParse(6, out AchieveQuantity);
        parser.TryParse(7, out RentItemCodeName);

        return true;
    }

    #region Fields

    public uint QuestId;
    public string QuestCodeName;
    public byte RewardType;
    public string ItemCodeName;
    public string OptionalItemCode;
    public int OptionalItemCount;
    public int AchieveQuantity;
    public string RentItemCodeName;

    public RefObjItem Item => ItemCodeName == "xxx" ? null : ReferenceProvider.Instance?.GetRefItem(ItemCodeName) as RefObjItem;

    public RefObjItem OptionalItem =>
        OptionalItemCode == "xxx" ? null : ReferenceProvider.Instance?.GetRefItem(OptionalItemCode) as RefObjItem;

    public RefObjItem RentItem => OptionalItemCode == "xxx" ? null : ReferenceProvider.Instance?.GetRefItem(RentItemCodeName) as RefObjItem;

    #endregion Fields
}
