#nullable enable annotations

using System;
using System.Collections.ObjectModel;
using System.Linq;
using UBot.Core.Abstractions.Services;
using UBot.Core.Services;

namespace UBot.Core.Components;

public class ProfileManager
{
    private static IProfileService _service = ServiceRuntime.Profile ?? new ProfileService();

    static ProfileManager()
    {
        Initialize(_service);
    }

    public static string[] Profiles => _service.Profiles;

    public static bool IsProfileLoadedByArgs
    {
        get => _service.IsProfileLoadedByArgs;
        set => _service.IsProfileLoadedByArgs = value;
    }

    public static string SelectedCharacter
    {
        get => _service.SelectedCharacter;
        set => _service.SelectedCharacter = value;
    }

    public static string SelectedProfile => _service.SelectedProfile;

    public static bool ShowProfileDialog
    {
        get => _service.ShowProfileDialog;
        set => _service.ShowProfileDialog = value;
    }

    public static void Initialize(IProfileService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        ServiceRuntime.Profile = _service;
        _service.Initialize();
    }

    public static bool Any() => _service.Any();
    public static bool SetSelectedProfile(string profile) => _service.SetSelectedProfile(profile);
    public static bool ProfileExists(string profile) => _service.ProfileExists(profile);
    public static bool Add(string profile, bool useAsBase = false) => _service.Add(profile, useAsBase);
    public static bool Remove(string profile) => _service.Remove(profile);
    public static string GetProfileConfigFileName() => _service.GetProfileConfigFileName();
    public static string GetProfileFile(string profileName) => _service.GetProfileFile(profileName);
    public static string GetProfileDirectory(string profileName) => _service.GetProfileDirectory(profileName);
}

public sealed class ProfileService : IProfileService
{
    private readonly ObservableCollection<string> _profiles = new();
    private string _selectedCharacter = string.Empty;
    private bool _suppressProfileSave;

    public string[] Profiles => _profiles.ToArray();
    public bool IsProfileLoadedByArgs { get; set; }

    public string SelectedCharacter
    {
        get => _selectedCharacter;
        set
        {
            var normalized = value ?? string.Empty;
            if (_selectedCharacter == normalized)
                return;

            _selectedCharacter = normalized;
            LoadProfiles();
        }
    }

    public string SelectedProfile => Storage?.GetSelectedProfile(SelectedCharacter) ?? "Default";

    public bool ShowProfileDialog
    {
        get => Storage?.GetShowProfileDialog(SelectedCharacter) ?? false;
        set => Storage?.SaveShowProfileDialog(SelectedCharacter, value);
    }

    public void Initialize()
    {
        LoadProfiles();
    }

    public bool Any()
    {
        return _profiles.Any();
    }

    public bool SetSelectedProfile(string profile)
    {
        if (!_profiles.Any(p => p == profile))
            return false;

        Storage?.SaveSelectedProfile(SelectedCharacter, profile);
        return true;
    }

    public bool ProfileExists(string profile)
    {
        return _profiles.Any(p => p.Equals(profile, StringComparison.InvariantCultureIgnoreCase));
    }

    public bool Add(string profile, bool useAsBase = false)
    {
        if (string.IsNullOrWhiteSpace(profile))
            return false;

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
        SaveProfiles();

        if (useAsBase)
            CopyOldProfileData(profile);

        Storage?.EnsureProfileDirectory(profile);
        SetSelectedProfile(profile);

        return true;
    }

    public bool Remove(string profile)
    {
        var removed = _profiles.Remove(profile);
        if (removed)
            SaveProfiles();

        return removed;
    }

    public string GetProfileConfigFileName()
    {
        return Storage?.GetProfileConfigFileName(SelectedCharacter) ?? string.Empty;
    }

    public string GetProfileFile(string profileName)
    {
        return Storage?.GetProfileFile(profileName) ?? string.Empty;
    }

    public string GetProfileDirectory(string profileName)
    {
        return Storage?.GetProfileDirectory(profileName) ?? string.Empty;
    }

    private void LoadProfiles()
    {
        _suppressProfileSave = true;
        try
        {
            _profiles.Clear();
            foreach (var profile in Storage?.LoadProfiles(SelectedCharacter) ?? Array.Empty<string>())
                _profiles.Add(profile);

            if (!_profiles.Any())
                _profiles.Add("Default");
        }
        finally
        {
            _suppressProfileSave = false;
        }

        SaveProfiles();
    }

    private void SaveProfiles()
    {
        if (_suppressProfileSave)
            return;

        Storage?.SaveProfiles(SelectedCharacter, _profiles);
    }

    private void CopyOldProfileData(string profile)
    {
        try
        {
            Storage?.CopyProfileFile(SelectedProfile, profile);

            if (!string.IsNullOrWhiteSpace(SelectedCharacter))
                Storage?.CopyCharacterProfileFile(SelectedCharacter, SelectedProfile, profile);

            Storage?.CopyAutoLoginFile(SelectedProfile, profile);
        }
        catch (Exception ex)
        {
            ServiceRuntime.Log?.Warn($"Could not copy old profile data to the new profile: {ex.Message}");
        }
    }

    private static IProfileStorage Storage => ServiceRuntime.ProfileStorage;
}
