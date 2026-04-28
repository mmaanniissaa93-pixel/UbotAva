using UBot.Core.Abstractions;

namespace UBot.GameData.ReferenceObjects;

internal readonly struct GameDataRegion : IRegion
{
    public GameDataRegion(ushort regionId)
    {
        RegionID = regionId;
        X = (byte)(regionId & 0xff);
        Y = (byte)(regionId >> 8);
    }

    public ushort RegionID { get; }
    public byte X { get; }
    public byte Y { get; }
    public bool IsDungeon => (RegionID & 0x8000) != 0;
}

internal readonly struct GameDataPosition : IPosition
{
    public GameDataPosition(ushort regionId, float xOffset, float yOffset, float zOffset)
    {
        Region = new GameDataRegion(regionId);
        XOffset = xOffset;
        YOffset = yOffset;
        ZOffset = zOffset;
        Angle = 0;
        WorldId = 0;
        LayerId = 0;
    }

    public IRegion Region { get; }
    public ushort RegionID => Region.RegionID;
    public float X => XOffset == 0 ? 0 : Region.IsDungeon ? XOffset / 10 : (Region.X - 135) * 192 + XOffset / 10;
    public float Y => YOffset == 0 ? 0 : Region.IsDungeon ? YOffset / 10 : (Region.Y - 92) * 192 + YOffset / 10;
    public float Z => ZOffset;
    public float XOffset { get; }
    public float YOffset { get; }
    public float ZOffset { get; }
    public short Angle { get; }
    public short WorldId { get; }
    public short LayerId { get; }
}
