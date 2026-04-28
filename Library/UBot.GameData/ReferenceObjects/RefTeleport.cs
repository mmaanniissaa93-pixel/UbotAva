using System.Collections.Generic;
using System.Linq;
using UBot.Core.Abstractions;
using UBot.Core.Client;

namespace UBot.GameData.ReferenceObjects;

public class RefTeleport : UBot.Core.Client.IReference<uint>, UBot.Core.Abstractions.IReference
{
    uint UBot.Core.Abstractions.IReference.ID => ID;
    string UBot.Core.Abstractions.IReference.CodeName => CodeName;

    public string GetName()
    {
        return ZoneName;
    }

    public string GetRealName(bool displayRarity = false)
    {
        return GetName();
    }

    public bool Load(ReferenceParser parser)
    {
        //Skip disabled
        if (!parser.TryParse(0, out Service) || Service == 0)
            return false;

        //Skip invalid ID (PK)
        if (!parser.TryParse(1, out ID))
            return false;

        //Skip invalid CodeName
        if (!parser.TryParse(2, out CodeName))
            return false;

        parser.TryParse(3, out AssocRefObjId);
        parser.TryParse(4, out ZoneName128);
        parser.TryParse(5, out GenRegionID);
        parser.TryParse(6, out GenPos_X);
        parser.TryParse(7, out GenPos_Z);
        parser.TryParse(8, out GenPos_Y);
        parser.TryParse(9, out GenAreaRadius);
        parser.TryParse(10, out CanBeResurrectPos);
        parser.TryParse(11, out CanGotoResurrectPos);

        return true;
    }

    /// <summary>
    ///     Gets the links.
    /// </summary>
    /// <returns></returns>
    public List<RefTeleportLink> GetLinks()
    {
        return ReferenceProvider.Instance?.GetTeleportLinks(ID).OfType<RefTeleportLink>().ToList()
            ?? new List<RefTeleportLink>();
    }

    /// <summary>
    ///     Gets the position of the teleporter.
    /// </summary>
    /// <returns></returns>
    public IPosition GetPosition()
    {
        return new GameDataPosition(GenRegionID, GenPos_X, GenPos_Y, GenPos_Z);
    }

    #region Fields

    public byte Service;
    public uint ID;
    public string CodeName;
    public string AssocRefObjCodeName;
    public uint AssocRefObjId;
    public string ZoneName128;
    public ushort GenRegionID;
    public short GenPos_X;
    public short GenPos_Y;
    public short GenPos_Z;
    public short GenAreaRadius;
    public byte CanBeResurrectPos;
    public byte CanGotoResurrectPos;
    public string ZoneName => ReferenceProvider.Instance?.GetTranslation(ZoneName128) ?? ZoneName128;
    public RefObjChar Character => ReferenceProvider.Instance?.GetRefObjChar(AssocRefObjId) as RefObjChar;

    public uint PrimaryKey => ID;

    #endregion Fields
}
