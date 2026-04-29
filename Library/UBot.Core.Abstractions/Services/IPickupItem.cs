namespace UBot.Core.Abstractions.Services;

public interface IPickupItem
{
    uint UniqueId { get; }
    uint OwnerJid { get; }
    bool HasOwner { get; }
    bool IsBehindObstacle { get; }
    bool IsSpecialtyGoodBox { get; }
    bool IsGold { get; }
    bool IsQuest { get; }
    bool IsEquip { get; }
    byte Rarity { get; }
    string CodeName { get; }
    object SourcePosition { get; }
}
