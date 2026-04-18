using System.Collections.Generic;
using UBot.Core.Client.ReferenceObjects;
using UBot.Core.Objects;

namespace UBot.Alchemy.Bundle.Magic;

internal class MagicBundleConfig
{
    #region Properties

    /// <summary>
    ///     Gets or sets the selected item
    /// </summary>
    public InventoryItem Item { get; set; }

    /// <summary>
    ///     Gets or sets a dictionary of inventory items and the referenced magic option
    /// </summary>
    public Dictionary<InventoryItem, RefMagicOpt> MagicStones { get; set; }

    /// <summary>
    ///     Gets or sets target values for each magic option group.
    /// </summary>
    public Dictionary<string, uint> TargetValues { get; set; }

    #endregion Properties
}
