using System.Collections.Generic;

namespace UBot.Core.Abstractions.Services;

public interface IProfileStorage
{
    string[] LoadProfiles(string character);
    void SaveProfiles(string character, IEnumerable<string> profiles);
    string GetSelectedProfile(string character);
    void SaveSelectedProfile(string character, string profile);
    bool GetShowProfileDialog(string character);
    void SaveShowProfileDialog(string character, bool show);

    void EnsureProfileDirectory(string profileName);
    void CopyProfileFile(string sourceProfile, string targetProfile);
    void CopyCharacterProfileFile(string character, string sourceProfile, string targetProfile);
    void CopyAutoLoginFile(string sourceProfile, string targetProfile);
    void DeleteProfile(string profileName);

    string GetProfileConfigFileName(string character);
    string GetProfileFile(string profileName);
    string GetProfileDirectory(string profileName);
}
