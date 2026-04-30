using System.Linq;
using UBot.Core;
using UBot.Core.Objects;

namespace UBot.Training.Bundle.Avoidance;

internal class AvoidanceBundle : IBundle
{
    /// <summary>
    ///     Gets the avoidance list.
    /// </summary>
    /// <value>
    ///     The avoidance list.
    /// </value>
    public MonsterRarity[] AvoidanceList => UBot.Core.RuntimeAccess.Player.GetEnums<MonsterRarity>("UBot.Avoidance.Avoid");

    /// <summary>
    ///     Gets the preferance list.
    /// </summary>
    /// <value>
    ///     The preferance list.
    /// </value>
    public MonsterRarity[] PreferanceList => UBot.Core.RuntimeAccess.Player.GetEnums<MonsterRarity>("UBot.Avoidance.Prefer");

    /// <summary>
    ///     Gets the berserk list.
    /// </summary>
    /// <value>
    ///     The berserk list.
    /// </value>
    public MonsterRarity[] BerserkerList => UBot.Core.RuntimeAccess.Player.GetEnums<MonsterRarity>("UBot.Avoidance.Berserk");

    /// <summary>
    ///     Invokes this instance.
    /// </summary>
    public void Invoke() { }

    /// <summary>
    ///     Refreshes this instance.
    /// </summary>
    public void Refresh() { }

    public void Stop()
    {
        //Nothing to do
    }

    /// <summary>
    ///     Avoids the monster.
    /// </summary>
    /// <param name="rarity">The rarity.</param>
    /// <returns></returns>
    public bool AvoidMonster(MonsterRarity rarity) => AvoidanceList.Contains(rarity);

    /// <summary>
    ///     Prefers the monster.
    /// </summary>
    /// <param name="rarity">The rarity.</param>
    /// <returns></returns>
    public bool PreferMonster(MonsterRarity rarity) => PreferanceList.Contains(rarity);

    /// <summary>
    ///     Use Berserk on monster.
    /// </summary>
    /// <param name="rarity">The rarity.</param>
    /// <returns></returns>
    public bool UseBerserkOnMonster(MonsterRarity rarity) => BerserkerList.Contains(rarity);
}
