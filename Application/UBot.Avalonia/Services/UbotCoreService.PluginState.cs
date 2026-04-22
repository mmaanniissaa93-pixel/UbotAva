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

namespace UBot.Avalonia.Services;

internal sealed partial class UbotPluginModuleService : UbotServiceBase
{
    private readonly UbotConnectionService _connectionService;
    private readonly UbotMapService _mapService;

    public Task<PluginStateDto> GetPluginStateAsync(string pluginId)
    {
        var statusSnapshot = _connectionService.CreateStatusSnapshot();
        var state = new Dictionary<string, object?>
        {
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["botRunning"] = Kernel.Bot != null && Kernel.Bot.Running,
            ["statusText"] = statusSnapshot.StatusText,
            ["player"] = statusSnapshot.Player
        };

        if (TryResolvePlugin(pluginId, out var plugin) && IsSkillsPlugin(plugin))
            state["skills"] = BuildSkillsPluginState();

        if (TryResolvePlugin(pluginId, out plugin) && IsInventoryPlugin(plugin))
            state["inventory"] = BuildInventoryPluginState();

        if (TryResolvePlugin(pluginId, out plugin) && IsMapPlugin(plugin))
            state["map"] = _mapService.BuildMapPluginStateSnapshot();

        if (TryResolvePlugin(pluginId, out plugin) && IsPartyPlugin(plugin))
            state["party"] = BuildPartyPluginState();

        if (TryResolvePlugin(pluginId, out plugin) && IsStatisticsPlugin(plugin))
            state["stats"] = BuildStatisticsState();

        if (TryResolvePlugin(pluginId, out plugin) && IsQuestPlugin(plugin))
            state["quests"] = BuildQuestState();

        if (TryResolvePlugin(pluginId, out plugin) && IsTargetAssistPlugin(plugin))
            state["targetAssist"] = BuildTargetAssistState();

        if (TryResolveBotbase(pluginId, out var botbase) && IsLureBotbase(botbase))
            state["lure"] = BuildLureState(botbase);

        if (TryResolveBotbase(pluginId, out botbase) && IsTradeBotbase(botbase))
            state["trade"] = BuildTradeState(botbase);

        if (TryResolveBotbase(pluginId, out botbase) && IsAlchemyBotbase(botbase))
            state["alchemy"] = BuildAlchemyState(botbase);

        var dto = new PluginStateDto
        {
            Id = pluginId ?? string.Empty,
            Enabled = ResolveEnabledState(pluginId),
            State = ToJsonElement(state)
        };

        return Task.FromResult(dto);
    }


    private static bool ResolveEnabledState(string pluginId)
    {
        if (TryResolvePlugin(pluginId, out var plugin))
            return plugin.Enabled;
        if (TryResolveBotbase(pluginId, out var botbase))
            return Kernel.Bot?.Botbase?.Name == botbase.Name;
        return false;
    }


    private static object BuildInventoryPluginState()
    {
        var player = Game.Player;
        if (player == null) return new { selectedTab = "Inventory", items = new List<object>(), freeSlots = 0, totalSlots = 0 };

        var type = PlayerConfig.Get("UBot.Desktop.Inventory.SelectedTab", "Inventory");
        var items = new List<InventoryItemDto>();
        var freeSlots = 0;
        var totalSlots = 0;

