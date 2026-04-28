using System.Collections.Generic;
using System.Linq;
using UBot.Core.Abstractions;
using UBot.Core.Client;

namespace UBot.GameData.ReferenceObjects;

public class RefShop : UBot.Core.Client.IReference<string>, UBot.Core.Abstractions.IReference
{
    public string PrimaryKey => CodeName;

    uint UBot.Core.Abstractions.IReference.ID => (uint)Id;
    string UBot.Core.Abstractions.IReference.CodeName => CodeName;

    public string GetName()
    {
        return CodeName;
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

        parser.TryParse(1, out Country);
        parser.TryParse(2, out Id);
        parser.TryParse(3, out CodeName);

        return true;
    }

    /// <summary>
    ///     Gets the tabs.
    /// </summary>
    /// <returns></returns>
    public List<RefShopTab> GetTabs()
    {
        return ReferenceProvider.Instance?.GetShopTabs(CodeName).OfType<RefShopTab>().ToList()
            ?? new List<RefShopTab>();
    }

    #region Fields

    /// <summary>
    ///     Gets or sets the service.
    /// </summary>
    /// <value>
    ///     The service.
    /// </value>
    public byte Service;

    /// <summary>
    ///     Gets or sets the country.
    /// </summary>
    /// <value>
    ///     The country.
    /// </value>
    public int Country;

    /// <summary>
    ///     Gets or sets the identifier.
    /// </summary>
    /// <value>
    ///     The identifier.
    /// </value>
    public int Id;

    /// <summary>
    ///     Gets or sets the name of the code.
    /// </summary>
    /// <value>
    ///     The name of the code.
    /// </value>
    public string CodeName;

    #endregion Fields
}

//Service tinyint
//Country int
//ID  int
//CodeName128 varchar(129)
//Param1 int
//Param1_Desc128  varchar(129)
//Param2 int
//Param2_Desc128  varchar(129)
//Param3 int
//Param3_Desc128  varchar(129)
//Param4 int
//Param4_Desc128  varchar(129)
