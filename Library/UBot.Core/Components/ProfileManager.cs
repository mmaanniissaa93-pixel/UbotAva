#nullable enable annotations

using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;

namespace UBot.Core.Components;

public class ProfileManager
{
    /// <summary>
    ///     The profile config
    /// </summary>
    private static Config _config;

    /// <summary>
    ///     Get active profiles
    /// </summary>
    private static readonly ObservableCollection<string> _profiles;

    /// <summary>
    ///     Initialize static ctor
    /// </summary>
    static ProfileManager()
    {
        _profiles = new ObservableCollection<string>();
        _profiles.CollectionChanged += Profiles_CollectionChanged;
        
        // Initial load
        var configPath = GetProfileConfigFileName();
        _config = new Config(configPath);
        LoadProfiles();
    }

    private static void LoadProfiles()
    {
        _profiles.Clear();
        var profiles = _config.GetArray<string>("UBot.Profiles", '|');
        foreach (var p in profiles)
            _profiles.Add(p);

        if (!_profiles.Any())
            _profiles.Add("Default");
    }

    /// <summary>
    ///     Get active profiles
    /// </summary>
    public static string[] Profiles => _profiles.ToArray();

    /// <summary>
    ///     If the selected profile loaded via program args <c>true</c>; otherwise <c>false</c>.
    /// </summary>
    public static bool IsProfileLoadedByArgs { get; set; }

    /// <summary>
    ///     The selected character
    /// </summary>
    private static string _selectedCharacter = string.Empty;
    public static string SelectedCharacter
    {
        get => _selectedCharacter;
        set
        {
            if (_selectedCharacter == value) return;
            _selectedCharacter = value;
            _config = new Config(GetProfileConfigFileName());
            LoadProfiles();
        }
    }

    /// <summary>
    ///     The selected profile
    /// </summary>
    public static string SelectedProfile => _config.Get("UBot.SelectedProfile", "Default");

    /// <summary>
    ///     Show the profile dialog <c>true</c>; otherwise <c>false</c>
    /// </summary>
    public static bool ShowProfileDialog
    {
        get => _config.Get("UBot.ShowProfileDialog", false);
        set
        {
            _config.Set("UBot.ShowProfileDialog", value);
            _config.Save();
        }
    }

    /// <summary>
    ///     There have any value in the collection <c>true</c>; otherwise <c>false</c>
    /// </summary>
    public static bool Any()
    {
        return _profiles.Any();
    }

    /// <summary>
    ///     Called after Profiles are changed
    /// </summary>
    private static void Profiles_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _config.SetArray("UBot.Profiles", _profiles, "|");
        _config.Save();
    }

    /// <summary>
    ///     Set selected profile
    /// </summary>
    /// <param name="profile">The profile</param>
    public static bool SetSelectedProfile(string profile)
    {
        if (!_profiles.Any(p => p == profile))
            return false;

        _config.Set("UBot.SelectedProfile", profile);
        _config.Save();

        return true;
    }

    /// <summary>
    ///     Is profile exists <c>true</c>; otherwise <c>false</c>
    /// </summary>
    /// <param name="profile">The profile</param>
    public static bool ProfileExists(string profile)
    {
        return _profiles.Any(p => p.Equals(profile, StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    ///     Create new profile
    /// </summary>
    /// <param name="profile">The profile</param>
    /// <param name="useAsBase">Use as base <c>true</c>; otherwise <c>false</c></param>
    /// <returns>Is created <c>true</c>; otherwise <c>false</c></returns>
    public static bool Add(string profile, bool useAsBase = false)
    {
        if (profile.Equals("Profiles", StringComparison.InvariantCultureIgnoreCase))
            return false;

        var existingProfile = _profiles.FirstOrDefault(p =>
            p.Equals(profile, StringComparison.InvariantCultureIgnoreCase)
        );
        if (!string.IsNullOrEmpty(existingProfile))
        {
            SetSelectedProfile(existingProfile);
            return true;
        }

        if (profile == SelectedProfile)
            return true;

        _profiles.Add(profile);

        if (useAsBase)
            CopyOldProfileData(profile);

        var newProfileDirectory = GetProfileDirectory(profile);
        if (!Directory.Exists(newProfileDirectory))
            Directory.CreateDirectory(newProfileDirectory);

        SetSelectedProfile(profile);

        return true;
    }

    /// <summary>
    ///     Remove the profile
    /// </summary>
    /// <param name="profile">The profile</param>
    /// <returns>Is removed <c>true</c>; otherwise <c>false</c></returns>
    public static bool Remove(string profile)
    {
        return _profiles.Remove(profile);
    }

    /// <summary>
    ///     Copies the old profile data to the new profile.
    /// </summary>
    /// <param name="profile">Name of the profile.</param>
    private static void CopyOldProfileData(string profile)
    {
        try
        {
            var oldProfileFilePath = GetProfileFile(SelectedProfile);
            var newProfileFilePath = GetProfileFile(profile);
            
            if (File.Exists(oldProfileFilePath))
                File.Copy(oldProfileFilePath, newProfileFilePath);

            // Copy Character specific profile (PlayerConfig) if character is selected
            if (!string.IsNullOrWhiteSpace(SelectedCharacter))
            {
                var charDir = Path.Combine(Kernel.BasePath, "User", SelectedCharacter);
                var oldPlayerConfig = Path.Combine(charDir, $"{SelectedProfile}.rs");
                var newPlayerConfig = Path.Combine(charDir, $"{profile}.rs");

                if (!Directory.Exists(charDir))
                    Directory.CreateDirectory(charDir);

                if (File.Exists(oldPlayerConfig))
                    File.Copy(oldPlayerConfig, newPlayerConfig);
            }

            var oldAutoLoginFile = Path.Combine(GetProfileDirectory(SelectedProfile), "autologin.data");
            var newAutoLoginFile = Path.Combine(GetProfileDirectory(profile), "autologin.data");

            var newProfileDir = GetProfileDirectory(profile);
            if (!Directory.Exists(newProfileDir))
                Directory.CreateDirectory(newProfileDir);

            if (File.Exists(oldAutoLoginFile))
                File.Copy(oldAutoLoginFile, newAutoLoginFile);
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not copy old profile data to the new profile: {ex.Message}");
        }
    }

    /// <summary>
    ///     Get profile config file name
    /// </summary>
    /// <returns></returns>
    public static string GetProfileConfigFileName()
    {
        if (!string.IsNullOrWhiteSpace(SelectedCharacter))
        {
            var charDir = Path.Combine(Kernel.BasePath, "User", SelectedCharacter);
            if (!Directory.Exists(charDir))
                Directory.CreateDirectory(charDir);
                
            return Path.Combine(charDir, "Profiles.rs");
        }
        return Path.Combine(Kernel.BasePath, "User", "Profiles.rs");
    }

    public static string GetProfileFile(string profileName)
    {
        return Path.Combine(Kernel.BasePath, "User", $"{profileName}.rs");
    }

    public static string GetProfileDirectory(string profileName)
    {
        return Path.Combine(Kernel.BasePath, "User", profileName);
    }
}
