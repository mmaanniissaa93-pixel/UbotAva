using System.Collections.Generic;

namespace UBot.Core.Abstractions.Services;

public interface ILanguageService
{
    string GetLang(string key);
    string GetLang(string key, params object[] args);
    string GetLangBySpecificKey(string parent, string key, string defaultValue = "");
    Dictionary<string, string> GetLanguages();
}
