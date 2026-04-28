using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using UBot.FileSystem;
using UBot.NavMeshApi;
using UBot.NavMeshApi.Dungeon;
using UBot.NavMeshApi.Edges;
using UBot.NavMeshApi.Extensions;
using UBot.NavMeshApi.Terrain;
using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Extensions;
using UBot.Core.Network;
using UBot.Core.Network.Protocol;
using UBot.Core.Objects;
using UBot.Core.Objects.Party;
using UBot.Core.Objects.Spawn;
using UBot.Core.Objects.Skill;
using UBot.Core.Plugins;
using Forms = System.Windows.Forms;
using CoreRegion = UBot.Core.Objects.Region;
using static UBot.Avalonia.Services.UbotPluginConfigHelpers;


namespace UBot.Avalonia.Services;

internal sealed class UbotTradeBotbaseService : UbotServiceBase
{
    private sealed class TradeRouteListDefinition
    {
        public string Name { get; set; } = string.Empty;
        public List<string> Scripts { get; set; } = new();
    }

    private static Dictionary<string, object?> BuildTradeBotbaseConfig()
    {
        var routeLists = LoadTradeRouteListsFromPlayerConfig();
        var selectedRouteListIndex = Math.Clamp(
            PlayerConfig.Get("UBot.Trade.SelectedRouteListIndex", 0),
            0,
            Math.Max(0, routeLists.Count - 1));

        return new Dictionary<string, object?>
        {
            ["tradeTracePlayerName"] = PlayerConfig.Get("UBot.Trade.TracePlayerName", string.Empty),
            ["tradeTracePlayer"] = PlayerConfig.Get("UBot.Trade.TracePlayer", false),
            ["tradeUseRouteScripts"] = PlayerConfig.Get("UBot.Trade.UseRouteScripts", true),
            ["tradeSelectedRouteListIndex"] = selectedRouteListIndex,
            ["tradeRouteLists"] = routeLists.Select(routeList => new Dictionary<string, object?>
            {
                ["name"] = routeList.Name,
                ["scripts"] = routeList.Scripts.Cast<object?>().ToList()
            }).Cast<object?>().ToList(),
            ["tradeRunTownScript"] = PlayerConfig.Get("UBot.Trade.RunTownScript", false),
            ["tradeWaitForHunter"] = PlayerConfig.Get("UBot.Trade.WaitForHunter", false),
            ["tradeAttackThiefPlayers"] = PlayerConfig.Get("UBot.Trade.AttackThiefPlayers", false),
            ["tradeAttackThiefNpcs"] = PlayerConfig.Get("UBot.Trade.AttackThiefNpcs", false),
            ["tradeCounterAttack"] = PlayerConfig.Get("UBot.Trade.CounterAttack", false),
            ["tradeProtectTransport"] = PlayerConfig.Get("UBot.Trade.ProtectTransport", false),
            ["tradeCastBuffs"] = PlayerConfig.Get("UBot.Trade.CastBuffs", false),
            ["tradeMountTransport"] = PlayerConfig.Get("UBot.Trade.MountTransport", false),
            ["tradeMaxTransportDistance"] = Math.Clamp(PlayerConfig.Get("UBot.Trade.MaxTransportDistance", 15), 1, 300),
            ["tradeSellGoods"] = PlayerConfig.Get("UBot.Trade.SellGoods", true),
            ["tradeBuyGoods"] = PlayerConfig.Get("UBot.Trade.BuyGoods", true),
            ["tradeBuyGoodsQuantity"] = Math.Max(0, PlayerConfig.Get("UBot.Trade.BuyGoodsQuantity", 0)),
            ["tradeRecorderScriptPath"] = PlayerConfig.Get("UBot.Desktop.Trade.RecorderScriptPath", string.Empty)
        };
    }

