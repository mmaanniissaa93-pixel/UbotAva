using System.Collections.Generic;
using System.IO;
using UBot.Core.Abstractions.Services;

namespace UBot.Core.IO;

internal sealed class ProfileFileStorage : IProfileStorage
{
    private readonly IAppPaths _paths;

    public ProfileFileStorage(IAppPaths paths)
    {
        _paths = paths;
    }

    public string[] LoadProfiles(string character)
    {
        return GetConfig(character).GetArray<string>("UBot.Profiles", '|');
    }

    public void SaveProfiles(string character, IEnumerable<string> profiles)
    {
        var config = GetConfig(character);
        config.SetArray("UBot.Profiles", profiles, "|");
        config.Save();
    }

    public string GetSelectedProfile(string character)
    {
        return GetConfig(character).Get("UBot.SelectedProfile", "Default");
    }

    public void SaveSelectedProfile(string character, string profile)
    {
        var config = GetConfig(character);
        config.Set("UBot.SelectedProfile", profile);
        config.Save();
    }

    public bool GetShowProfileDialog(string character)
    {
        return GetConfig(character).Get("UBot.ShowProfileDialog", false);
    }

    public void SaveShowProfileDialog(string character, bool show)
    {
        var config = GetConfig(character);
        config.Set("UBot.ShowProfileDialog", show);
        config.Save();
    }

    public void EnsureProfileDirectory(string profileName)
    {
        var directory = GetProfileDirectory(profileName);
        if (!Directory.Exists(directory))
            Directory.CreateDirectory(directory);
    }

    public void CopyProfileFile(string sourceProfile, string targetProfile)
    {
        var source = GetProfileFile(sourceProfile);
        var target = GetProfileFile(targetProfile);

        if (File.Exists(source))
            File.Copy(source, target, true);
    }

    public void CopyCharacterProfileFile(string character, string sourceProfile, string targetProfile)
    {
        if (string.IsNullOrWhiteSpace(character))
            return;

        var charDir = GetCharacterDirectory(character);
        if (!Directory.Exists(charDir))
            Directory.CreateDirectory(charDir);

        var source = Path.Combine(charDir, $"{sourceProfile}.rs");
        var target = Path.Combine(charDir, $"{targetProfile}.rs");

        if (File.Exists(source))
            File.Copy(source, target, true);
    }

    public void CopyAutoLoginFile(string sourceProfile, string targetProfile)
    {
        var source = Path.Combine(GetProfileDirectory(sourceProfile), "autologin.data");
        var targetDirectory = GetProfileDirectory(targetProfile);
        var target = Path.Combine(targetDirectory, "autologin.data");

        if (!Directory.Exists(targetDirectory))
            Directory.CreateDirectory(targetDirectory);

        if (File.Exists(source))
            File.Copy(source, target, true);
    }

    public void DeleteProfile(string profileName)
    {
        var profileFile = GetProfileFile(profileName);
        if (File.Exists(profileFile))
            File.Delete(profileFile);
    }

    public string GetProfileConfigFileName(string character)
    {
        if (!string.IsNullOrWhiteSpace(character))
        {
            var charDir = GetCharacterDirectory(character);
            if (!Directory.Exists(charDir))
                Directory.CreateDirectory(charDir);

            return Path.Combine(charDir, "Profiles.rs");
        }

        return Path.Combine(UserDirectory, "Profiles.rs");
    }

    public string GetProfileFile(string profileName)
    {
        return Path.Combine(UserDirectory, $"{profileName}.rs");
    }

    public string GetProfileDirectory(string profileName)
    {
        return Path.Combine(UserDirectory, profileName);
    }

    private Config GetConfig(string character)
    {
        return new Config(GetProfileConfigFileName(character));
    }

    private string GetCharacterDirectory(string character)
    {
        return Path.Combine(UserDirectory, character);
    }

    private string UserDirectory => Path.Combine(_paths.BasePath, "User");
}
