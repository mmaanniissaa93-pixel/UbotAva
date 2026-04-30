using UBot.Core;
using UBot.Core.Components;
using UBot.Core.Objects.Spawn;

namespace UBot.Training.Bundle.Berzerk;

internal class BerzerkBundle : IBundle
{
    /// <summary>
    ///     Gets or sets the configuration.
    /// </summary>
    /// <value>
    ///     The configuration.
    /// </value>
    public BerzerkConfig Config { get; set; }

    /// <summary>
    ///     Invokes this instance.
    /// </summary>
    /// <exception cref="System.NotImplementedException"></exception>
    public void Invoke()
    {
        if (!UBot.Core.RuntimeAccess.Session.Player.CanEnterBerzerk || UBot.Core.RuntimeAccess.Session.Player.HasActiveVehicle)
            return;

        if (Config.WhenFull)
        {
            UBot.Core.RuntimeAccess.Session.Player.EnterBerzerkMode();

            return;
        }

        if (Config.SurroundedByMonsters)
        {
            var mobAmount = SpawnManager.Count<SpawnedMonster>(m => m.AttackingPlayer && m.DistanceToPlayer < 20);
            if (mobAmount >= Config.SurroundingMonsterAmount)
            {
                UBot.Core.RuntimeAccess.Session.Player.EnterBerzerkMode();
                return;
            }
        }

        if (Config.WhenTargetSpecificRartiyMonster)
        {
            if (UBot.Core.RuntimeAccess.Session.SelectedEntity is SpawnedMonster e && Bundles.Avoidance.UseBerserkOnMonster(e.Rarity))
            {
                UBot.Core.RuntimeAccess.Session.Player.EnterBerzerkMode();
                return;
            }
        }

        if (!Config.BeeingAttackedByAwareMonster)
            return;

        if (UBot.Core.RuntimeAccess.Session.SelectedEntity is not SpawnedMonster entity)
            return;

        if (Bundles.Avoidance.AvoidMonster(entity.Rarity))
            UBot.Core.RuntimeAccess.Session.Player.EnterBerzerkMode();
    }

    /// <summary>
    ///     Refreshes this instance.
    /// </summary>
    public void Refresh()
    {
        Config = new BerzerkConfig
        {
            WhenFull = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Training.checkBerzerkWhenFull"),
            BeeingAttackedByAwareMonster = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Training.checkBerzerkAvoidance"),
            SurroundedByMonsters = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Training.checkBerzerkMonsterAmount"),
            SurroundingMonsterAmount = UBot.Core.RuntimeAccess.Player.Get<byte>("UBot.Training.numBerzerkMonsterAmount", 5),
            WhenTargetSpecificRartiyMonster = UBot.Core.RuntimeAccess.Player.Get<bool>("UBot.Training.checkBerserkOnMonsterRarity"),
        };
    }

    public void Stop()
    {
        //Nothing to do
    }
}