        try
        {
            switch (type)
            {
                case "Inventory":
                    if (player.Inventory != null)
                    {
                        // Filter out equipment slots (0-12) for the main inventory tab
                        items = player.Inventory.Where(x => x?.Record != null && x.Slot >= 13).Select(ToInventoryItemDto).ToList();
                        freeSlots = player.Inventory.FreeSlots;
                        totalSlots = player.Inventory.Capacity;
                    }
                    break;
                case "Equipment":
                    if (player.Inventory != null)
                        items = player.Inventory.Where(x => x?.Record != null && x.Slot < 13).Select(ToInventoryItemDto).ToList();
                    break;
                case "Avatars":
                    if (player.Avatars != null)
                        items = player.Avatars.Where(x => x?.Record != null).Select(ToInventoryItemDto).ToList();
                    break;
                case "Storage":
                    if (player.Storage != null)
                    {
                        items = player.Storage.Where(x => x?.Record != null).Select(ToInventoryItemDto).ToList();
                        freeSlots = player.Storage.FreeSlots;
                        totalSlots = player.Storage.Capacity;
                    }
                    break;
                case "Guild Storage":
                    if (player.GuildStorage != null)
                    {
                        items = player.GuildStorage.Where(x => x?.Record != null).Select(ToInventoryItemDto).ToList();
                        freeSlots = player.GuildStorage.FreeSlots;
                        totalSlots = player.GuildStorage.Capacity;
                    }
                    break;
                case "Grab Pet":
                    if (player.AbilityPet?.Inventory != null)
                    {
                        items = player.AbilityPet.Inventory.Where(x => x?.Record != null).Select(ToInventoryItemDto).ToList();
                        freeSlots = player.AbilityPet.Inventory.FreeSlots;
                        totalSlots = player.AbilityPet.Inventory.Capacity;
                    }
                    break;
                case "Job Transport":
                    if (player.JobTransport?.Inventory != null)
                    {
                        items = player.JobTransport.Inventory.Where(x => x?.Record != null).Select(ToInventoryItemDto).ToList();
                        freeSlots = player.JobTransport.Inventory.FreeSlots;
                        totalSlots = player.JobTransport.Inventory.Capacity;
                    }
                    break;
                case "Specialty":
                    if (player.Job2SpecialtyBag != null)
                    {
                        items = player.Job2SpecialtyBag.Where(x => x?.Record != null).Select(ToInventoryItemDto).ToList();
                        freeSlots = player.Job2SpecialtyBag.FreeSlots;
                        totalSlots = player.Job2SpecialtyBag.Capacity;
                    }
                    break;
                case "Job Equipment":
                    if (player.Job2 != null)
                    {
                        items = player.Job2.Where(x => x?.Record != null).Select(ToInventoryItemDto).ToList();
                        freeSlots = player.Job2.FreeSlots;
                        totalSlots = player.Job2.Capacity;
                    }
                    break;
                case "Fellow Pet":
                    if (player.Fellow?.Inventory != null)
                    {
                        items = player.Fellow.Inventory.Where(x => x?.Record != null).Select(ToInventoryItemDto).ToList();
                        freeSlots = player.Fellow.Inventory.FreeSlots;
                        totalSlots = player.Fellow.Inventory.Capacity;
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error building inventory state: {ex.Message}");
        }

        return new
        {
            selectedTab = type,
            items = items,
            freeSlots = freeSlots,
            totalSlots = totalSlots,
            autoSort = PlayerConfig.Get("UBot.Inventory.AutoSort", false)
        };
    }

    private static InventoryItemDto ToInventoryItemDto(InventoryItem item)
    {
        return new InventoryItemDto
        {
            Slot = item.Slot,
            Name = item.Record?.GetRealName() ?? "Unknown",
            Amount = item.Amount,
            Opt = item.OptLevel,
            Icon = item.Record?.AssocFileIcon ?? "",
            CanUse = item.Record != null && (item.Record.CanUse & ObjectUseType.Yes) != 0,
            CanDrop = item.Record != null && item.Record.CanDrop != ObjectDropType.No,
            IsReverseReturnScroll = item.Equals(ReverseReturnScrollFilter),
            Code = item.Record?.CodeName ?? ""
        };
    }


    private static Dictionary<string, object?> BuildPartyPluginState()
    {
        var settings = GetPartySettingsSnapshot();
        var members = new List<Dictionary<string, object?>>();
        var party = Game.Party;

        if (party?.Members != null)
        {
            foreach (var member in party.Members.Where(member => member != null))
            {
                var hpPercent = ((member.HealthMana >> 4) & 0x0F) * 10;
                var mpPercent = (member.HealthMana & 0x0F) * 10;

                hpPercent = Math.Clamp(hpPercent, 0, 100);
                mpPercent = Math.Clamp(mpPercent, 0, 100);

                var positionText = $"{member.Position.X:0.0}, {member.Position.Y:0.0}";

                members.Add(new Dictionary<string, object?>
                {
                    ["memberId"] = member.MemberId,
                    ["name"] = member.Name ?? string.Empty,
                    ["level"] = member.Level,
                    ["guild"] = member.Guild ?? string.Empty,
                    ["hpPercent"] = hpPercent,
                    ["mpPercent"] = mpPercent,
                    ["hpMp"] = $"{hpPercent}/{mpPercent}",
                    ["position"] = positionText
                });
            }
        }

        var buffCatalog = BuildPartyBuffCatalog(PlayerConfig.Get("UBot.Party.Buff.HideLowLevelSkills", false));
        var assignments = ParsePartyBuffAssignments(PlayerConfig.GetArray<string>("UBot.Party.Buff.Assignments"));
        var memberBuffs = assignments
            .Select(assignment => new Dictionary<string, object?>
            {
                ["name"] = assignment.Name,
                ["group"] = assignment.Group,
                ["buffs"] = assignment.Buffs.Cast<object?>().ToList()
            })
            .Cast<object?>()
            .ToList();

        var pluginConfig = LoadPluginJsonConfig(PartyPluginName);
        pluginConfig.TryGetValue("matchingResults", out var matchingResultsRaw);
        var matchingResults = matchingResultsRaw as IList ?? new List<object?>();

        return new Dictionary<string, object?>
        {
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["isInParty"] = party?.IsInParty == true,
            ["isLeader"] = party?.IsLeader == true,
            ["leaderName"] = party?.Leader?.Name ?? "Not in a party",
            ["canInvite"] = party?.CanInvite == true,
            ["expAutoShare"] = settings.ExperienceAutoShare,
            ["itemAutoShare"] = settings.ItemAutoShare,
            ["allowInvitations"] = settings.AllowInvitation,
            ["members"] = members.Cast<object?>().ToList(),
            ["buffCatalog"] = buffCatalog.Cast<object?>().ToList(),
            ["memberBuffs"] = memberBuffs,
            ["matchingResults"] = matchingResults.Cast<object?>().ToList()
        };
    }

    private static object BuildLureState(IBotbase botbase)
    {
        var area = botbase?.Area;
        var areaPosition = area?.Position ?? default;
        var playerPosition = Game.Player?.Position ?? default;

        return new Dictionary<string, object?>
        {
            ["selected"] = Kernel.Bot?.Botbase?.Name == botbase?.Name,
            ["centerRegion"] = (int)areaPosition.Region.Id,
            ["centerXSector"] = (int)areaPosition.Region.X,
            ["centerYSector"] = (int)areaPosition.Region.Y,
            ["centerX"] = Math.Round(areaPosition.XOffset, 2),
            ["centerY"] = Math.Round(areaPosition.YOffset, 2),
            ["centerZ"] = Math.Round(areaPosition.ZOffset, 2),
            ["radius"] = area?.Radius ?? Math.Clamp(PlayerConfig.Get("UBot.Lure.Area.Radius", 20), 1, 200),
            ["currentPosition"] = new Dictionary<string, object?>
            {
                ["region"] = (int)playerPosition.Region.Id,
                ["xSector"] = (int)playerPosition.Region.X,
                ["ySector"] = (int)playerPosition.Region.Y,
                ["x"] = Math.Round(playerPosition.XOffset, 2),
                ["y"] = Math.Round(playerPosition.YOffset, 2),
                ["z"] = Math.Round(playerPosition.ZOffset, 2)
            },
            ["hasCenter"] = areaPosition.Region.Id != 0 || areaPosition.XOffset != 0 || areaPosition.YOffset != 0
        };
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

    private static object BuildAlchemyState(IBotbase botbase)
    {
        var selectableItems = GetAlchemySelectableItems();
        var selectedItem = ResolveAlchemySelectedItem(selectableItems);
        var blues = BuildAlchemyBlueRows(selectedItem);
        var stats = BuildAlchemyStatRows(selectedItem);

        return new Dictionary<string, object?>
        {
            ["selected"] = Kernel.Bot?.Botbase?.Name == botbase?.Name,
            ["mode"] = NormalizeAlchemyMode(PlayerConfig.Get(AlchemyModeKey, "enhance")),
            ["hasItem"] = selectedItem != null,
            ["selectedItem"] = selectedItem == null
                ? null
                : new Dictionary<string, object?>
                {
                    ["codeName"] = selectedItem.Record?.CodeName ?? string.Empty,
                    ["name"] = selectedItem.Record?.GetRealName(true) ?? selectedItem.ItemId.ToString(CultureInfo.InvariantCulture),
                    ["degree"] = selectedItem.Record?.Degree ?? 0,
                    ["optLevel"] = selectedItem.OptLevel,
                    ["slot"] = selectedItem.Slot
                },
            ["luckyPowderCount"] = selectedItem != null ? GetAlchemyLuckyPowderCount(selectedItem) : 0,
            ["luckyStoneCount"] = selectedItem != null ? GetAlchemyStonesByGroup(selectedItem, RefMagicOpt.MaterialLuck).Sum(item => item.Amount) : 0,
            ["immortalStoneCount"] = selectedItem != null ? GetAlchemyStonesByGroup(selectedItem, RefMagicOpt.MaterialImmortal).Sum(item => item.Amount) : 0,
            ["astralStoneCount"] = selectedItem != null ? GetAlchemyStonesByGroup(selectedItem, RefMagicOpt.MaterialAstral).Sum(item => item.Amount) : 0,
            ["steadyStoneCount"] = selectedItem != null ? GetAlchemyStonesByGroup(selectedItem, RefMagicOpt.MaterialSteady).Sum(item => item.Amount) : 0,
            ["itemsCatalog"] = selectableItems
                .GroupBy(item => item.Record?.CodeName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(item => item.OptLevel).ThenBy(item => item.Slot).First())
                .OrderBy(item => item.Record?.GetRealName() ?? string.Empty)
                .Select(item => new Dictionary<string, object?>
                {
                    ["codeName"] = item.Record?.CodeName ?? string.Empty,
                    ["name"] = $"{item.Record?.GetRealName(true) ?? item.ItemId.ToString(CultureInfo.InvariantCulture)} (+{item.OptLevel})",
                    ["degree"] = item.Record?.Degree ?? 0,
                    ["optLevel"] = item.OptLevel,
                    ["slot"] = item.Slot
                })
                .Cast<object?>()
                .ToList(),
            ["alchemyBlues"] = blues.Cast<object?>().ToList(),
            ["alchemyStats"] = stats.Cast<object?>().ToList()
        };
    }

    private static object[] BuildAlchemyBlueRows(InventoryItem selectedItem)
    {
        if (selectedItem?.Record == null)
            return Array.Empty<object>();

        var degree = selectedItem.Record.Degree;
        var rows = AlchemyBlueOptions.Select(option =>
        {
            var currentValue = GetAlchemyMagicOptionValue(selectedItem, option.Group);
            var maxValue = GetAlchemyMagicOptionMaxValue(selectedItem, option.Group);
            var stones = GetAlchemyStonesByGroup(selectedItem, option.Group).Sum(item => item.Amount);

            return new Dictionary<string, object?>
            {
                ["key"] = option.Key,
                ["name"] = option.Label,
                ["value"] = currentValue.ToString(CultureInfo.InvariantCulture),
                ["current"] = (int)currentValue,
                ["max"] = (int)maxValue,
                ["stoneCount"] = stones,
                ["group"] = option.Group,
                ["degree"] = degree
            };
        }).Cast<object>().ToList();

        rows.Add(new Dictionary<string, object?>
        {
            ["key"] = "availableSlots",
            ["name"] = "Available slots",
            ["value"] = selectedItem.MagicOptions?.Count.ToString(CultureInfo.InvariantCulture) ?? "0",
            ["current"] = selectedItem.MagicOptions?.Count ?? 0,
            ["max"] = selectedItem.MagicOptions?.Count ?? 0,
            ["stoneCount"] = 0,
            ["group"] = string.Empty,
            ["degree"] = degree
        });

        return rows.ToArray();
    }

    private static object[] BuildAlchemyStatRows(InventoryItem selectedItem)
    {
        if (selectedItem?.Record == null)
            return Array.Empty<object>();

        var availableGroups = ItemAttributesInfo.GetAvailableAttributeGroupsForItem(selectedItem.Record)?.ToHashSet()
            ?? new HashSet<ItemAttributeGroup>();

        return AlchemyStatOptions
            .Select(option =>
            {
                var currentPercent = availableGroups.Contains(option.Group)
                    ? GetAlchemyAttributePercentage(selectedItem, option.Group)
                    : 0;

                return new Dictionary<string, object?>
                {
                    ["key"] = option.Key,
                    ["name"] = option.Label,
                    ["value"] = currentPercent > 0 ? $"+{currentPercent}%" : "0",
                    ["current"] = currentPercent
                };
            })
            .Cast<object>()
            .ToArray();
    }

    private static IReadOnlyList<InventoryItem> GetAlchemySelectableItems()
    {
        var inventory = Game.Player?.Inventory;
        if (inventory == null)
            return Array.Empty<InventoryItem>();

        return inventory
            .GetNormalPartItems(item =>
                item?.Record != null
                && item.Record.IsEquip
                && !item.Record.IsAvatar
                && (item.Record.IsWeapon || item.Record.IsShield || item.Record.IsArmor || item.Record.IsAccessory))
            .OrderBy(item => item.Slot)
            .ToArray();
    }

    private static InventoryItem ResolveAlchemySelectedItem(IEnumerable<InventoryItem>? candidates = null)
    {
        var source = candidates?.ToList() ?? GetAlchemySelectableItems().ToList();
        if (source.Count == 0)
            return null;

        var codeName = (PlayerConfig.Get(AlchemyItemCodeKey, string.Empty) ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(codeName))
        {
            var matched = source.FirstOrDefault(item =>
                item.Record?.CodeName != null
                && item.Record.CodeName.Equals(codeName, StringComparison.OrdinalIgnoreCase));
            if (matched != null)
                return matched;
        }

        return source.FirstOrDefault();
    }

    private static IReadOnlyList<InventoryItem> GetAlchemyStonesByGroup(InventoryItem targetItem, string group)
    {
        if (targetItem?.Record == null || string.IsNullOrWhiteSpace(group))
            return Array.Empty<InventoryItem>();

        var inventory = Game.Player?.Inventory;
        if (inventory == null)
            return Array.Empty<InventoryItem>();

        return inventory
            .GetNormalPartItems(item =>
                item?.Record != null
                && item.Record.Desc1 == group
                && item.Record.ItemClass == targetItem.Record.Degree)
            .ToArray();
    }

    private static IReadOnlyList<InventoryItem> ResolveAlchemyElixirs(InventoryItem targetItem, string elixirType)
    {
        var inventory = Game.Player?.Inventory;
        if (inventory == null || targetItem?.Record == null)
            return Array.Empty<InventoryItem>();

        const int protectorParam = 16909056;
        const int weaponParam = 100663296;
        const int accessoryParam = 83886080;
        const int shieldParam = 67108864;

        var normalizedType = NormalizeAlchemyElixirType(elixirType);
        var paramValue = normalizedType switch
        {
            "shield" => shieldParam,
            "protector" => protectorParam,
            "accessory" => accessoryParam,
            _ => weaponParam
        };

        var degree = targetItem.Record.Degree;
        Func<InventoryItem, bool> predicate;
        if (Game.ClientType >= GameClientType.Chinese && degree >= 12)
            predicate = item => item.Record.Param1 == degree && item.Record.Param3 == paramValue;
        else
            predicate = item => item.Record.Param1 == paramValue;

        return inventory.GetNormalPartItems(item => item?.Record != null && predicate(item)).ToArray();
    }

    private static int GetAlchemyLuckyPowderCount(InventoryItem targetItem)
    {
        if (targetItem?.Record == null)
            return 0;

        var inventory = Game.Player?.Inventory;
        if (inventory == null)
            return 0;

        var powders = inventory
            .GetNormalPartItems(item =>
                item?.Record != null
                && item.Record.TypeID2 == 3
                && item.Record.TypeID3 == 10
                && item.Record.TypeID4 == 2
                && item.Record.ItemClass == targetItem.Record.Degree)
            .Sum(item => item.Amount);

        if (Game.ClientType >= GameClientType.Chinese && targetItem.Record.Degree >= 12)
        {
            powders += inventory
                .GetNormalPartItems(item =>
                    item?.Record != null
                    && item.Record.TypeID2 == 3
                    && item.Record.TypeID3 == 10
                    && item.Record.TypeID4 == 8
                    && item.Record.Param1 == targetItem.Record.ItemClass)
                .Sum(item => item.Amount);
        }

        return powders;
    }

    private static uint GetAlchemyMagicOptionValue(InventoryItem targetItem, string group)
    {
        if (targetItem?.Record == null || string.IsNullOrWhiteSpace(group))
            return 0;

        var option = targetItem.MagicOptions?.FirstOrDefault(m =>
        {
            var record = m?.Record ?? Game.ReferenceManager.GetMagicOption(m?.Id ?? 0);
            return record?.Group == group;
        });

        return option?.Value ?? 0;
    }

    private static ushort GetAlchemyMagicOptionMaxValue(InventoryItem targetItem, string group)
    {
        if (targetItem?.Record == null || string.IsNullOrWhiteSpace(group))
            return 0;

        var current = targetItem.MagicOptions?.FirstOrDefault(m =>
        {
            var record = m?.Record ?? Game.ReferenceManager.GetMagicOption(m?.Id ?? 0);
            return record?.Group == group;
        });

        if (current?.Record != null)
            return current.Record.GetMaxValue();

        var byDegree = Game.ReferenceManager.GetMagicOption(group, (byte)targetItem.Record.Degree);
        return byDegree?.GetMaxValue() ?? 0;
    }

    private static int GetAlchemyAttributePercentage(InventoryItem targetItem, ItemAttributeGroup group)
    {
        if (targetItem?.Record == null)
            return 0;

        var available = ItemAttributesInfo.GetAvailableAttributeGroupsForItem(targetItem.Record);
        if (available == null || !available.Contains(group))
            return 0;

        var slot = ItemAttributesInfo.GetAttributeSlotForItem(group, targetItem.Record);
        return targetItem.Attributes.GetPercentage(slot);
    }

    private static string NormalizeAlchemyMode(string mode)
    {
        var normalized = (mode ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "blues" => "blues",
            "stats" => "stats",
            _ => "enhance"
        };
    }

    private static string NormalizeAlchemyElixirType(string type)
    {
        var normalized = (type ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "shield" => "shield",
            "protector" => "protector",
            "accessory" => "accessory",
            _ => "weapon"
        };
    }

    private static string NormalizeAlchemyStatTarget(string target)
    {
        var normalized = (target ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "low" => "low",
            "medium" => "medium",
            "high" => "high",
            "max" => "max",
            _ => "off"
        };
    }

    private static int MapAlchemyStatTargetToPercent(string target)
    {
        return NormalizeAlchemyStatTarget(target) switch
        {
            "low" => 25,
            "medium" => 50,
            "high" => 75,
            "max" => 100,
            _ => 0
        };
    }

    private static string GetAlchemyBlueEnabledConfigKey(string key) => $"UBot.Desktop.Alchemy.Blue.{key}.Enabled";
    private static string GetAlchemyBlueMaxConfigKey(string key) => $"UBot.Desktop.Alchemy.Blue.{key}.Max";
    private static string GetAlchemyStatEnabledConfigKey(string key) => $"UBot.Desktop.Alchemy.Stat.{key}.Enabled";
    private static string GetAlchemyStatTargetConfigKey(string key) => $"UBot.Desktop.Alchemy.Stat.{key}.Target";

    private static string InferAlchemyElixirType(InventoryItem selectedItem)
    {
        var record = selectedItem?.Record;
        if (record == null)
            return "weapon";

        if (record.IsShield)
            return "shield";
        if (record.IsAccessory)
            return "accessory";
        if (record.IsArmor)
            return "protector";
        return "weapon";
    }

    private static object BuildQuestState()
    {
        var quests = Game.Player?.QuestLog?.ActiveQuests?.Values;
        var completed = Game.Player?.QuestLog?.CompletedQuests?.Length ?? 0;
        var activeRows = new List<Dictionary<string, object?>>();
        if (quests != null)
        {
            foreach (var quest in quests)
            {
                activeRows.Add(new Dictionary<string, object?>
                {
                    ["id"] = quest.Id,
                    ["name"] = quest.Quest?.GetTranslatedName() ?? quest.Quest?.CodeName ?? quest.Id.ToString(CultureInfo.InvariantCulture),
                    ["status"] = quest.Status.ToString(),
                    ["objectiveCount"] = quest.Objectives?.Length ?? 0,
                    ["remainingTime"] = quest.RemainingTime
                });
            }
        }

        return new Dictionary<string, object?>
        {
            ["activeCount"] = activeRows.Count,
            ["completedCount"] = completed,
            ["active"] = activeRows
                .OrderBy(quest => quest["name"]?.ToString() ?? string.Empty)
                .Take(200)
                .Cast<object?>()
                .ToList()
        };
    }

    private object BuildStatisticsState()
    {
        SpawnManager.TryGetEntities<SpawnedMonster>(out var monsters);
        var monsterCount = monsters?.Count() ?? 0;
        var inventoryCount = Game.Player?.Inventory?.GetNormalPartItems().Count ?? 0;

        return new Dictionary<string, object?>
        {
            ["status"] = _connectionService.CreateStatusSnapshot().StatusText,
            ["monsterCount"] = monsterCount,
            ["inventoryCount"] = inventoryCount,
            ["botRunning"] = Kernel.Bot != null && Kernel.Bot.Running,
            ["clientless"] = Game.Clientless
        };
    }

    private static object BuildTargetAssistState()
    {
        const int effectTransferParam = 1701213281;
        var bloodyStormCodeTokens = new[] { "FANSTORM", "FAN_STORM" };

        var enabled = PlayerConfig.Get("UBot.TargetAssist.Enabled", false);
        var maxRange = Math.Clamp(PlayerConfig.Get("UBot.TargetAssist.MaxRange", 40f), 5f, 400f);
        var includeDeadTargets = PlayerConfig.Get("UBot.TargetAssist.IncludeDeadTargets", false);
        var ignoreSnowShieldTargets = PlayerConfig.Get("UBot.TargetAssist.IgnoreSnowShieldTargets", true);
        var ignoreBloodyStormTargets = PlayerConfig.Get("UBot.TargetAssist.IgnoreBloodyStormTargets", false);
        var onlyCustomPlayers = PlayerConfig.Get("UBot.TargetAssist.OnlyCustomPlayers", false);

        var roleModeRaw = PlayerConfig.Get("UBot.TargetAssist.RoleMode", "Civil");
        var roleMode = roleModeRaw.Equals("Thief", StringComparison.OrdinalIgnoreCase)
            ? "thief"
            : roleModeRaw.Equals("HunterTrader", StringComparison.OrdinalIgnoreCase)
                ? "hunterTrader"
                : "civil";

        var ignoredGuilds = PlayerConfig.GetArray<string>("UBot.TargetAssist.IgnoredGuilds", '|')
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var ignoredGuildSet = new HashSet<string>(ignoredGuilds, StringComparer.OrdinalIgnoreCase);

        var customPlayers = PlayerConfig.GetArray<string>("UBot.TargetAssist.CustomPlayers", '|')
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var customPlayerSet = new HashSet<string>(customPlayers, StringComparer.OrdinalIgnoreCase);

        var candidateCount = 0;
        var nearestTargetName = string.Empty;
        var nearestTargetDistance = -1d;

        if (enabled
            && Game.Ready
            && Game.Player != null
            && Game.Player.State.LifeState == LifeState.Alive
            && SpawnManager.TryGetEntities<SpawnedPlayer>(out var players))
        {
            var candidates = players
                .Where(player => player != null && player.UniqueId != Game.Player.UniqueId)
                .Where(player => !string.IsNullOrWhiteSpace(player.Name))
                .Where(player => includeDeadTargets || player.State.LifeState == LifeState.Alive)
                .Where(player => player.DistanceToPlayer <= maxRange)
                .Where(player => !ignoreSnowShieldTargets || !HasSnowShieldBuff(player, effectTransferParam))
                .Where(player => !ignoreBloodyStormTargets || !HasAnyBuffCodeToken(player, bloodyStormCodeTokens))
                .Where(player => !IsIgnoredGuildName(player, ignoredGuildSet))
                .Where(player => !onlyCustomPlayers || customPlayerSet.Contains(player.Name.Trim()))
                .Where(player => MatchesTargetAssistRoleMode(player, roleMode))
                .OrderBy(player => player.DistanceToPlayer)
                .ToList();

            candidateCount = candidates.Count;
            if (candidateCount > 0)
            {
                nearestTargetName = candidates[0].Name;
                nearestTargetDistance = Math.Round(candidates[0].DistanceToPlayer, 1);
            }
        }

        return new Dictionary<string, object?>
        {
            ["enabled"] = enabled,
            ["maxRange"] = maxRange,
            ["includeDeadTargets"] = includeDeadTargets,
            ["ignoreSnowShieldTargets"] = ignoreSnowShieldTargets,
            ["ignoreBloodyStormTargets"] = ignoreBloodyStormTargets,
            ["onlyCustomPlayers"] = onlyCustomPlayers,
            ["roleMode"] = roleMode,
            ["targetCycleKey"] = PlayerConfig.Get("UBot.TargetAssist.TargetCycleKey", "Oem3"),
            ["ignoredGuilds"] = ignoredGuilds.Cast<object?>().ToList(),
            ["customPlayers"] = customPlayers.Cast<object?>().ToList(),
            ["candidateCount"] = candidateCount,
            ["nearestTargetName"] = nearestTargetName,
            ["nearestTargetDistance"] = nearestTargetDistance
        };
    }

    private static bool HasSnowShieldBuff(SpawnedPlayer player, int effectTransferParam)
    {
        if (player?.State?.ActiveBuffs == null)
            return false;

        foreach (var buff in player.State.ActiveBuffs)
        {
            var record = buff?.Record;
            if (record == null)
                continue;

            var code = record.Basic_Code;
            if (!string.IsNullOrWhiteSpace(code) && code.IndexOf("COLD_SHIELD", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (record.Params.Contains(effectTransferParam))
                return true;
        }

        return false;
    }

    private static bool HasAnyBuffCodeToken(SpawnedPlayer player, IEnumerable<string> tokens)
    {
        if (player?.State?.ActiveBuffs == null || tokens == null)
            return false;

        foreach (var buff in player.State.ActiveBuffs)
        {
            var code = buff?.Record?.Basic_Code;
            if (string.IsNullOrWhiteSpace(code))
                continue;

            foreach (var token in tokens)
            {
                if (string.IsNullOrWhiteSpace(token))
                    continue;

                if (code.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
        }

        return false;
    }

    private static bool IsIgnoredGuildName(SpawnedPlayer player, HashSet<string> ignoredGuildSet)
    {
        var guildName = player.Guild?.Name;
        if (string.IsNullOrWhiteSpace(guildName))
            return false;

        return ignoredGuildSet.Contains(guildName.Trim());
    }

    private static bool MatchesTargetAssistRoleMode(SpawnedPlayer player, string roleMode)
    {
        if (roleMode.Equals("thief", StringComparison.OrdinalIgnoreCase))
            return player.WearsJobSuite && (player.Job == JobType.Hunter || player.Job == JobType.Trade);

        if (roleMode.Equals("hunterTrader", StringComparison.OrdinalIgnoreCase))
            return player.WearsJobSuite && player.Job == JobType.Thief;

        return true;
    }

    private static Dictionary<string, object?> BuildSkillsPluginState()
    {
        return new Dictionary<string, object?>
        {
            ["timestamp"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["playerReady"] = Game.Player != null,
            ["skillCatalog"] = BuildSkillCatalog(),
            ["masteryCatalog"] = BuildMasteryCatalog(),
            ["activeBuffs"] = BuildActiveBuffSnapshot()
        };
    }

    private static List<Dictionary<string, object?>> BuildSkillCatalog()
    {
        var entries = new List<Dictionary<string, object?>>();
        foreach (var skill in CollectKnownAndAbilitySkills())
        {
            var record = skill.Record;
            if (record == null)
                continue;

            var name = record.GetRealName();
            if (string.IsNullOrWhiteSpace(name))
                name = record.Basic_Code;
            if (string.IsNullOrWhiteSpace(name))
                name = $"Skill {skill.Id}";

            var isPassive = skill.IsPassive;
            var isAttack = skill.IsAttack;
            var isImbue = skill.IsImbue;
            bool isLowLevel;
            try
            {
                isLowLevel = skill.IsLowLevel();
            }
            catch
            {
                isLowLevel = false;
            }

            entries.Add(new Dictionary<string, object?>
            {
                ["id"] = skill.Id,
                ["name"] = name,
                ["isPassive"] = isPassive,
                ["isAttack"] = isAttack,
                ["isBuff"] = !isPassive && !isAttack,
                ["isImbue"] = isImbue,
                ["isLowLevel"] = isLowLevel,
                ["icon"] = record.UI_IconFile
            });
        }

        return entries
            .OrderBy(row => row.TryGetValue("name", out var n) ? n?.ToString() : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.TryGetValue("id", out var id) && id is uint u ? u : 0)
            .ToList();
    }

    private static List<Dictionary<string, object?>> BuildMasteryCatalog()
    {
        var result = new List<Dictionary<string, object?>>();
        var masteries = Game.Player?.Skills?.Masteries;
        if (masteries == null)
            return result;

        foreach (var mastery in masteries)
        {
            var record = mastery.Record;
            if (record == null)
                continue;

            var name = record.Name;
            if (string.IsNullOrWhiteSpace(name))
                name = $"Mastery {mastery.Id}";

            result.Add(new Dictionary<string, object?>
            {
                ["id"] = mastery.Id,
                ["name"] = name,
                ["level"] = mastery.Level
            });
        }

        return result
            .OrderBy(row => row.TryGetValue("name", out var n) ? n?.ToString() : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<Dictionary<string, object?>> BuildActiveBuffSnapshot()
    {
        var result = new List<Dictionary<string, object?>>();
        var buffs = Game.Player?.State?.ActiveBuffs;
        if (buffs == null)
            return result;

        foreach (var buff in buffs)
        {
            var record = buff.Record;
            if (record == null)
                continue;

            var name = record.GetRealName();
            if (string.IsNullOrWhiteSpace(name))
                name = record.Basic_Code;
            if (string.IsNullOrWhiteSpace(name))
                name = $"Buff {buff.Id}";

            result.Add(new Dictionary<string, object?>
            {
                ["id"] = buff.Id,
                ["token"] = buff.Token,
                ["name"] = name,
                ["remainingMs"] = buff.RemainingMilliseconds,
                ["remainingPercent"] = Math.Round(buff.RemainingPercent * 100d, 2)
            });
        }

        return result
            .OrderBy(row => row.TryGetValue("name", out var n) ? n?.ToString() : string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<SkillInfo> CollectKnownAndAbilitySkills()
    {
        var result = new Dictionary<uint, SkillInfo>();
        var knownSkills = Game.Player?.Skills?.KnownSkills;
        if (knownSkills != null)
        {
            foreach (var known in knownSkills)
            {
                if (known?.Record == null || result.ContainsKey(known.Id))
                    continue;

                result[known.Id] = known;
            }
        }

        if (Game.Player != null && Game.Player.TryGetAbilitySkills(out var abilitySkills))
        {
            foreach (var ability in abilitySkills)
            {
                if (ability?.Record == null || result.ContainsKey(ability.Id))
                    continue;

                result[ability.Id] = ability;
            }
        }

        return result.Values.ToList();
    }
}


