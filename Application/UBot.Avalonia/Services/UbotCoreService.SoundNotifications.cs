using System;
using System.Threading.Tasks;
using UBot.Core;
using UBot.Core.Objects;

namespace UBot.Avalonia.Services;

internal sealed class UbotSoundNotificationService : UbotServiceBase
{
    internal Task<SoundNotificationSettingsDto> GetSoundNotificationSettingsAsync()
    {
        var player = UBot.Core.RuntimeAccess.Session?.Player;
        var sounds = player?.NotificationSounds;

        T GetConfig<T>(string key, T defaultValue)
        {
            return player != null 
                ? UBot.Core.RuntimeAccess.Player.Get(key, defaultValue) 
                : UBot.Core.RuntimeAccess.Global.Get(key, defaultValue);
        }

        var settings = new SoundNotificationSettingsDto
        {
            IsPlayerLoggedIn = player != null,

            PlayUniqueAppeared = sounds?.PlayUniqueAlarmGeneral ?? GetConfig("UBot.Sounds.PlayUniqueAlarmGeneral", false),
            PathUniqueAppeared = sounds?.PathUniqueAlarmGeneral ?? GetConfig("UBot.Sounds.PathUniqueAlarmGeneral", string.Empty),
            MatchRegex = sounds?.RegexUniqueAlarmGeneral?.ToString() ?? GetConfig("UBot.Sounds.RegexUniqueAlarmGeneral", "^.*$"),

            PlayTigerGirl = sounds?.PlayUniqueAlarmTigerGirl ?? GetConfig("UBot.Sounds.PlayUniqueAlarmTigerGirl", false),
            PathTigerGirl = sounds?.PathUniqueAlarmTigerGirl ?? GetConfig("UBot.Sounds.PathUniqueAlarmTigerGirl", string.Empty),

            PlayCerberus = sounds?.PlayUniqueAlarmCerberus ?? GetConfig("UBot.Sounds.PlayUniqueAlarmCerberus", false),
            PathCerberus = sounds?.PathUniqueAlarmCerberus ?? GetConfig("UBot.Sounds.PathUniqueAlarmCerberus", string.Empty),

            PlayCaptainIvy = sounds?.PlayUniqueAlarmCaptainIvy ?? GetConfig("UBot.Sounds.PlayUniqueAlarmCaptainIvy", false),
            PathCaptainIvy = sounds?.PathUniqueAlarmCaptainIvy ?? GetConfig("UBot.Sounds.PathUniqueAlarmCaptainIvy", string.Empty),

            PlayUruchi = sounds?.PlayUniqueAlarmUruchi ?? GetConfig("UBot.Sounds.PlayUniqueAlarmUruchi", false),
            PathUruchi = sounds?.PathUniqueAlarmUruchi ?? GetConfig("UBot.Sounds.PathUniqueAlarmUruchi", string.Empty),

            PlayIsyutaru = sounds?.PlayUniqueAlarmIsyutaru ?? GetConfig("UBot.Sounds.PlayUniqueAlarmIsyutaru", false),
            PathIsyutaru = sounds?.PathUniqueAlarmIsyutaru ?? GetConfig("UBot.Sounds.PathUniqueAlarmIsyutaru", string.Empty),

            PlayLordYarkan = sounds?.PlayUniqueAlarmLordYarkan ?? GetConfig("UBot.Sounds.PlayUniqueAlarmLordYarkan", false),
            PathLordYarkan = sounds?.PathUniqueAlarmLordYarkan ?? GetConfig("UBot.Sounds.PathUniqueAlarmLordYarkan", string.Empty),

            PlayDemonShaitan = sounds?.PlayUniqueAlarmDemonShaitan ?? GetConfig("UBot.Sounds.PlayUniqueAlarmDemonShaitan", false),
            PathDemonShaitan = sounds?.PathUniqueAlarmDemonShaitan ?? GetConfig("UBot.Sounds.PathUniqueAlarmDemonShaitan", string.Empty),

            PlayUniqueInRange = sounds?.PlayAlarmUniqueInRange ?? GetConfig("UBot.Sounds.PlayAlarmUniqueInRange", false),
            PathUniqueInRange = sounds?.PathAlarmUniqueInRange ?? GetConfig("UBot.Sounds.PathAlarmUniqueInRange", string.Empty)
        };

        if (string.IsNullOrWhiteSpace(settings.MatchRegex))
            settings.MatchRegex = "^.*$";

        return Task.FromResult(settings);
    }

