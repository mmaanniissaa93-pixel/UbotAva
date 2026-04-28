using UBot.Core;
using UBot.Core.Abstractions;
using UBot.Core.Client;

namespace UBot.GameData.ReferenceObjects;

public class RefLevel : UBot.Core.Client.IReference<byte>, UBot.Core.Abstractions.IReference
{
    public byte PrimaryKey => Level;

    uint UBot.Core.Abstractions.IReference.ID => Level;
    string UBot.Core.Abstractions.IReference.CodeName => Level.ToString();

    public string GetName()
    {
        return Level.ToString();
    }

    public string GetRealName(bool displayRarity = false)
    {
        return GetName();
    }

    public bool Load(ReferenceParser parser)
    {
        if (!parser.TryParse(0, out Level))
            return false;

        parser.TryParse(1, out Exp_C);
        parser.TryParse(2, out Exp_M);

        if ((ReferenceProvider.Instance?.ClientType ?? GameClientType.Vietnam) >= GameClientType.Chinese_Old)
        {
            parser.TryParse(9, out Exp_C_Pet2);
            parser.TryParse(10, out StoredSp_Pet2);
        }

        return true;
    }

    #region Fields

    public byte Level;
    public long Exp_C;

    public int Exp_M;

    /*public int Cost_M;
    public int Cost_ST;
    public int GUST_Mob_Exp;
    public int JobExp_Trader;
    public int JobExp_Robber;
    public int JobExp_Hunter;*/
    public long Exp_C_Pet2;
    public int StoredSp_Pet2;

    #endregion Fields
}
