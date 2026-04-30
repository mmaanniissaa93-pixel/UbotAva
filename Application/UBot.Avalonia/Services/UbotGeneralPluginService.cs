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

internal sealed class UbotGeneralPluginService : UbotServiceBase
{
    private readonly UbotAutoLoginService _autoLoginService;
    internal UbotGeneralPluginService(UbotAutoLoginService autoLoginService)
    {
        _autoLoginService = autoLoginService;
    }
    private Dictionary<string, object?> BuildGeneralPluginConfig()
    {
        var config = LoadPluginJsonConfig("UBot.General");
        var savedAccounts = _autoLoginService.GetAutoLoginAccountsAsync().GetAwaiter().GetResult();
        config["enableAutomatedLogin"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.EnableAutomatedLogin", false);
        config["autoLoginAccount"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.AutoLoginAccountUsername", string.Empty);
        config["selectedCharacter"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.AutoLoginCharacter", string.Empty);
        config["autoCharSelect"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.CharacterAutoSelect", false);
        config["enableLoginDelay"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.EnableLoginDelay", false);
        config["loginDelay"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.LoginDelay", 10);
        config["enableWaitAfterDc"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.EnableWaitAfterDC", false);
        config["waitAfterDc"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.WaitAfterDC", 3);
        config["enableStaticCaptcha"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.EnableStaticCaptcha", false);
        config["staticCaptcha"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.StaticCaptcha", string.Empty);
        config["autoStartBot"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.StartBot", false);
        config["useReturnScroll"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.UseReturnScroll", false);
        config["autoHideClient"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.HideOnStartClient", false);
        config["characterAutoSelectFirst"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.CharacterAutoSelectFirst", false);
        config["characterAutoSelectHigher"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.CharacterAutoSelectHigher", false);
        config["stayConnectedAfterClientExit"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.StayConnected", false);
        config["moveToTrayOnMinimize"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.TrayWhenMinimize", false);
        config["autoHidePendingWindow"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.AutoHidePendingWindow", false);
        config["enablePendingQueueLogs"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.PendingEnableQueueLogs", false);
        config["enableQueueNotification"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.EnableQueueNotification", false);
        config["queuePeopleLeft"] = UBot.Core.RuntimeAccess.Global.Get("UBot.General.QueueLeft", 30);
        config["sroExecutable"] = Path.Combine(
            UBot.Core.RuntimeAccess.Global.Get("UBot.SilkroadDirectory", string.Empty),
            UBot.Core.RuntimeAccess.Global.Get("UBot.SilkroadExecutable", string.Empty));

        var accounts = savedAccounts.Select(x => x.Username).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var characterMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var account in savedAccounts)
        {
            if (string.IsNullOrWhiteSpace(account.Username))
                continue;

            characterMap[account.Username] = account.Characters
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        var selectedAccount = UBot.Core.RuntimeAccess.Global.Get("UBot.General.AutoLoginAccountUsername", string.Empty);
        characterMap.TryGetValue(selectedAccount, out var selectedCharacters);
        var selectedCharacter = UBot.Core.RuntimeAccess.Global.Get("UBot.General.AutoLoginCharacter", string.Empty);

        if (string.IsNullOrWhiteSpace(selectedCharacter))
        {
            var preferredCharacter = savedAccounts
                .FirstOrDefault(x => string.Equals(x.Username, selectedAccount, StringComparison.OrdinalIgnoreCase))
                ?.SelectedCharacter;
            if (!string.IsNullOrWhiteSpace(preferredCharacter))
                selectedCharacter = preferredCharacter;
        }

        config["autoLoginAccounts"] = accounts;
        config["autoLoginCharacters"] = selectedCharacters ?? new List<string>();
        config["autoLoginCharacterMap"] = characterMap;
        config["selectedCharacter"] = selectedCharacter;
        return config;
    }

    private bool ApplyGeneralPluginPatch(Dictionary<string, object?> patch)
    {
        var changed = false;
        var selectedCharacterPatched = false;
        string? selectedCharacterValue = null;
        string? selectedAccountValue = null;
        foreach (var kv in patch)
        {
            switch (kv.Key)
            {
                case "sroExecutable":
                    if (kv.Value is string sroPath && !string.IsNullOrWhiteSpace(sroPath))
                    {
                        var path = sroPath.Trim().Trim('"');
                        if (File.Exists(path))
                        {
                            UBot.Core.RuntimeAccess.Global.Set("UBot.SilkroadDirectory", Path.GetDirectoryName(path) ?? string.Empty);
                            UBot.Core.RuntimeAccess.Global.Set("UBot.SilkroadExecutable", Path.GetFileName(path));
                            changed = true;
                        }
                    }
                    break;
                case "enableAutomatedLogin":
                    changed |= SetGlobalBool("UBot.General.EnableAutomatedLogin", kv.Value);
                    break;
                case "autoLoginAccount":
                    changed |= SetGlobalString("UBot.General.AutoLoginAccountUsername", kv.Value);
                    selectedAccountValue = kv.Value?.ToString()?.Trim();
                    break;
                case "selectedCharacter":
                    changed |= SetGlobalString("UBot.General.AutoLoginCharacter", kv.Value);
                    selectedCharacterPatched = true;
                    selectedCharacterValue = kv.Value?.ToString()?.Trim() ?? string.Empty;
                    break;
                case "autoCharSelect":
                    changed |= SetGlobalBool("UBot.General.CharacterAutoSelect", kv.Value);
                    break;
                case "enableLoginDelay":
                    changed |= SetGlobalBool("UBot.General.EnableLoginDelay", kv.Value);
                    break;
                case "loginDelay":
                    changed |= SetGlobalInt("UBot.General.LoginDelay", kv.Value, 0, 3600);
                    break;
                case "enableWaitAfterDc":
                    changed |= SetGlobalBool("UBot.General.EnableWaitAfterDC", kv.Value);
                    break;
                case "waitAfterDc":
                    changed |= SetGlobalInt("UBot.General.WaitAfterDC", kv.Value, 0, 3600);
                    break;
                case "enableStaticCaptcha":
                    changed |= SetGlobalBool("UBot.General.EnableStaticCaptcha", kv.Value);
                    break;
                case "staticCaptcha":
                    changed |= SetGlobalString("UBot.General.StaticCaptcha", kv.Value);
                    break;
                case "autoStartBot":
                    changed |= SetGlobalBool("UBot.General.StartBot", kv.Value);
                    break;
                case "useReturnScroll":
                    changed |= SetGlobalBool("UBot.General.UseReturnScroll", kv.Value);
                    break;
                case "autoHideClient":
                    changed |= SetGlobalBool("UBot.General.HideOnStartClient", kv.Value);
                    break;
                case "characterAutoSelectFirst":
                    changed |= SetGlobalBool("UBot.General.CharacterAutoSelectFirst", kv.Value);
                    break;
                case "characterAutoSelectHigher":
                    changed |= SetGlobalBool("UBot.General.CharacterAutoSelectHigher", kv.Value);
                    break;
                case "stayConnectedAfterClientExit":
                    changed |= SetGlobalBool("UBot.General.StayConnected", kv.Value);
                    break;
                case "moveToTrayOnMinimize":
                    changed |= SetGlobalBool("UBot.General.TrayWhenMinimize", kv.Value);
                    break;
                case "autoHidePendingWindow":
                    changed |= SetGlobalBool("UBot.General.AutoHidePendingWindow", kv.Value);
                    break;
                case "enablePendingQueueLogs":
                    changed |= SetGlobalBool("UBot.General.PendingEnableQueueLogs", kv.Value);
                    break;
                case "enableQueueNotification":
                    changed |= SetGlobalBool("UBot.General.EnableQueueNotification", kv.Value);
                    break;
                case "queuePeopleLeft":
                    changed |= SetGlobalInt("UBot.General.QueueLeft", kv.Value, 0, 999);
                    break;
                default:
                    break;
            }
        }

        if (selectedCharacterPatched)
        {
            if (string.IsNullOrWhiteSpace(selectedAccountValue))
                selectedAccountValue = UBot.Core.RuntimeAccess.Global.Get("UBot.General.AutoLoginAccountUsername", string.Empty);

            changed |= _autoLoginService.UpdateSelectedCharacterForAccount(selectedAccountValue, selectedCharacterValue);
        }

        return changed;
    }

    internal Dictionary<string, object?> BuildConfig() => BuildGeneralPluginConfig();
    internal bool ApplyPatch(Dictionary<string, object?> patch) => ApplyGeneralPluginPatch(patch);
}

