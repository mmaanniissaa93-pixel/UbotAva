using System.Collections.Generic;
using System.Linq;
using UBot.Core.Abstractions;
using UBot.Core.Objects.Spawn;

namespace UBot.Core.Objects.Inventory;

public class Storage : InventoryItemCollection
{
    private readonly IGameStateRuntimeContext _context;

    /// <summary>
    ///     Gets a value indicating whether this instance is sorting.
    /// </summary>
    /// <value>
    ///     <c>true</c> if this instance is sorting; otherwise, <c>false</c>.
    /// </value>
    public bool IsSorting { get; private set; }

    /// <summary>
    ///     The gold amount in the storage
    /// </summary>
    public ulong Gold;

    /// <summary>
    ///     Create instance of the <seealso cref="Storage" />
    /// </summary>
    /// <param name="size">The standart 150(5 page)</param>
    public Storage(byte size = 150, IGameStateRuntimeContext context = null)
        : base(size)
    {
        _context = context ?? GameStateRuntimeProvider.Instance;
    }

    /// <summary>
    ///     Moves the item inside Character's Inventory.
    /// </summary>
    /// <param name="sourceSlot">The source slot.</param>
    /// <param name="destinationSlot">The destination slot.</param>
    /// <param name="amount">The amount.</param>
    /// <param name="npc">Selected storage NPC.</param>
    /// <returns><c>true</c> if successfully moved; otherwise, <c>false</c>.</returns>
    public bool MoveItem(byte sourceSlot, byte destinationSlot, ushort amount = 0, SpawnedBionic npc = null)
    {
        var itemAtSource = GetItemAt(sourceSlot);
        if (itemAtSource == null)
            return false;

        if (npc == null || npc.UniqueId == 0)
            return false;

        if (amount == 0)
            amount = itemAtSource.Amount;

        return _context.SendStorageMove(sourceSlot, destinationSlot, amount, npc);
    }

    public void Sort(SpawnedBionic npc)
    {
        if (IsSorting || _context.IsPlayerInAction)
            return;

        IsSorting = true;
        _context.LogDebug("Sorting the storage...");

        //Use iterations to avoid deadlocks!
        const int maxIterations = 10;
        var iterations = 0;

        //Ignore items which move operations failed in the next iteration
        var blacklistedItems = new List<uint>(4);

        for (var iIteration = 0; iIteration < maxIterations; iIteration++)
        {
            iterations++;

            var itemsToStackGroups = this.Where(i =>
                    i.Record.IsStackable
                    && i.Record.MaxStack > i.Amount
                    && !blacklistedItems.Contains(i.ItemId)
                )
                .GroupBy(i => i.ItemId);

            if (!itemsToStackGroups.Any())
                break;

            var itemsToStack = itemsToStackGroups.FirstOrDefault(g => g.Count() >= 2)?.OrderBy(i => i.Slot).ToList();

            if (itemsToStack == null)
                break;

            var source = itemsToStack.FirstOrDefault();
            if (source == null)
                continue;

            var destination = itemsToStack.FirstOrDefault(i => i.Record.ID == source.ItemId && i.Slot != source.Slot);
            if (destination == null)
                continue;

            var amount = destination.Record.MaxStack - destination.Amount;
            var actualAmount = source.Amount > amount ? amount : source.Amount;

            if (!MoveItem(source.Slot, destination.Slot, (ushort)actualAmount, npc))
                blacklistedItems.Add(source.ItemId);
        }

        IsSorting = false;
        _context.LogDebug($"Sorting finished after {iterations}/{maxIterations}");
    }
}
