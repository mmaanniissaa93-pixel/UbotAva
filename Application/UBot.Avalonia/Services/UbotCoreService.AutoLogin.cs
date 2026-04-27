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

internal sealed class UbotAutoLoginService : UbotServiceBase
{
    private static readonly JsonSerializerOptions AutoLoginReadOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public Task<IReadOnlyList<AutoLoginAccountDto>> GetAutoLoginAccountsAsync()
    {
        var accounts = LoadAutoLoginAccountsFromFile()
            .OrderBy(x => x.Username, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return Task.FromResult((IReadOnlyList<AutoLoginAccountDto>)accounts);
    }

    public Task<bool> SaveAutoLoginAccountsAsync(IReadOnlyList<AutoLoginAccountDto> accounts)
    {
        try
        {
            var sanitized = (accounts ?? Array.Empty<AutoLoginAccountDto>())
                .Where(x => !string.IsNullOrWhiteSpace(x.Username))
                .GroupBy(x => x.Username.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    var item = group.Last();
                    return new AutoLoginAccountDto
                    {
                        Username = item.Username.Trim(),
                        Password = item.Password ?? string.Empty,
                        SecondaryPassword = item.SecondaryPassword ?? string.Empty,
                        Channel = item.Channel == 0 ? (byte)1 : item.Channel,
                        Type = string.IsNullOrWhiteSpace(item.Type) ? "Joymax" : item.Type,
                        ServerName = item.ServerName?.Trim() ?? string.Empty,
                        SelectedCharacter = item.SelectedCharacter?.Trim() ?? string.Empty,
                        Characters = (item.Characters ?? new List<string>())
                            .Where(name => !string.IsNullOrWhiteSpace(name))
                            .Select(name => name.Trim())
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList()
                    };
                })
                .OrderBy(x => x.Username, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!WriteAutoLoginAccountsToFile(sanitized))
                return Task.FromResult(false);

            ReloadGeneralAccountsRuntime();
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }


    private static List<AutoLoginAccountDto> LoadAutoLoginAccountsFromFile()
    {
        try
        {
            var filePath = GetAutoLoginDataFilePath();
            if (!File.Exists(filePath))
                return new List<AutoLoginAccountDto>();

            var encoded = File.ReadAllBytes(filePath);
            if (encoded.Length == 0)
                return new List<AutoLoginAccountDto>();

            var blowfish = new Blowfish();
            var decoded = blowfish.Decode(encoded);
            if (decoded == null || decoded.Length == 0)
                return new List<AutoLoginAccountDto>();

            var json = Encoding.UTF8.GetString(decoded).Trim('\0').Trim();
            if (string.IsNullOrWhiteSpace(json))
                return new List<AutoLoginAccountDto>();

            var accounts = JsonSerializer.Deserialize<List<AutoLoginAccountDto>>(json, AutoLoginReadOptions);
            if (accounts == null)
                return new List<AutoLoginAccountDto>();

            foreach (var account in accounts)
            {
                account.Username = account.Username?.Trim() ?? string.Empty;
                account.Password ??= string.Empty;
                account.SecondaryPassword ??= string.Empty;
                account.Type = string.IsNullOrWhiteSpace(account.Type) ? "Joymax" : account.Type;
                account.ServerName = account.ServerName?.Trim() ?? string.Empty;
                account.SelectedCharacter = account.SelectedCharacter?.Trim() ?? string.Empty;
                account.Characters = (account.Characters ?? new List<string>())
                    .Where(name => !string.IsNullOrWhiteSpace(name))
                    .Select(name => name.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();
                if (account.Channel == 0)
                    account.Channel = 1;
            }

            var result = accounts.Where(x => !string.IsNullOrWhiteSpace(x.Username)).ToList();

            // Compatibility migration:
            // Earlier Avalonia builds wrote camelCase keys (username/serverName/...).
            // UBot.General deserializes Account with PascalCase property names, so auto-login
            // can silently fail if file shape is not normalized.
            if (RequiresLegacyAutoLoginMigration(json))
            {
                if (WriteAutoLoginAccountsToFile(result))
                    ReloadGeneralAccountsRuntime();
            }

            return result;
        }
        catch
        {
            return new List<AutoLoginAccountDto>();
        }
    }

    private static bool WriteAutoLoginAccountsToFile(IReadOnlyList<AutoLoginAccountDto> accounts)
    {
        try
        {
            var json = JsonSerializer.Serialize(accounts ?? Array.Empty<AutoLoginAccountDto>());
            var data = Encoding.UTF8.GetBytes(json);
            var blowfish = new Blowfish();
            var encoded = blowfish.Encode(data);
            if (encoded == null)
                return false;

            var filePath = GetAutoLoginDataFilePath();
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            File.WriteAllBytes(filePath, encoded);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool RequiresLegacyAutoLoginMigration(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;

        return json.Contains("\"username\"", StringComparison.Ordinal)
               || json.Contains("\"password\"", StringComparison.Ordinal)
               || json.Contains("\"secondaryPassword\"", StringComparison.Ordinal)
               || json.Contains("\"serverName\"", StringComparison.Ordinal)
               || json.Contains("\"selectedCharacter\"", StringComparison.Ordinal)
               || json.Contains("\"characters\"", StringComparison.Ordinal);
    }

    internal bool UpdateSelectedCharacterForAccount(string? username, string? selectedCharacter)
    {
        if (string.IsNullOrWhiteSpace(username))
            return false;

        var accounts = LoadAutoLoginAccountsFromFile();
        if (accounts.Count == 0)
            return false;

        var account = accounts.FirstOrDefault(x =>
            string.Equals(x.Username, username.Trim(), StringComparison.OrdinalIgnoreCase));
        if (account == null)
            return false;

        var normalizedCharacter = selectedCharacter?.Trim() ?? string.Empty;
        if (string.Equals(account.SelectedCharacter ?? string.Empty, normalizedCharacter, StringComparison.Ordinal))
            return false;

        account.SelectedCharacter = normalizedCharacter;
        if (!WriteAutoLoginAccountsToFile(accounts))
            return false;

        ReloadGeneralAccountsRuntime();
        return true;
    }

    private static void ReloadGeneralAccountsRuntime()
    {
        try
        {
            var accountsType = Type.GetType("UBot.General.Components.Accounts, UBot.General", false);
            var loadMethod = accountsType?.GetMethod("Load", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            loadMethod?.Invoke(null, null);
        }
        catch
        {
            // ignored
        }
    }

    private static string GetAutoLoginDataFilePath()
    {
        return Path.Combine(ProfileManager.GetProfileDirectory(ProfileManager.SelectedProfile), "autologin.data");
    }
}

