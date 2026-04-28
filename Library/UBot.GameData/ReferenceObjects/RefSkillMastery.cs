using UBot.Core;
using UBot.Core.Abstractions;
using UBot.Core.Client;

namespace UBot.GameData.ReferenceObjects;

public class RefSkillMastery : UBot.Core.Client.IReference<uint>, UBot.Core.Abstractions.IReference
{
    public string Name => ReferenceProvider.Instance?.GetTranslation(NameCode) ?? NameCode;

    uint UBot.Core.Abstractions.IReference.ID => ID;
    public string CodeName => NameCode;

    public string GetName()
    {
        return Name;
    }

    public string GetRealName(bool displayRarity = false)
    {
        return Name;
    }

    #region Fields

    public uint ID;

    //public string Name;
    public string NameCode;

    //public byte GroupNum;
    //public string Description;
    //public string TabNameCode;
    //public byte TabID;

    //public byte SkillToolTipType;

    //public byte WeaponType1;
    //public byte WeaponType2;
    //public byte WeaponType3;
    //public string Icon;
    //public string FocusIcon;

    #endregion Fields

    #region IReference

    public uint PrimaryKey => ID;

    public bool Load(ReferenceParser parser)
    {
        parser.TryParse(0, out ID);
        //parser.TryParseString(1, out Name);
        if ((ReferenceProvider.Instance?.ClientType ?? GameClientType.Vietnam) >= GameClientType.Chinese_Old)
            parser.TryParse(3, out NameCode);
        else
            parser.TryParse(2, out NameCode);

        //parser.TryParseByte(3, out GroupNum);
        //parser.TryParseString(4, out Description);
        //parser.TryParseString(5, out TabNameCode);
        //parser.TryParseByte(6, out TabID);
        //parser.TryParseByte(7, out SkillToolTipType);
        //parser.TryParseByte(8, out WeaponType1);
        //parser.TryParseByte(9, out WeaponType2);
        //parser.TryParseByte(10, out WeaponType3);
        //parser.TryParseString(11, out Icon);
        //parser.TryParseString(12, out FocusIcon);

        return true;
    }

    #endregion IReference
}

//Mastery ID: 257
//Mastery Name - Do Not Use: ????101
//MasteryNameCode: UIIT_STT_MASTERY_VI
//GroupNum: 10
//Mastery Description ID: UIIT_STT_MASTERY_VI_EXPLANATION
//Tab Name Code: UIIT_CTL_WEAPON_SKILL
//Type (TabID): 0
//SkillToolTipType: 0
//Weapon Type 1: 2
//Weapon Type 2: 3
//Weapon Type 3: 0
//Mastery Icon: icon\skillmastery\china\mastery_sword.ddj
//Mastery Focus Icon: icon\skillmastery\china\mastery_sword_focus.ddj
