namespace UBot.Core.Abstractions;

public interface IRegion
{
    ushort RegionID { get; }
    byte X { get; }
    byte Y { get; }
    bool IsDungeon { get; }
}
