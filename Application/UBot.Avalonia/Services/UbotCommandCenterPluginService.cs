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

internal sealed class UbotCommandCenterPluginService : UbotServiceBase
{
    private readonly UbotCommandCenterService _commandCenterService;

    internal UbotCommandCenterPluginService(UbotCommandCenterService commandCenterService)
    {
        _commandCenterService = commandCenterService;
    }

    internal Dictionary<string, object?> BuildConfig()
    {
        return _commandCenterService.BuildCommandCenterPluginConfig();
    }

    internal bool ApplyPatch(Dictionary<string, object?> patch)
    {
        return _commandCenterService.ApplyCommandCenterPluginPatch(patch);
    }
}

