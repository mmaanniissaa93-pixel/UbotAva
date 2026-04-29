using System.Collections.Generic;

namespace UBot.Core.Abstractions.Services;

public interface ILanguageService
{
    Dictionary<string, string> ParseLanguageFile(string file);
    string GetLang(string key);
    string GetLang(string key, params object[] args);
    string GetLangBySpecificKey(string parent, string key, string defaultValue = "");
    void Translate(object view, string language = "en_US");
    Dictionary<string, string> GetLanguages();
}
