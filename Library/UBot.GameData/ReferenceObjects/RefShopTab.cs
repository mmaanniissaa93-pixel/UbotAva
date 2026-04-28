using System.Collections.Generic;
using System.Linq;
using UBot.Core.Abstractions;
using UBot.Core.Client;

namespace UBot.GameData.ReferenceObjects;

public class RefShopTab : UBot.Core.Client.IReference<string>, UBot.Core.Abstractions.IReference
{
    public string PrimaryKey => CodeName;

    uint UBot.Core.Abstractions.IReference.ID => (uint)Id;
    string UBot.Core.Abstractions.IReference.CodeName => CodeName;

    public string GetName()
    {
        return StrID128_Tab ?? CodeName;
    }

    public string GetRealName(bool displayRarity = false)
    {
        return ReferenceProvider.Instance?.GetTranslation(StrID128_Tab) ?? GetName();
    }

    public bool Load(ReferenceParser parser)
    {
        //Skip disabled
        if (!parser.TryParse(0, out Service) || Service == 0)
            return false;

        parser.TryParse(1, out Country);
        parser.TryParse(2, out Id);
        parser.TryParse(3, out CodeName);
        parser.TryParse(4, out RefTabGroupCodeName);
        parser.TryParse(5, out StrID128_Tab);

        return true;
    }

    /// <summary>
    ///     Gets the goods.
    /// </summary>
    /// <returns></returns>
    public List<RefShopGood> GetGoods()
    {
        return ReferenceProvider.Instance?.GetShopGoods(CodeName).OfType<RefShopGood>().ToList()
            ?? new List<RefShopGood>();
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

    /// <summary>
    ///     Gets or sets the name of the reference tab group code.
    /// </summary>
    /// <value>
    ///     The name of the reference tab group code.
    /// </value>
    public string RefTabGroupCodeName;

    /// <summary>
    ///     Gets or sets the string i D128_ tab.
    /// </summary>
    /// <value>
    ///     The string i D128_ tab.
    /// </value>
    public string StrID128_Tab;

    #endregion Fields
}

//Service tinyint
//Country int
//ID  int
//CodeName128 varchar(129)
//RefTabGroupCodeName varchar(129)
//StrID128_Tab varchar(129)
