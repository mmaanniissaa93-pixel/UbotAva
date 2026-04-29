using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using UBot.Core.Abstractions.Services;
using UBot.Core.Services;

namespace UBot.Core.Components;

using LangDict = Dictionary<string, string>;

public class LanguageManager
{
    private static ILanguageService _service = new LanguageService();

    public static void Initialize(ILanguageService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        ServiceRuntime.Language = _service;
    }

    public static LangDict ParseLanguageFile(string file) => _service.ParseLanguageFile(file);

    public static string GetLang(string key) => _service.GetLang(key);

    public static string GetLang(string key, params object[] args) => _service.GetLang(key, args);

    public static string GetLangBySpecificKey(string parent, string key, string @default = "")
    {
        return _service.GetLangBySpecificKey(parent, key, @default);
    }

    public static void Translate(object view, string language = "en_US") => _service.Translate(view, language);

    public static Dictionary<string, string> GetLanguages() => _service.GetLanguages();
}

public sealed class LanguageService : ILanguageService
{
    private readonly Dictionary<string, LangDict> _values = new();

    public LangDict ParseLanguageFile(string file)
    {
        var languages = new LangDict();
        var lines = File.ReadAllLines(file);

        foreach (var line in lines)
        {
            var trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine) || !trimmedLine.Contains("="))
                continue;

            var parts = trimmedLine.Split(new[] { '=' }, 2);
            if (parts.Length < 2)
                continue;

            var key = parts[0].Trim();
            var value = parts[1].Trim().Trim('"');

            value = value
                .Replace("\\r\\n", "\r\n")
                .Replace("\\n", "\n")
                .Replace("\\r", "\r");

            languages.TryAdd(key, value);
        }

        return languages;
    }

    public string GetLang(string key)
    {
        var trace = new StackTrace();

        var parent = string.Empty;
        for (var i = 0; i < trace.FrameCount; i++)
        {
            parent = Path.GetFileNameWithoutExtension(trace.GetFrame(i).GetMethod().Module.Name);
            if (parent != "UBot.Core.Services" && parent != "UBot.Core")
                break;
        }

        if (_values.ContainsKey(parent) && _values[parent].ContainsKey(key))
            return _values[parent][key];

        return string.Empty;
    }

    public string GetLang(string key, params object[] args)
    {
        return string.Format(GetLang(key), args);
    }

    public string GetLangBySpecificKey(string parent, string key, string defaultValue = "")
    {
        if (_values.ContainsKey(parent) && _values[parent].ContainsKey(key))
            return _values[parent][key];

        return defaultValue;
    }

    public void Translate(object view, string language = "en_US")
    {
        if (view == null)
            return;

        var type = view.GetType();
        var assembly = type.Assembly.GetName().Name;
        var path = Path.Combine(LanguagePath, assembly, language + ".rsl");
        var dir = Path.GetDirectoryName(path);

        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        if (!File.Exists(path))
            return;

        var values = ParseLanguageFile(path);
        _values[assembly] = values;

        TranslateControls(values, view, assembly);
    }

    public Dictionary<string, string> GetLanguages()
    {
        var filePath = Path.Combine(LanguagePath, "langs.rsl");
        if (!File.Exists(filePath))
        {
            ServiceRuntime.Log?.Warn($"Language list file is missing! {filePath}");
            Environment.Exit(0);
        }

        return File.ReadAllLines(filePath).ToDictionary(p => p.Split(':')[0], p => p.Split(':')[1]);
    }

    private static void TranslateControls(LangDict values, object view, string header)
    {
        foreach (var control in EnumerateControls(view))
        {
            var parent = GetPropertyValue(control, "Parent");
            var parentName = parent?.GetType().Name ?? view.GetType().Name;
            var headerEx = $"{header}.{parentName}";

            if (IsToolStripLike(control))
            {
                foreach (var item in EnumerateItems(control, "Items"))
                foreach (var subItem in GetAllMenuItems(item))
                    TrySetTranslatedText(values, subItem, $"{headerEx}.{GetStringProperty(subItem, "Name")}");

                continue;
            }

            TrySetTranslatedText(values, control, $"{headerEx}.{GetStringProperty(control, "Name")}");
            TranslateControls(values, control, headerEx);
        }
    }

    private static IEnumerable<object> GetAllMenuItems(object menuItem)
    {
        if (menuItem == null)
            yield break;

        yield return menuItem;

        foreach (var item in EnumerateItems(menuItem, "DropDownItems"))
        foreach (var subItem in GetAllMenuItems(item))
            yield return subItem;
    }

    private static IEnumerable<object> EnumerateControls(object view)
    {
        return EnumerateItems(view, "Controls");
    }

    private static IEnumerable<object> EnumerateItems(object instance, string propertyName)
    {
        if (instance == null)
            yield break;

        var collection = GetPropertyValue(instance, propertyName) as IEnumerable;
        if (collection == null)
            yield break;

        foreach (var item in collection)
            if (item != null)
                yield return item;
    }

    private static bool IsToolStripLike(object control)
    {
        return control?.GetType().Name.Contains("ToolStrip", StringComparison.OrdinalIgnoreCase) == true;
    }

    private static void TrySetTranslatedText(LangDict values, object target, string key)
    {
        if (target == null || string.IsNullOrWhiteSpace(key))
            return;

        if (!values.TryGetValue(key, out var translatedText) || string.IsNullOrWhiteSpace(translatedText))
            return;

        var textProperty = target.GetType().GetProperty("Text", BindingFlags.Instance | BindingFlags.Public);
        if (textProperty?.CanWrite == true)
            textProperty.SetValue(target, translatedText);
    }

    private static object GetPropertyValue(object instance, string propertyName)
    {
        return instance?.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public)?.GetValue(instance);
    }

    private static string GetStringProperty(object instance, string propertyName)
    {
        return GetPropertyValue(instance, propertyName) as string ?? string.Empty;
    }

    private static string LanguagePath => Path.Combine(ServiceRuntime.Environment?.BasePath ?? string.Empty, "Data", "Languages");
}
