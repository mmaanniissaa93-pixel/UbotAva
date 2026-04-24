using System;
using System.Threading.Tasks;
using UBot.Core;
using UBot.Core.Objects;

namespace UBot.Avalonia.Services;

internal sealed class UbotSoundNotificationService : UbotServiceBase
{
    internal Task<SoundNotificationSettingsDto> GetSoundNotificationSettingsAsync()
    {
        var sounds = Game.Player?.NotificationSounds;

        var settings = new SoundNotificationSettingsDto
        {
            IsPlayerLoggedIn = Game.Player != null,

            PlayUniqueAppeared = sounds?.PlayUniqueAlarmGeneral ?? PlayerConfig.Get("UBot.Sounds.PlayUniqueAlarmGeneral", false),
            PathUniqueAppeared = sounds?.PathUniqueAlarmGeneral ?? PlayerConfig.Get("UBot.Sounds.PathUniqueAlarmGeneral", string.Empty),
            MatchRegex = sounds?.RegexUniqueAlarmGeneral?.ToString() ?? PlayerConfig.Get("UBot.Sounds.RegexUniqueAlarmGeneral", "^.*$"),

            PlayTigerGirl = sounds?.PlayUniqueAlarmTigerGirl ?? PlayerConfig.Get("UBot.Sounds.PlayUniqueAlarmTigerGirl", false),
            PathTigerGirl = sounds?.PathUniqueAlarmTigerGirl ?? PlayerConfig.Get("UBot.Sounds.PathUniqueAlarmTigerGirl", string.Empty),

            PlayCerberus = sounds?.PlayUniqueAlarmCerberus ?? PlayerConfig.Get("UBot.Sounds.PlayUniqueAlarmCerberus", false),
            PathCerberus = sounds?.PathUniqueAlarmCerberus ?? PlayerConfig.Get("UBot.Sounds.PathUniqueAlarmCerberus", string.Empty),

            PlayCaptainIvy = sounds?.PlayUniqueAlarmCaptainIvy ?? PlayerConfig.Get("UBot.Sounds.PlayUniqueAlarmCaptainIvy", false),
            PathCaptainIvy = sounds?.PathUniqueAlarmCaptainIvy ?? PlayerConfig.Get("UBot.Sounds.PathUniqueAlarmCaptainIvy", string.Empty),

            PlayUruchi = sounds?.PlayUniqueAlarmUruchi ?? PlayerConfig.Get("UBot.Sounds.PlayUniqueAlarmUruchi", false),
            PathUruchi = sounds?.PathUniqueAlarmUruchi ?? PlayerConfig.Get("UBot.Sounds.PathUniqueAlarmUruchi", string.Empty),

            PlayIsyutaru = sounds?.PlayUniqueAlarmIsyutaru ?? PlayerConfig.Get("UBot.Sounds.PlayUniqueAlarmIsyutaru", false),
            PathIsyutaru = sounds?.PathUniqueAlarmIsyutaru ?? PlayerConfig.Get("UBot.Sounds.PathUniqueAlarmIsyutaru", string.Empty),

            PlayLordYarkan = sounds?.PlayUniqueAlarmLordYarkan ?? PlayerConfig.Get("UBot.Sounds.PlayUniqueAlarmLordYarkan", false),
            PathLordYarkan = sounds?.PathUniqueAlarmLordYarkan ?? PlayerConfig.Get("UBot.Sounds.PathUniqueAlarmLordYarkan", string.Empty),

            PlayDemonShaitan = sounds?.PlayUniqueAlarmDemonShaitan ?? PlayerConfig.Get("UBot.Sounds.PlayUniqueAlarmDemonShaitan", false),
            PathDemonShaitan = sounds?.PathUniqueAlarmDemonShaitan ?? PlayerConfig.Get("UBot.Sounds.PathUniqueAlarmDemonShaitan", string.Empty),

            PlayUniqueInRange = sounds?.PlayAlarmUniqueInRange ?? PlayerConfig.Get("UBot.Sounds.PlayAlarmUniqueInRange", false),
            PathUniqueInRange = sounds?.PathAlarmUniqueInRange ?? PlayerConfig.Get("UBot.Sounds.PathAlarmUniqueInRange", string.Empty)
        };

        if (string.IsNullOrWhiteSpace(settings.MatchRegex))
            settings.MatchRegex = "^.*$";

        return Task.FromResult(settings);
    }

    internal Task<bool> SaveSoundNotificationSettingsAsync(SoundNotificationSettingsDto settings)
    {
        if (settings == null)
            return Task.FromResult(false);

        var sounds = Game.Player?.NotificationSounds;
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

        PlayerConfig.Save();
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
