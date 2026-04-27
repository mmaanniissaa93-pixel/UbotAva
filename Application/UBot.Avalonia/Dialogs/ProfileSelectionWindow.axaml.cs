using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Plugins;

namespace UBot.Avalonia.Dialogs;

public partial class ProfileSelectionWindow : Window
{
    private readonly ObservableCollection<string> _profiles = new();

    public string SelectedProfile { get; private set; } = string.Empty;
    public bool SaveSelection { get; private set; } = true;
    public bool Applied { get; private set; }

    public ProfileSelectionWindow()
    {
        InitializeComponent();
        ProfileList.ItemsSource = _profiles;

        var hasCharacter = !string.IsNullOrWhiteSpace(ProfileManager.SelectedCharacter);
        ProfileList.IsEnabled = hasCharacter;
        NewProfileBox.IsEnabled = hasCharacter;
        AddBtn.IsEnabled = hasCharacter;
        DeleteBtn.IsEnabled = hasCharacter;

        if (!hasCharacter)
        {
            var msg = "Entering game is required for profile management.";
            ToolTip.SetTip(ProfileList, "Select profile is disabled until character is loaded.");
            ToolTip.SetTip(NewProfileBox, msg);
            ToolTip.SetTip(AddBtn, msg);
            ToolTip.SetTip(DeleteBtn, msg);
        }
        try
        {
            LoadProfiles();
        }
        catch
        {
            // Keep dialog open even if profile storage is unavailable.
            _profiles.Clear();
            _profiles.Add("Default");
            ProfileList.SelectedItem = "Default";
            NewProfileBox.Text = "Default";
            SaveSelectionCheck.IsChecked = true;
        }
    }

    private void AddProfile_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProfileManager.SelectedCharacter))
            return;

        var candidate = (NewProfileBox.Text ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(candidate))
            candidate = GenerateProfileName();

        if (ProfileManager.ProfileExists(candidate))
        {
            SelectProfile(candidate);
            return;
        }

        if (!ProfileManager.Add(candidate, true))
            return;

        LoadProfiles(candidate);
    }

    private void DeleteProfile_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(ProfileManager.SelectedCharacter))
            return;

        var current = GetSelectedExistingProfile();
        if (string.IsNullOrWhiteSpace(current))
            return;

        if (_profiles.Count <= 1)
            return;

        var wasSelected = string.Equals(ProfileManager.SelectedProfile, current, StringComparison.OrdinalIgnoreCase);
        var fallback = _profiles.FirstOrDefault(p => !string.Equals(p, current, StringComparison.OrdinalIgnoreCase)) ?? "Default";

        if (!ProfileManager.Remove(current))
            return;

        if (!ProfileManager.Any())
            ProfileManager.Add("Default");

        if (wasSelected)
            ProfileManager.SetSelectedProfile(fallback);

        LoadProfiles(wasSelected ? fallback : ProfileManager.SelectedProfile);
    }

    private void Continue_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var selected = GetCandidateProfileName();
            if (string.IsNullOrWhiteSpace(selected))
                selected = ProfileManager.SelectedProfile;

            if (!ProfileManager.ProfileExists(selected))
            {
                if (string.IsNullOrWhiteSpace(ProfileManager.SelectedCharacter))
                {
                    // Fallback to Default if trying to use non-existent profile without character
                    selected = "Default";
                }
                else if (!ProfileManager.Add(selected, true))
                {
                    return;
                }
            }

            if (!ProfileManager.SetSelectedProfile(selected))
                return;

            try
            {
                GlobalConfig.Load();
                
                var activeChar = ProfileManager.SelectedCharacter;
                if (string.IsNullOrWhiteSpace(activeChar))
                    activeChar = GlobalConfig.Get("UBot.General.AutoLoginCharacter", string.Empty)?.Trim() ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(activeChar))
                {
                    ProfileManager.SelectedCharacter = activeChar;
                    PlayerConfig.Load(activeChar);
                }

                ExtensionManager.OnProfileChanged();
            }
            catch
            {
                // Keep dialog flow resilient even if profile data is incomplete.
            }

            SaveSelection = SaveSelectionCheck.IsChecked == true;
            ProfileManager.ShowProfileDialog = !SaveSelection;
            SelectedProfile = selected;
            Applied = true;
            Close();
        }
        catch
        {
            // Do not close owner window on unexpected profile apply failures.
        }
    }

    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void LoadProfiles(string? preferred = null)
    {
        _profiles.Clear();
        foreach (var profile in ProfileManager.Profiles.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
            _profiles.Add(profile);

        if (_profiles.Count == 0)
        {
            ProfileManager.Add("Default");
            _profiles.Add("Default");
        }

        SaveSelectionCheck.IsChecked = !ProfileManager.ShowProfileDialog;
        SelectProfile(string.IsNullOrWhiteSpace(preferred) ? ProfileManager.SelectedProfile : preferred);
    }

    private void SelectProfile(string profile)
    {
        var existing = _profiles.FirstOrDefault(p => string.Equals(p, profile, StringComparison.OrdinalIgnoreCase));
        if (string.IsNullOrWhiteSpace(existing))
            existing = _profiles.FirstOrDefault() ?? "Default";

        ProfileList.SelectedItem = existing;
        NewProfileBox.Text = existing;
    }

    private string GetCandidateProfileName()
    {
        var textValue = (NewProfileBox.Text ?? string.Empty).Trim();
        var selectedValue = GetSelectedExistingProfile();

        // If user typed something new that isn't the current selection, use that.
        if (!string.IsNullOrWhiteSpace(textValue) && !string.Equals(textValue, selectedValue, StringComparison.OrdinalIgnoreCase))
            return textValue;

        // Otherwise use the selected value from dropdown.
        if (!string.IsNullOrWhiteSpace(selectedValue))
            return selectedValue;

        return textValue;
    }

    private string GetSelectedExistingProfile()
    {
        return ProfileList.SelectedItem switch
        {
            string value => value.Trim(),
            ComboBoxItem item => (item.Content?.ToString() ?? string.Empty).Trim(),
            _ => string.Empty
        };
    }

    private string GenerateProfileName()
    {
        var index = 1;
        while (true)
        {
            var candidate = $"Profile {index}";
            if (!ProfileManager.ProfileExists(candidate))
                return candidate;
            index++;
        }
    }
}
