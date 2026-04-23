using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.VisualTree;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace UBot.Avalonia.Services;

public static class DesktopLanguageService
{
    private static readonly object Sync = new();
    private static bool _loaded;
    private static readonly Dictionary<string, string> EnToTr = new(StringComparer.Ordinal);
    private static readonly Dictionary<string, string> TrToEn = new(StringComparer.Ordinal);

    public static string CurrentLanguage { get; private set; } = "English";

    public static void SetLanguage(string language)
    {
        CurrentLanguage = string.Equals(language, "Turkish", StringComparison.OrdinalIgnoreCase)
            ? "Turkish"
            : "English";
    }

    public static void ApplyToControl(Control root, string language)
    {
        if (root == null)
            return;

        EnsureLoaded();
        SetLanguage(language);
        ApplyRecursive(root, CurrentLanguage == "Turkish");
    }

    public static string Translate(string source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return source;

        EnsureLoaded();
        return CurrentLanguage == "Turkish"
            ? TranslateToTarget(source, EnToTr)
            : TranslateToTarget(source, TrToEn);
    }

    private static void EnsureLoaded()
    {
        lock (Sync)
        {
            if (_loaded)
                return;

            LoadFromCentralDictionary();
            _loaded = true;
        }
    }

    private static void LoadFromCentralDictionary()
    {
        const string uri = "avares://UBot.Avalonia/Assets/Localization/translations.json";
        try
        {
            using var stream = AssetLoader.Open(new Uri(uri));
            using var doc = JsonDocument.Parse(stream);
            if (!doc.RootElement.TryGetProperty("pairs", out var pairs) || pairs.ValueKind != JsonValueKind.Object)
                return;

            foreach (var pair in pairs.EnumerateObject())
            {
                var en = pair.Name?.Trim();
                var tr = pair.Value.GetString()?.Trim();
                if (string.IsNullOrWhiteSpace(en) || string.IsNullOrWhiteSpace(tr))
                    continue;

                AddPair(en, tr);
            }
        }
        catch
        {
            // Keep UI stable if the dictionary fails to load.
        }
    }

    private static void AddPair(string en, string tr)
    {
        if (string.IsNullOrWhiteSpace(en) || string.IsNullOrWhiteSpace(tr))
            return;

        if (!EnToTr.ContainsKey(en))
            EnToTr[en] = tr;
        if (!TrToEn.ContainsKey(tr))
            TrToEn[tr] = en;
    }

    private static void ApplyRecursive(Control control, bool toTurkish)
    {
        ApplyOne(control, toTurkish);

        foreach (var child in control.GetVisualDescendants().OfType<Control>())
        {
            if (child == control)
                continue;

            ApplyOne(child, toTurkish);
        }
    }

    private static void ApplyOne(Control control, bool toTurkish)
    {
        if (control is TextBlock tb && !string.IsNullOrWhiteSpace(tb.Text))
            tb.Text = toTurkish ? TranslateToTarget(tb.Text, EnToTr) : TranslateToTarget(tb.Text, TrToEn);

        if (control is ContentControl cc && cc.Content is string content && !string.IsNullOrWhiteSpace(content))
            cc.Content = toTurkish ? TranslateToTarget(content, EnToTr) : TranslateToTarget(content, TrToEn);

        if (control is TextBox input && input.Watermark is string watermark && !string.IsNullOrWhiteSpace(watermark))
            input.Watermark = toTurkish ? TranslateToTarget(watermark, EnToTr) : TranslateToTarget(watermark, TrToEn);

        if (control is MenuItem menu && menu.Header is string header && !string.IsNullOrWhiteSpace(header))
            menu.Header = toTurkish ? TranslateToTarget(header, EnToTr) : TranslateToTarget(header, TrToEn);
    }

    private static string TranslateToTarget(string source, Dictionary<string, string> map)
    {
        if (map.TryGetValue(source, out var translated))
            return translated;

        return source;
    }
}
