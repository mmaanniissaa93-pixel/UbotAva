using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Network.Protocol;
using UBot.General.Models;

namespace UBot.General.Components;

public static class Accounts
{
    public static List<Account> SavedAccounts { get; set; }
    public static Account Joined { get; set; }

    private static string _filePath =>
        Path.Combine(ProfileManager.GetProfileDirectory(ProfileManager.SelectedProfile), "autologin.data");

    private static void EnsureDirectoryExists()
    {
        var directory = Path.GetDirectoryName(_filePath);
        Directory.CreateDirectory(directory);
    }

    public static void Load()
    {
        try
        {
            EnsureDirectoryExists();
            SavedAccounts = new List<Account>();

            if (!File.Exists(_filePath))
                return;

            var buffer = File.ReadAllBytes(_filePath);
            if (buffer.Length == 0)
                return;

            var blowfish = new Blowfish();
            buffer = blowfish.Decode(buffer);

            var serialized = Encoding.UTF8.GetString(buffer).Trim('\0');
            SavedAccounts = JsonSerializer.Deserialize<List<Account>>(serialized) ?? new List<Account>(4);
        }
        catch (Exception ex)
        {
            Log.NotifyLang("FileNotFound", _filePath);
            Log.Fatal(ex);
        }
    }

    public static void Save()
    {
        EnsureDirectoryExists();

        if (SavedAccounts == null)
            return;

        try
        {
            var buffer = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(SavedAccounts));

            var blowfish = new Blowfish();
            buffer = blowfish.Encode(buffer);

            File.WriteAllBytes(_filePath, buffer);
        }
        catch
        {
            Log.NotifyLang("FileNotFound", _filePath);
        }
    }
}
