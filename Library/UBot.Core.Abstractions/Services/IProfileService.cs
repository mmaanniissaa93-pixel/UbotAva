namespace UBot.Core.Abstractions.Services;

public interface IProfileService
{
    string[] Profiles { get; }
    bool IsProfileLoadedByArgs { get; set; }
    string SelectedCharacter { get; set; }
    string SelectedProfile { get; }
    bool ShowProfileDialog { get; set; }

    void Initialize();
    bool Any();
    bool SetSelectedProfile(string profile);
    bool ProfileExists(string profile);
    bool Add(string profile, bool useAsBase = false);
    bool Remove(string profile);
    string GetProfileConfigFileName();
    string GetProfileFile(string profileName);
    string GetProfileDirectory(string profileName);
}
