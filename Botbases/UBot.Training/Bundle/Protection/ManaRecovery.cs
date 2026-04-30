using UBot.Core;

namespace UBot.Training.Bundle.Protection;

internal class ManaRecovery
{
    public static bool Active => UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Protection.checkUseSkillMP");
    public static int Value => UBot.Core.RuntimeAccess.Player.Get("UBot.Protection.ThresholdPlayerSkillMPMin", 50);
    public static uint SkillId => UBot.Core.RuntimeAccess.Player.Get<uint>("UBot.Protection.MpSkill");
    public static double Current => 100.0 * UBot.Core.RuntimeAccess.Session.Player.Mana / UBot.Core.RuntimeAccess.Session.Player.MaximumMana;
}
