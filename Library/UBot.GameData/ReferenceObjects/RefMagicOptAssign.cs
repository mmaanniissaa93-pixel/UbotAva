using System.Collections.Generic;
using UBot.Core.Client;

namespace UBot.GameData.ReferenceObjects;

public class RefMagicOptAssign : UBot.Core.Client.IReference, UBot.Core.Abstractions.IReference
{
    uint UBot.Core.Abstractions.IReference.ID => 0;
    public string CodeName => $"{Race}:{TypeId3}:{TypeId4}";

    public string GetName()
    {
        return CodeName;
    }

    public string GetRealName(bool displayRarity = false)
    {
        return GetName();
    }

    #region Methods

    public bool Load(ReferenceParser parser)
    {
        if (parser == null)
            return false;

        parser.TryParse(1, out Race);
        parser.TryParse(2, out TypeId3);
        parser.TryParse(3, out TypeId4);

        AvailableMagicOptions = new List<string>(80);
        for (var i = 4; i < parser.GetColumnCount(); i++)
            if (parser.TryParse(i, out string option))
                AvailableMagicOptions.Add(option);

        AvailableMagicOptions.RemoveAll(m => string.IsNullOrEmpty(m) || m == "xxx");

        return true;
    }

    #endregion Methods

    #region Fields

    public byte Race;
    public byte TypeId3;
    public byte TypeId4;
    public List<string> AvailableMagicOptions;

    #endregion Fields
}
