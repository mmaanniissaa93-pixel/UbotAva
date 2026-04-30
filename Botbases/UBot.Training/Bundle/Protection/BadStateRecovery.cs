using UBot.Core;
using UBot.Core.Objects;

namespace UBot.Training.Bundle.Protection;

internal class BadStateRecovery
{
    public static bool Active => UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.CheckUseBadStatusSkill");
    public static uint SkillId => UBot.Core.RuntimeAccess.Player.Get<uint>("UBot.Protection.BadStatusSkill");
    public static uint SkillIdForUniversall => UBot.Core.RuntimeAccess.Player.Get<uint>("UBot.Protection.BadStatusSkill");
    public static uint SkillIdForPurification => UBot.Core.RuntimeAccess.Player.Get<uint>("UBot.Protection.BadStatusSkill");
    public static bool IsInBadStatus => IsUniversall || IsPurification;

    public static bool IsUniversall => (UBot.Core.RuntimeAccess.Session.Player.BadEffect & BadEffectAll.UniversallPillEffects) != 0;
    public static bool IsPurification => (UBot.Core.RuntimeAccess.Session.Player.BadEffect & BadEffectAll.PurificationPillEffects) != 0;
}
