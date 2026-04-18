using UBot.Core;

namespace UBot.Training.Bundle.Protection;

internal class HealthRecovery
{
    public static bool Active => PlayerConfig.Get<bool>("UBot.Protection.checkUseSkillHP");
    public static int Value => PlayerConfig.Get("UBot.Protection.numPlayerSkillHPMin", 50);
    public static uint SkillId => PlayerConfig.Get<uint>("UBot.Protection.HpSkill");
    public static double Current => 100.0 * Game.Player.Health / Game.Player.MaximumHealth;
}
