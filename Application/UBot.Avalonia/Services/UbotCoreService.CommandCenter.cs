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

internal sealed class UbotCommandCenterService : UbotServiceBase
{
    private static readonly (string Name, string Label, string IconKey, string DefaultCommand)[] CommandCenterEmoteDefinitions =
    {
        ("emoticon.no", "No", "no", "stop"),
        ("emoticon.joy", "Joy", "joy", "none"),
        ("emoticon.rush", "Rush", "rush", "area"),
        ("emoticon.yes", "Yes", "yes", "start"),
        ("emoticon.greeting", "Greeting", "greeting", "area"),
        ("emoticon.smile", "Smile", "smile", "show"),
        ("emoticon.hi", "Hi", "hi", "none")
    };

    internal Dictionary<string, object?> BuildCommandCenterPluginConfig()
    {
        var config = LoadPluginJsonConfig(CommandCenterPluginName);
        var descriptions = CommandManager.GetCommandDescriptions() ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        config["enabled"] = PlayerConfig.Get("UBot.CommandCenter.Enabled", true);
        config["commandOptions"] = descriptions
            .Select(entry => new Dictionary<string, object?>
            {
                ["value"] = entry.Key,
                ["label"] = string.IsNullOrWhiteSpace(entry.Value) ? entry.Key : entry.Value
            })
            .OrderBy(option => string.Equals(option["value"]?.ToString(), "none", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(option => option["label"]?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Cast<object?>()
            .ToList();

        config["emotes"] = CommandCenterEmoteDefinitions
            .Select(definition =>
            {
                var mappedCommand = PlayerConfig.Get(
                    $"UBot.CommandCenter.MappedEmotes.{definition.Name}",
                    definition.DefaultCommand);

                return new Dictionary<string, object?>
                {
                    ["id"] = definition.Name,
                    ["name"] = definition.Name,
                    ["label"] = definition.Label,
                    ["iconKey"] = definition.IconKey,
                    ["command"] = NormalizeCommandCenterCommand(mappedCommand),
                    ["defaultCommand"] = definition.DefaultCommand
                };
            })
            .Cast<object?>()
            .ToList();

        config["chatCommands"] = new[]
        {
            BuildCommandCenterChatCommand("area", "Set the training area", descriptions),
            BuildCommandCenterChatCommand("buff", "Cast all buffs", descriptions),
            BuildCommandCenterChatCommand("show", "Show the bot window", descriptions),
            BuildCommandCenterChatCommand("start", "Start the bot", descriptions),
            BuildCommandCenterChatCommand("here", "Set training area and start bot", descriptions),
            BuildCommandCenterChatCommand("stop", "Stop the bot", descriptions)
        }.Cast<object?>().ToList();

        return config;
    }

    internal bool ApplyCommandCenterPluginPatch(Dictionary<string, object?> patch)
    {
        var changed = false;
        if (TryGetBoolValue(patch, "enabled", out var enabled))
        {
            PlayerConfig.Set("UBot.CommandCenter.Enabled", enabled);
            changed = true;
        }

        if (patch.TryGetValue("emotes", out var emotesRaw) && emotesRaw != null)
        {
            if (TryConvertObjectToDictionary(emotesRaw, out var emoteMap))
            {
                foreach (var entry in emoteMap)
                {
                    var emoteName = entry.Key?.Trim();
                    if (string.IsNullOrWhiteSpace(emoteName))
                        continue;

                    var command = NormalizeCommandCenterCommand(entry.Value?.ToString() ?? string.Empty);
                    PlayerConfig.Set($"UBot.CommandCenter.MappedEmotes.{emoteName}", command);
                    changed = true;
                }
            }
            else if (emotesRaw is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    if (!TryConvertObjectToDictionary(item, out var entry))
                        continue;

                    var emoteName = TryGetStringValue(entry, "id", out var id)
                        ? id
                        : TryGetStringValue(entry, "name", out var name)
                            ? name
                            : string.Empty;
                    if (string.IsNullOrWhiteSpace(emoteName))
                        continue;

                    var command = TryGetStringValue(entry, "command", out var mapped)
                        ? NormalizeCommandCenterCommand(mapped)
                        : "none";
                    PlayerConfig.Set($"UBot.CommandCenter.MappedEmotes.{emoteName.Trim()}", command);
                    changed = true;
                }
            }
        }

        if (changed)
            EventManager.FireEvent("OnSavePlayerConfig");

        return changed;
    }

    private static Dictionary<string, object?> BuildCommandCenterChatCommand(
        string commandName,
        string fallbackDescription,
        IReadOnlyDictionary<string, string> descriptions)
    {
        var description = descriptions.TryGetValue(commandName, out var dynamicDescription) && !string.IsNullOrWhiteSpace(dynamicDescription)
            ? dynamicDescription
            : fallbackDescription;

        return new Dictionary<string, object?>
        {
            ["trigger"] = $"\\{commandName}",
            ["command"] = commandName,
            ["description"] = description
        };
    }

    private static string NormalizeCommandCenterCommand(string? value)
    {
        var normalized = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(normalized) ? "none" : normalized;
    }
}

