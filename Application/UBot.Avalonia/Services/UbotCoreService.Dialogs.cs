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

namespace UBot.Avalonia.Services;

internal sealed class UbotDialogService : UbotServiceBase
{
    public Task<string> PickExecutableAsync()
    {
        using var dialog = new Forms.OpenFileDialog
        {
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            Title = "Select Silkroad executable"
        };

        var result = dialog.ShowDialog();
        return Task.FromResult(result == Forms.DialogResult.OK ? dialog.FileName : string.Empty);
    }

    public Task<string> PickSoundFileAsync()
    {
        using var dialog = new Forms.OpenFileDialog
        {
            Filter = "WAV files (*.wav)|*.wav|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            Title = "Select sound file"
        };

        var result = dialog.ShowDialog();
        return Task.FromResult(result == Forms.DialogResult.OK ? dialog.FileName : string.Empty);
    }

    public Task<string> PickScriptFileAsync()
    {
        using var dialog = new Forms.OpenFileDialog
        {
            Filter = "Script files (*.txt;*.script)|*.txt;*.script|All files (*.*)|*.*",
            CheckFileExists = true,
            Multiselect = false,
            Title = "Select script file"
        };

        var result = dialog.ShowDialog();
        return Task.FromResult(result == Forms.DialogResult.OK ? dialog.FileName : string.Empty);
    }
}

