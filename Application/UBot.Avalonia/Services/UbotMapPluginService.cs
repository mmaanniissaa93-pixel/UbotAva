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

internal sealed class UbotMapPluginService : UbotServiceBase
{
    private static Dictionary<string, object?> BuildMapPluginConfig()
    {
        var config = LoadPluginJsonConfig(MapPluginName);
        var showFilter = NormalizeMapShowFilterValue(
            PlayerConfig.Get("UBot.Desktop.Map.ShowFilter",
                PlayerConfig.Get("UBot.Desktop.Map.EntityFilter", "All")));

        var collisionDetection = GlobalConfig.Get("UBot.EnableCollisionDetection",
            PlayerConfig.Get("UBot.Desktop.Map.CollisionDetection", false));
        var autoSelectUniques = PlayerConfig.Get("UBot.Map.AutoSelectUnique",
            PlayerConfig.Get("UBot.Desktop.Map.AutoSelectUniques", false));

        config["showFilter"] = showFilter;
        config["entityFilter"] = showFilter;
        config["collisionDetection"] = collisionDetection;
        config["autoSelectUniques"] = autoSelectUniques;
        config["autoSelectUnique"] = autoSelectUniques;
        config["resetToPlayerAt"] = PlayerConfig.Get("UBot.Desktop.Map.ResetToPlayerAt", 0L);
        return config;
    }

    private static bool ApplyMapPluginPatch(Dictionary<string, object?> patch)
    {
        var changed = false;
        if (TryGetStringValue(patch, "showFilter", out var showFilter) || TryGetStringValue(patch, "entityFilter", out showFilter))
        {
            var normalized = NormalizeMapShowFilterValue(showFilter);
            PlayerConfig.Set("UBot.Desktop.Map.ShowFilter", normalized);
            PlayerConfig.Set("UBot.Desktop.Map.EntityFilter", normalized);
            changed = true;
        }

        if (TryGetBoolValue(patch, "collisionDetection", out var collision))
        {
            GlobalConfig.Set("UBot.EnableCollisionDetection", collision);
            PlayerConfig.Set("UBot.Desktop.Map.CollisionDetection", collision);
            changed = true;
        }

        if (TryGetBoolValue(patch, "autoSelectUniques", out var autoSelect) || TryGetBoolValue(patch, "autoSelectUnique", out autoSelect))
        {
            PlayerConfig.Set("UBot.Map.AutoSelectUnique", autoSelect);
            PlayerConfig.Set("UBot.Desktop.Map.AutoSelectUniques", autoSelect);
            changed = true;
        }

        if (changed)
            EventManager.FireEvent("OnSavePlayerConfig");
        return changed;
    }

    internal Dictionary<string, object?> BuildConfig() => BuildMapPluginConfig();
    internal bool ApplyPatch(Dictionary<string, object?> patch) => ApplyMapPluginPatch(patch);
}