    private static bool ApplyTradeBotbasePatch(IBotbase botbase, Dictionary<string, object?> patch)
    {
        var changed = false;
        var selectedRouteListIndex = PlayerConfig.Get("UBot.Trade.SelectedRouteListIndex", 0);
        var routeLists = LoadTradeRouteListsFromPlayerConfig();
        var routeListsChanged = false;

        bool? tracePlayerToggle = null;
        bool? useRouteScriptsToggle = null;

        changed |= SetPlayerString("UBot.Trade.TracePlayerName", patch, "tradeTracePlayerName");
        changed |= SetPlayerBool("UBot.Trade.RunTownScript", patch, "tradeRunTownScript");
        changed |= SetPlayerBool("UBot.Trade.WaitForHunter", patch, "tradeWaitForHunter");
        changed |= SetPlayerBool("UBot.Trade.AttackThiefPlayers", patch, "tradeAttackThiefPlayers");
        changed |= SetPlayerBool("UBot.Trade.AttackThiefNpcs", patch, "tradeAttackThiefNpcs");
        changed |= SetPlayerBool("UBot.Trade.CounterAttack", patch, "tradeCounterAttack");
        changed |= SetPlayerBool("UBot.Trade.ProtectTransport", patch, "tradeProtectTransport");
        changed |= SetPlayerBool("UBot.Trade.CastBuffs", patch, "tradeCastBuffs");
        changed |= SetPlayerBool("UBot.Trade.MountTransport", patch, "tradeMountTransport");
        changed |= SetPlayerInt("UBot.Trade.MaxTransportDistance", patch, "tradeMaxTransportDistance", 1, 300);
        changed |= SetPlayerBool("UBot.Trade.SellGoods", patch, "tradeSellGoods");
        changed |= SetPlayerBool("UBot.Trade.BuyGoods", patch, "tradeBuyGoods");
        changed |= SetPlayerInt("UBot.Trade.BuyGoodsQuantity", patch, "tradeBuyGoodsQuantity", 0, int.MaxValue);
        changed |= SetPlayerString("UBot.Desktop.Trade.RecorderScriptPath", patch, "tradeRecorderScriptPath");

        if (TryGetBoolValue(patch, "tradeTracePlayer", out var tracePlayer))
            tracePlayerToggle = tracePlayer;

        if (TryGetBoolValue(patch, "tradeUseRouteScripts", out var useRouteScripts))
            useRouteScriptsToggle = useRouteScripts;

        if (TryGetIntValue(patch, "tradeSelectedRouteListIndex", out var parsedRouteListIndex))
        {
            selectedRouteListIndex = parsedRouteListIndex;
            changed = true;
        }

        if (patch.TryGetValue("tradeRouteLists", out var tradeRouteListsRaw) && tradeRouteListsRaw != null)
        {
            routeLists = ParseTradeRouteListsPatch(tradeRouteListsRaw);
            routeListsChanged = true;
            changed = true;
        }

        if (tracePlayerToggle.HasValue || useRouteScriptsToggle.HasValue)
        {
            var finalUseRouteScripts = PlayerConfig.Get("UBot.Trade.UseRouteScripts", true);
            var finalTracePlayer = PlayerConfig.Get("UBot.Trade.TracePlayer", false);

            if (useRouteScriptsToggle.HasValue)
                finalUseRouteScripts = useRouteScriptsToggle.Value;
            if (tracePlayerToggle.HasValue)
                finalTracePlayer = tracePlayerToggle.Value;

            if (useRouteScriptsToggle.HasValue && !tracePlayerToggle.HasValue)
                finalTracePlayer = !finalUseRouteScripts;
            else if (!useRouteScriptsToggle.HasValue && tracePlayerToggle.HasValue)
                finalUseRouteScripts = !finalTracePlayer;
            else if (useRouteScriptsToggle.HasValue && tracePlayerToggle.HasValue && finalUseRouteScripts == finalTracePlayer)
                finalTracePlayer = !finalUseRouteScripts;

            PlayerConfig.Set("UBot.Trade.UseRouteScripts", finalUseRouteScripts);
            PlayerConfig.Set("UBot.Trade.TracePlayer", finalTracePlayer);
            changed = true;
        }

        routeLists = NormalizeTradeRouteLists(routeLists);
        if (routeListsChanged)
            SaveTradeRouteListsToPlayerConfig(routeLists);

        selectedRouteListIndex = Math.Clamp(selectedRouteListIndex, 0, Math.Max(0, routeLists.Count - 1));
        PlayerConfig.Set("UBot.Trade.SelectedRouteListIndex", selectedRouteListIndex);

        if (changed)
            EventManager.FireEvent("OnSavePlayerConfig");

        return changed;
    }

    private static List<TradeRouteListDefinition> ParseTradeRouteListsPatch(object raw)
    {
        if (raw is not IEnumerable enumerable || raw is string)
            return LoadTradeRouteListsFromPlayerConfig();

        var routeLists = new List<TradeRouteListDefinition>();
        foreach (var item in enumerable)
        {
            if (!TryConvertObjectToDictionary(item, out var row))
                continue;

            var listName = TryGetStringValue(row, "name", out var parsedName) ? parsedName : string.Empty;
            var scripts = new List<string>();
            if (row.TryGetValue("scripts", out var scriptsRaw) && scriptsRaw is IEnumerable scriptsEnum && scriptsRaw is not string)
            {
                foreach (var script in scriptsEnum)
                {
                    var value = script?.ToString()?.Trim();
                    if (!string.IsNullOrWhiteSpace(value))
                        scripts.Add(value);
                }
            }

            routeLists.Add(new TradeRouteListDefinition
            {
                Name = listName,
                Scripts = scripts
            });
        }

        return NormalizeTradeRouteLists(routeLists);
    }

