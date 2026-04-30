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

internal sealed class UbotPluginStateAuxService : UbotServiceBase
{
    private readonly UbotConnectionService _connectionService;
    internal UbotPluginStateAuxService(UbotConnectionService connectionService)
    {
        _connectionService = connectionService;
    }
    private static object BuildQuestState()
    {
        var quests = UBot.Core.RuntimeAccess.Session.Player?.QuestLog?.ActiveQuests?.Values;
        var completed = UBot.Core.RuntimeAccess.Session.Player?.QuestLog?.CompletedQuests?.Length ?? 0;
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
        var inventoryCount = UBot.Core.RuntimeAccess.Session.Player?.Inventory?.GetNormalPartItems().Count ?? 0;

        return new Dictionary<string, object?>
        {
            ["status"] = _connectionService.CreateStatusSnapshot().StatusText,
            ["monsterCount"] = monsterCount,
            ["inventoryCount"] = inventoryCount,
            ["botRunning"] = UBot.Core.RuntimeAccess.Core.Bot != null && UBot.Core.RuntimeAccess.Core.Bot.Running,
            ["clientless"] = UBot.Core.RuntimeAccess.Session.Clientless
        };
    }

    internal object BuildQuestPluginState() => BuildQuestState();
    internal object BuildStatisticsPluginState() => BuildStatisticsState();
}

