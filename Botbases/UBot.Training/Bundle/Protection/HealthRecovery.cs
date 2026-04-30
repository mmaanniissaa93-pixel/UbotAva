using UBot.Core;

namespace UBot.Training.Bundle.Protection;

internal class HealthRecovery
{
    public static bool Active => UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.checkUseSkillHP");
    public static int Value => UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.numPlayerSkillHPMin", 50);
    public static uint SkillId => UBot.Core.RuntimeAccess.Player.Get<uint>("UBot.Protection.HpSkill");
    public static double Current => 100.0 * UBot.Core.RuntimeAccess.Session.Player.Health / UBot.Core.RuntimeAccess.Session.Player.MaximumHealth;
}
