namespace UBot.Core.Abstractions;

public interface IPosition
{
    IRegion Region { get; }
    ushort RegionID { get; }
    float X { get; }
    float Y { get; }
    float Z { get; }
    float XOffset { get; }
    float YOffset { get; }
    float ZOffset { get; }
    short Angle { get; }
    short WorldId { get; }
    short LayerId { get; }
}