    internal Task<bool> SaveSoundNotificationSettingsAsync(SoundNotificationSettingsDto settings)
    {
        if (settings == null)
            return Task.FromResult(false);

        var sounds = UBot.Core.RuntimeAccess.Session.Player?.NotificationSounds;
        if (sounds == null)
            return Task.FromResult(false);

        sounds.UpdatePlayerSettings("UBot.Sounds.PlayUniqueAlarmGeneral", settings.PlayUniqueAppeared);
        sounds.UpdatePlayerSettings("UBot.Sounds.PathUniqueAlarmGeneral", NormalizePath(settings.PathUniqueAppeared));
        sounds.UpdatePlayerSettings("UBot.Sounds.RegexUniqueAlarmGeneral", NormalizeRegex(settings.MatchRegex));

        sounds.UpdatePlayerSettings("UBot.Sounds.PlayUniqueAlarmTigerGirl", settings.PlayTigerGirl);
        sounds.UpdatePlayerSettings("UBot.Sounds.PathUniqueAlarmTigerGirl", NormalizePath(settings.PathTigerGirl));

        sounds.UpdatePlayerSettings("UBot.Sounds.PlayUniqueAlarmCerberus", settings.PlayCerberus);
        sounds.UpdatePlayerSettings("UBot.Sounds.PathUniqueAlarmCerberus", NormalizePath(settings.PathCerberus));

        sounds.UpdatePlayerSettings("UBot.Sounds.PlayUniqueAlarmCaptainIvy", settings.PlayCaptainIvy);
        sounds.UpdatePlayerSettings("UBot.Sounds.PathUniqueAlarmCaptainIvy", NormalizePath(settings.PathCaptainIvy));

        sounds.UpdatePlayerSettings("UBot.Sounds.PlayUniqueAlarmUruchi", settings.PlayUruchi);
        sounds.UpdatePlayerSettings("UBot.Sounds.PathUniqueAlarmUruchi", NormalizePath(settings.PathUruchi));

        sounds.UpdatePlayerSettings("UBot.Sounds.PlayUniqueAlarmIsyutaru", settings.PlayIsyutaru);
        sounds.UpdatePlayerSettings("UBot.Sounds.PathUniqueAlarmIsyutaru", NormalizePath(settings.PathIsyutaru));

        sounds.UpdatePlayerSettings("UBot.Sounds.PlayUniqueAlarmLordYarkan", settings.PlayLordYarkan);
        sounds.UpdatePlayerSettings("UBot.Sounds.PathUniqueAlarmLordYarkan", NormalizePath(settings.PathLordYarkan));

        sounds.UpdatePlayerSettings("UBot.Sounds.PlayUniqueAlarmDemonShaitan", settings.PlayDemonShaitan);
        sounds.UpdatePlayerSettings("UBot.Sounds.PathUniqueAlarmDemonShaitan", NormalizePath(settings.PathDemonShaitan));

        sounds.UpdatePlayerSettings("UBot.Sounds.PlayAlarmUniqueInRange", settings.PlayUniqueInRange);
        sounds.UpdatePlayerSettings("UBot.Sounds.PathAlarmUniqueInRange", NormalizePath(settings.PathUniqueInRange));

        if (UBot.Core.RuntimeAccess.Session?.Player != null)
            UBot.Core.RuntimeAccess.Player.Save();
        return Task.FromResult(true);
    }

    private static string NormalizePath(string? path)
    {
        return (path ?? string.Empty).Trim().Trim('"');
    }

    private static string NormalizeRegex(string? value)
    {
        var regex = (value ?? string.Empty).Trim();
        return string.IsNullOrWhiteSpace(regex) ? "^.*$" : regex;
    }
}
