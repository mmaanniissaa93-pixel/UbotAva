using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using UBot.Core.Abstractions;

namespace UBot.Core.Objects.Spawn;

public class SpawnedBionic : SpawnedEntity
{
    /// <summary>
    ///     <inheritdoc />
    /// </summary>
    /// <param name="objId">The ref obj id</param>
    public SpawnedBionic(uint objId, IGameStateRuntimeContext context = null)
        : base(context)
    {
        Id = objId;

        if (Record != null)
            Health = Record.MaxHealth;
    }

    /// <summary>
    ///     Gets the distance to player.
    /// </summary>
    /// <value>
    ///     The distance to player.
    /// </value>
    public double DistanceToPlayer => _context.DistanceToPlayer(Movement.Source);

    /// <summary>
    ///     Gets a value indicating whether [attacking player].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [attacking player]; otherwise, <c>false</c>.
    /// </value>
    public bool AttackingPlayer { get; private set; }

    /// <summary>
    ///     Gets or sets a value indicating whether this instance has health.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance has health; otherwise, <c>false</c>.
    /// </value>
    public bool HasHealth => Health > 0;

    /// <summary>
    ///     Gets or sets the health.
    /// </summary>
    /// <value>
    ///     The health.
    /// </value>
    public int Health { get; set; }

    /// <summary>
    ///     Gets or sets the bad effect.
    /// </summary>
    /// <value>
    ///     The bad effect.
    /// </value>
    public BadEffect BadEffect { get; set; }

    /// <summary>
    ///     Gets or sets the target identifier.
    /// </summary>
    /// <value>
    ///     The target identifier.
    /// </value>
    public uint TargetId { get; set; }

    /// <summary>
    ///     Starts the attacking timer.
    /// </summary>
    /// <param name="duration">The duration.</param>
    public void StartAttackingTimer(int duration = 10000)
    {
        //Log.Debug("Attacking timer has started.");

        AttackingPlayer = true;
        /*
        var timer = new Timer();
        timer.AutoReset = false;
        timer.Elapsed += Timer_Elapsed;*/
    }

    /// <summary>
    ///     Handles the Elapsed event of the Timer control.
    /// </summary>
    /// <param name="sender">The source of the event.</param>
    /// <param name="e">The <see cref="ElapsedEventArgs" /> instance containing the event data.</param>
    private void Timer_Elapsed(object sender, ElapsedEventArgs e)
    {
        _context.LogDebug("Attacking timer has elapsed.");
        AttackingPlayer = false;
    }

    /// <summary>
    ///     Selects the entity.
    /// </summary>
    /// <param name="uniqueId">The unique identifier.</param>
    /// <returns></returns>
    public bool TrySelect()
    {
        if ((_context.SelectedEntity as SpawnedBionic)?.UniqueId == UniqueId)
            return true;

        _context.LogDebug(
            $"Trying to select the entity: {UniqueId} State: {State.LifeState} Health: {Health} HasHealth: {HasHealth} Dst: {Math.Round(DistanceToPlayer, 1)}"
        );
        return _context.SendSelectEntity(UniqueId);
    }

    /// <summary>
    ///     Deselects the entity.
    /// </summary>
    /// <returns></returns>
    public bool TryDeselect()
    {
        _context.LogDebug($"Entity deselected: {UniqueId}");
        return _context.SendDeselectEntity(UniqueId);
    }

    /// <summary>
    ///     Gets a list of spawned bionics that are attacking this entity.
    /// </summary>
    /// <returns></returns>
    public List<SpawnedBionic> GetAttackers()
    {
        return (_context.GetEntities(typeof(SpawnedBionic), entity => ((SpawnedBionic)entity).TargetId == UniqueId)
                as IEnumerable<SpawnedBionic>)
            ?.ToList();
    }
}
