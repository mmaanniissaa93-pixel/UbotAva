namespace UBot.Core.Client.ReferenceObjects;

public class RefText : IReference<string>
{
    private const int LANG_OFFSET = 2;
    private const int LANG_COUNT = 14;
    public string Data;
    public string NameStrId;

    private byte Service;

    //private readonly string[] _data = new string[LANG_COUNT];

    #region IRefrerence

    public string PrimaryKey => NameStrId;

    #endregion IRefrerence

    // languageFlag
    // 0 - Korean
    // 1 - Chinese
    // 2 - Taiwan
    // 3 - Japan
    // 4 - English
    // 5 - Vietnam
    // 7 - Turkey
    // 8 - Thailand
    // 9 - Russia
    // 10 - Spain
    // 11 - Arabic

    //public string Lang0 => _data[OFFSET + 0];
    //public string Lang1 => _data[OFFSET + 1];
    //public string Lang2 => _data[OFFSET + 2];
    //public string Lang3 => _data[OFFSET + 3];
    //public string Lang4 => _data[OFFSET + 4];
    //public string Lang5 => _data[OFFSET + 5];
    //public string Lang6 => _data[OFFSET + 6];
    //public string Lang7 => _data[OFFSET + 7];
    //public string Lang8 => _data[OFFSET + 8];

    //public string this[int index] => _data[index];

    public bool Load(ReferenceParser parser)
    {
        if (!parser.TryParse(0, out Service) || Service == 0)
            return false;

        var nameStrIndex = 1;
        if (Game.ClientType >= GameClientType.Chinese)
            nameStrIndex = 2;

        if (!parser.TryParse(nameStrIndex, out NameStrId))
            return false;

        foreach (var languageTab in GetLanguageTabs(parser.GetColumnCount(), nameStrIndex))
        {
            if (parser.TryParse(languageTab, out Data) && !IsEmptyString(Data))
                return true;
        }

        Data = NameStrId;

        return true;
    }

    private static System.Collections.Generic.IEnumerable<int> GetLanguageTabs(int columnCount, int nameStrIndex)
    {
        var yielded = new System.Collections.Generic.HashSet<int>();

        foreach (var tab in GetPreferredLanguageTabs())
            if (IsUsableLanguageTab(tab, columnCount, nameStrIndex) && yielded.Add(tab))
                yield return tab;

        for (var tab = nameStrIndex + 1; tab < columnCount; tab++)
            if (IsUsableLanguageTab(tab, columnCount, nameStrIndex) && yielded.Add(tab))
                yield return tab;
    }

    private static bool IsUsableLanguageTab(int tab, int columnCount, int nameStrIndex)
    {
        if (tab <= nameStrIndex || tab >= columnCount)
            return false;

        return Game.ClientType != GameClientType.Turkey || tab != nameStrIndex + 1;
    }

    private static System.Collections.Generic.IEnumerable<int> GetPreferredLanguageTabs()
    {
        if (Game.ClientType == GameClientType.Turkey)
            yield return 13;

        if (Game.ClientType == GameClientType.RuSro)
            yield return 12;

        if (Game.ClientType == GameClientType.Japanese)
            yield return 9;

        if (Game.ReferenceManager?.LanguageTab > 0)
            yield return Game.ReferenceManager.LanguageTab;

        yield return 8;
    }

    private bool IsEmptyString(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return true;

        if (data == "0")
            return true;

        if (data.StartsWith("?"))
            return true;

        return false;
    }
}
