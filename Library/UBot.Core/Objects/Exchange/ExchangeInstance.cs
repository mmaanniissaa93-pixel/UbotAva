using System.Collections.Generic;
using UBot.Core.Abstractions;
using UBot.Core.Objects.Spawn;

namespace UBot.Core.Objects.Exchange;

public class ExchangeInstance
{
    #region Fields

    private readonly IGameStateRuntimeContext _context;
    private readonly uint _exchangePlayerUniqueId;

    #endregion Fields

    /// <summary>
    ///     Initializes a new instance of the <see cref="ExchangeInstance" /> class.
    /// </summary>
    /// <param name="exchangePlayerUniqueId">The exchange player unique identifier.</param>
    public ExchangeInstance(uint exchangePlayerUniqueId, IGameStateRuntimeContext context = null)
    {
        _exchangePlayerUniqueId = exchangePlayerUniqueId;
        _context = context ?? GameStateRuntimeProvider.Instance;
    }

    /// <summary>
    ///     Gets the receiving items.
    /// </summary>
    /// <value>
    ///     The receiving items.
    /// </value>
    public List<ExchangeItem> ReceivingItems { get; private set; }

    /// <summary>
    ///     Gets the sending items.
    /// </summary>
    /// <value>
    ///     The sending items.
    /// </value>
    public List<ExchangeItem> SendingItems { get; private set; }

    /// <summary>
    ///     Gets the exchange player.
    /// </summary>
    /// <value>
    ///     The exchange player.
    /// </value>
    public SpawnedPlayer ExchangePlayer => _context.GetEntity(typeof(SpawnedPlayer), _exchangePlayerUniqueId) as SpawnedPlayer;

    internal void SetItems(bool playerIsSender, List<ExchangeItem> items)
    {
        if (playerIsSender)
            SendingItems = items;
        else
            ReceivingItems = items;
    }

    /// <summary>
    ///     Completes the exchange request. It updates the inventory item by the temporary stored information.
    /// </summary>
    public void Complete()
    {
        if (ReceivingItems == null || _context.Player is not Player player)
            return;

        foreach (var item in ReceivingItems)
        {
            item.Item.Slot = player.Inventory.GetFreeSlot();
            player.Inventory.Add(item.Item);
        }

        if (SendingItems != null)
            foreach (var item in SendingItems)
                player.Inventory.RemoveAt(item.SourceSlot);
    }
}
