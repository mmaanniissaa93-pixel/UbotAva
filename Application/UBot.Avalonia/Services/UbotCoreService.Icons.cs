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

internal sealed class UbotIconService : UbotServiceBase
{
    // Mapping of emoticon names to their icon paths (local to this project to avoid internals visibility issues)
    private static readonly Dictionary<string, string> EmoteIconPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["emoticon.hi"] = "icon\\action\\emot_act_greeting.ddj",
        ["emoticon.smile"] = "icon\\action\\emot_act_laugh.ddj",
        ["emoticon.greeting"] = "icon\\action\\emot_act_pokun.ddj",
        ["emoticon.yes"] = "icon\\action\\emot_act_yes.ddj",
        ["emoticon.rush"] = "icon\\action\\emot_act_rush.ddj",
        ["emoticon.joy"] = "icon\\action\\emot_act_joy.ddj",
        ["emoticon.no"] = "icon\\action\\emot_act_no.ddj"
    };


    public async Task<byte[]?> GetSkillIconAsync(string iconFile)
    {
        if (string.IsNullOrWhiteSpace(iconFile)) return null;

        try
        {
            // Normalize path
            var cleanPath = iconFile.Replace("/", "\\").TrimStart('\\');
            if (!cleanPath.StartsWith("icon\\", StringComparison.OrdinalIgnoreCase))
                cleanPath = Path.Combine("icon", cleanPath);

            // Avoid double icon\icon\
            if (cleanPath.StartsWith("icon\\icon\\", StringComparison.OrdinalIgnoreCase))
                cleanPath = cleanPath.Substring(5);

            if (Game.MediaPk2 == null) return null;

            if (!Game.MediaPk2.TryGetFile(cleanPath, out var file))
            {
                // Try just the filename in icon folder as fallback
                var fileName = Path.GetFileName(cleanPath);
                var fallbackPath = Path.Combine("icon", fileName);
                if (!Game.MediaPk2.TryGetFile(fallbackPath, out file))
                {
                    // Last resort: default icon
                    if (!Game.MediaPk2.TryGetFile("icon\\icon_default.ddj", out file))
                        return null;
                }
            }

            using var drawingImage = file.ToImage();
            if (drawingImage == null || (drawingImage.Width <= 16 && drawingImage.Height <= 16)) 
                return null; // Don't return the 16x16 placeholder from Pk2Extensions

            using var ms = new MemoryStream();
            drawingImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return ms.ToArray();
        }
        catch
        {
            return null;
        }
    }

    // Emote icon retrieval (simple internal mapping to avoid cross-assembly internals)
    public Task<byte[]?> GetEmoteIconAsync(string emoteName)
    {
        if (string.IsNullOrWhiteSpace(emoteName))
            return Task.FromResult<byte[]>(null);

        if (!EmoteIconPaths.TryGetValue(emoteName, out var iconPath))
            return Task.FromResult<byte[]>(null);

        try
        {
            if (Game.MediaPk2 == null) return Task.FromResult<byte[]>(null);
            if (!Game.MediaPk2.TryGetFile(iconPath, out var file))
            {
                // Fallback: try as-is added by default
                if (!Game.MediaPk2.TryGetFile(iconPath, out file))
                    return Task.FromResult<byte[]>(null);
            }

            using var drawingImage = file.ToImage();
            if (drawingImage == null)
                return Task.FromResult<byte[]>(null);

            using var ms = new MemoryStream();
            drawingImage.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            return Task.FromResult<byte[]>(ms.ToArray());
        }
        catch
        {
            return Task.FromResult<byte[]>(null);
        }
    }
}

