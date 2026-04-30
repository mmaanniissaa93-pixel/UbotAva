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
            UBot.Core.RuntimeAccess.Player.Get("UBot.Desktop.Map.ShowFilter",
                UBot.Core.RuntimeAccess.Player.Get("UBot.Desktop.Map.EntityFilter", "All")));

        var collisionDetection = UBot.Core.RuntimeAccess.Global.Get("UBot.EnableCollisionDetection",
            UBot.Core.RuntimeAccess.Player.Get("UBot.Desktop.Map.CollisionDetection", false));
        var autoSelectUniques = UBot.Core.RuntimeAccess.Player.Get("UBot.Map.AutoSelectUnique",
            UBot.Core.RuntimeAccess.Player.Get("UBot.Desktop.Map.AutoSelectUniques", false));

        config["showFilter"] = showFilter;
        config["entityFilter"] = showFilter;
        config["collisionDetection"] = collisionDetection;
        config["autoSelectUniques"] = autoSelectUniques;
        config["autoSelectUnique"] = autoSelectUniques;
        config["resetToPlayerAt"] = UBot.Core.RuntimeAccess.Player.Get("UBot.Desktop.Map.ResetToPlayerAt", 0L);
        return config;
    }

    private static bool ApplyMapPluginPatch(Dictionary<string, object?> patch)
    {
        var changed = false;
        if (TryGetStringValue(patch, "showFilter", out var showFilter) || TryGetStringValue(patch, "entityFilter", out showFilter))
        {
            var normalized = NormalizeMapShowFilterValue(showFilter);
            UBot.Core.RuntimeAccess.Player.Set("UBot.Desktop.Map.ShowFilter", normalized);
            UBot.Core.RuntimeAccess.Player.Set("UBot.Desktop.Map.EntityFilter", normalized);
            changed = true;
        }

        if (TryGetBoolValue(patch, "collisionDetection", out var collision))
        {
            UBot.Core.RuntimeAccess.Global.Set("UBot.EnableCollisionDetection", collision);
            UBot.Core.RuntimeAccess.Player.Set("UBot.Desktop.Map.CollisionDetection", collision);
            changed = true;
        }

        if (TryGetBoolValue(patch, "autoSelectUniques", out var autoSelect) || TryGetBoolValue(patch, "autoSelectUnique", out autoSelect))
        {
            UBot.Core.RuntimeAccess.Player.Set("UBot.Map.AutoSelectUnique", autoSelect);
            UBot.Core.RuntimeAccess.Player.Set("UBot.Desktop.Map.AutoSelectUniques", autoSelect);
            changed = true;
        }

        if (changed)
            UBot.Core.RuntimeAccess.Events.FireEvent("OnSavePlayerConfig");
        return changed;
    }

    internal Dictionary<string, object?> BuildConfig() => BuildMapPluginConfig();
    internal bool ApplyPatch(Dictionary<string, object?> patch) => ApplyMapPluginPatch(patch);
}

