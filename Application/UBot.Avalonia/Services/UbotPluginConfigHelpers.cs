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

internal sealed class UbotPluginConfigHelpers : UbotServiceBase
{
    internal static Dictionary<string, object?> LoadRawConfig(string pluginId)
    {
        return LoadPluginJsonConfig(pluginId);
    }

    internal static bool ApplyGenericPatch(string pluginId, Dictionary<string, object?> patch)
    {
        var current = LoadPluginJsonConfig(pluginId);
        foreach (var kv in patch)
            current[kv.Key] = kv.Value;

        SavePluginJsonConfig(pluginId, current);
        return true;
    }

    internal static bool SetGlobalBool(string key, object? value)
    {
        if (!TryConvertBool(value, out var parsed))
            return false;
        GlobalConfig.Set(key, parsed);
        return true;
    }

    internal static bool SetGlobalInt(string key, object? value, int min, int max)
    {
        if (!TryConvertInt(value, out var parsed))
            return false;
        GlobalConfig.Set(key, Math.Clamp(parsed, min, max));
        return true;
    }

    internal static bool SetGlobalString(string key, object? value)
    {
        if (value == null)
            return false;
        GlobalConfig.Set(key, value.ToString() ?? string.Empty);
        return true;
    }

    internal static bool SetPlayerBool(string targetKey, IDictionary<string, object?> patch, string patchKey)
    {
        if (!patch.TryGetValue(patchKey, out var value) || !TryConvertBool(value, out var parsed))
            return false;
        PlayerConfig.Set(targetKey, parsed);
        return true;
    }

    internal static bool SetPlayerInt(string targetKey, IDictionary<string, object?> patch, string patchKey, int min, int max)
    {
        if (!patch.TryGetValue(patchKey, out var value) || !TryConvertInt(value, out var parsed))
            return false;
        PlayerConfig.Set(targetKey, Math.Clamp(parsed, min, max));
        return true;
    }

    internal static bool SetPlayerFloat(string targetKey, IDictionary<string, object?> patch, string patchKey)
    {
        if (!patch.TryGetValue(patchKey, out var value) || !TryConvertDouble(value, out var parsed))
            return false;
        PlayerConfig.Set(targetKey, (float)parsed);
        return true;
    }

    internal static bool SetPlayerString(string targetKey, IDictionary<string, object?> patch, string patchKey)
    {
        if (!patch.TryGetValue(patchKey, out var value))
            return false;
        PlayerConfig.Set(targetKey, value?.ToString() ?? string.Empty);
        return true;
    }
}