    private static List<TradeRouteListDefinition> LoadTradeRouteListsFromPlayerConfig()
    {
        var names = PlayerConfig.GetArray<string>("UBot.Trade.RouteScriptList", ';')
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .ToList();

        if (names.Count == 0)
            names.Add("Default");

        var routeLists = new List<TradeRouteListDefinition>(names.Count);
        foreach (var name in names)
        {
            var scripts = PlayerConfig
                .GetArray<string>($"UBot.Trade.RouteScriptList.{name}")
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            routeLists.Add(new TradeRouteListDefinition
            {
                Name = name,
                Scripts = scripts
            });
        }

        return NormalizeTradeRouteLists(routeLists);
    }

    private static void SaveTradeRouteListsToPlayerConfig(IReadOnlyList<TradeRouteListDefinition> routeLists)
    {
        var normalizedLists = NormalizeTradeRouteLists(routeLists);
        var previousNames = PlayerConfig.GetArray<string>("UBot.Trade.RouteScriptList", ';')
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var currentNames = normalizedLists
            .Select(routeList => routeList.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var oldName in previousNames.Where(name => !currentNames.Contains(name, StringComparer.OrdinalIgnoreCase)))
            PlayerConfig.SetArray($"UBot.Trade.RouteScriptList.{oldName}", Array.Empty<string>());

        PlayerConfig.SetArray("UBot.Trade.RouteScriptList", currentNames, ";");
        foreach (var routeList in normalizedLists)
            PlayerConfig.SetArray($"UBot.Trade.RouteScriptList.{routeList.Name}", routeList.Scripts.Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
    }

    private static List<TradeRouteListDefinition> NormalizeTradeRouteLists(IEnumerable<TradeRouteListDefinition> routeLists)
    {
        var normalized = new List<TradeRouteListDefinition>();
        var takenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var routeList in routeLists ?? Array.Empty<TradeRouteListDefinition>())
        {
            var baseName = NormalizeTradeRouteListName(routeList?.Name, "Route List");
            var finalName = baseName;
            var suffix = 2;
            while (!takenNames.Add(finalName))
                finalName = $"{baseName} {suffix++}";

            var scripts = (routeList?.Scripts ?? new List<string>())
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Select(path => path.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            normalized.Add(new TradeRouteListDefinition
            {
                Name = finalName,
                Scripts = scripts
            });
        }

        if (normalized.Count == 0)
        {
            normalized.Add(new TradeRouteListDefinition
            {
                Name = "Default",
                Scripts = new List<string>()
            });
            return normalized;
        }

        if (!normalized.Any(routeList => routeList.Name.Equals("Default", StringComparison.OrdinalIgnoreCase)))
        {
            normalized.Insert(0, new TradeRouteListDefinition
            {
                Name = "Default",
                Scripts = new List<string>()
            });
        }

        return normalized;
    }

    private static string NormalizeTradeRouteListName(string value, string fallback)
    {
        var raw = (value ?? string.Empty).Trim();
        if (raw.Length == 0)
            raw = fallback;

        var cleaned = new string(raw.Where(ch => char.IsLetterOrDigit(ch) || ch == ' ' || ch == '-' || ch == '_').ToArray()).Trim();
        if (cleaned.Length == 0)
            cleaned = fallback;
        if (cleaned.Length > 48)
            cleaned = cleaned.Substring(0, 48).Trim();
        return cleaned;
    }

    private static object BuildTradeState(IBotbase botbase)
    {
        var player = Game.Player;
        var routeLists = LoadTradeRouteListsFromPlayerConfig();
        var selectedRouteListIndex = Math.Clamp(
            PlayerConfig.Get("UBot.Trade.SelectedRouteListIndex", 0),
            0,
            Math.Max(0, routeLists.Count - 1));

        var selectedRouteList = routeLists.Count > 0
            ? routeLists[selectedRouteListIndex]
            : new TradeRouteListDefinition { Name = "Default", Scripts = new List<string>() };

        var routeRows = selectedRouteList.Scripts
            .Select(BuildTradeRouteRow)
            .Cast<object?>()
            .ToList();

        var currentRouteFile = ScriptManager.File ?? string.Empty;
        var jobInfo = player?.JobInformation;

        return new Dictionary<string, object?>
        {
            ["selected"] = Kernel.Bot?.Botbase?.Name == botbase?.Name,
            ["useRouteScripts"] = PlayerConfig.Get("UBot.Trade.UseRouteScripts", true),
            ["tracePlayer"] = PlayerConfig.Get("UBot.Trade.TracePlayer", false),
            ["selectedRouteList"] = selectedRouteList.Name,
            ["selectedRouteListIndex"] = selectedRouteListIndex,
            ["routeRows"] = routeRows,
            ["scriptRunning"] = ScriptManager.Running,
            ["currentRouteFile"] = currentRouteFile,
            ["currentRouteName"] = string.IsNullOrWhiteSpace(currentRouteFile) ? string.Empty : Path.GetFileNameWithoutExtension(currentRouteFile),
            ["hasTransport"] = player?.JobTransport != null,
            ["transportDistance"] = player?.JobTransport != null ? Math.Round(player.JobTransport.Position.DistanceToPlayer(), 1) : -1d,
            ["jobOverview"] = new Dictionary<string, object?>
            {
                ["difficulty"] = player?.TradeInfo?.Scale ?? 0,
                ["alias"] = jobInfo?.Name ?? string.Empty,
                ["level"] = (int)(jobInfo?.Level ?? 0),
                ["experience"] = jobInfo?.Experience ?? 0L,
                ["type"] = (jobInfo?.Type ?? JobType.None).ToString()
            }
        };
    }

    private static Dictionary<string, object?> BuildTradeRouteRow(string scriptPath)
    {
        var normalizedPath = scriptPath?.Trim() ?? string.Empty;
        var routeName = string.IsNullOrWhiteSpace(normalizedPath)
            ? "(empty)"
            : Path.GetFileNameWithoutExtension(normalizedPath);
        if (string.IsNullOrWhiteSpace(routeName))
            routeName = Path.GetFileName(normalizedPath);

        var info = ReadTradeRouteScriptInfo(normalizedPath);
        return new Dictionary<string, object?>
        {
            ["path"] = normalizedPath,
            ["name"] = routeName,
            ["startRegion"] = info.StartRegion,
            ["endRegion"] = info.EndRegion,
            ["numSteps"] = info.StepCount,
            ["missing"] = info.Missing
        };
    }

    private static (string StartRegion, string EndRegion, int StepCount, bool Missing) ReadTradeRouteScriptInfo(string scriptPath)
    {
        if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
            return ("-", "-", 0, true);

        try
        {
            Position first = default;
            Position last = default;
            var found = false;
            var steps = 0;

            foreach (var line in File.ReadLines(scriptPath))
            {
                if (!TryParseTradeMoveCommand(line, out var point))
                    continue;

                if (!found)
                {
                    first = point;
                    found = true;
                }

                last = point;
                steps++;
            }

            if (!found)
                return ("-", "-", 0, false);

            return (
                first.Region.Id == 0 ? "-" : first.Region.Id.ToString(CultureInfo.InvariantCulture),
                last.Region.Id == 0 ? "-" : last.Region.Id.ToString(CultureInfo.InvariantCulture),
                steps,
                false);
        }
        catch
        {
            return ("-", "-", 0, true);
        }
    }

    private static bool TryParseTradeMoveCommand(string line, out Position point)
    {
        point = default;
        if (string.IsNullOrWhiteSpace(line))
            return false;

        var trimmed = line.Trim();
        if (trimmed.StartsWith("#", StringComparison.Ordinal) || trimmed.StartsWith("//", StringComparison.Ordinal))
            return false;

        var split = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (split.Length < 6 || !split[0].Equals("move", StringComparison.OrdinalIgnoreCase))
            return false;

        if (!float.TryParse(split[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var xOffset)
            || !float.TryParse(split[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var yOffset)
            || !float.TryParse(split[3], NumberStyles.Float, CultureInfo.InvariantCulture, out var zOffset)
            || !byte.TryParse(split[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out var xSector)
            || !byte.TryParse(split[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out var ySector))
        {
            return false;
        }

        point = new Position(xSector, ySector, xOffset, yOffset, zOffset);
        return true;
    }

    internal Dictionary<string, object?> BuildConfig() => BuildTradeBotbaseConfig();
    internal bool ApplyPatch(IBotbase botbase, Dictionary<string, object?> patch) => ApplyTradeBotbasePatch(botbase, patch);
    internal object BuildState(IBotbase botbase) => BuildTradeState(botbase);
}

