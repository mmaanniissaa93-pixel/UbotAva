using UBot.Core.Client;

namespace UBot.GameData.ReferenceObjects;

public class RefMappingShopWithTab : UBot.Core.Client.IReference, UBot.Core.Abstractions.IReference
{
    uint UBot.Core.Abstractions.IReference.ID => 0;
    public string CodeName => $"{Shop}:{Tab}";

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
        parser.TryParse(2, out Shop);
        parser.TryParse(3, out Tab);

        return true;
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
    ///     Gets or sets the shop.
    /// </summary>
    /// <value>
    ///     The shop.
    /// </value>
    public string Shop;

    /// <summary>
    ///     Gets or sets the tab.
    /// </summary>
    /// <value>
    ///     The tab.
    /// </value>
    public string Tab;

    #endregion Fields
}
