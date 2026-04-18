using UBot.Core;

namespace UBot.Training.Bundle.Protection;

internal class ManaRecovery
{
    public static bool Active => PlayerConfig.Get<bool>("UBot.Protection.checkUseSkillMP");
    public static int Value => PlayerConfig.Get("UBot.Protection.numPlayerSkillMPMin", 50);
    public static uint SkillId => PlayerConfig.Get<uint>("UBot.Protection.MpSkill");
    public static double Current => 100.0 * Game.Player.Mana / Game.Player.MaximumMana;
}
