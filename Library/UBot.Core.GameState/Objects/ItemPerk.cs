#nullable enable annotations

using System.ComponentModel.DataAnnotations;
using UBot.Core.Client.ReferenceObjects;
using UBot.Core.Abstractions;
using UBot.GameData.ReferenceObjects;

namespace UBot.Core.Objects;

public class ItemPerk
{
    #region Properties

    [Required]
    public uint ItemId { get; init; }

    [Required]
    public uint Token { get; init; }

    public uint Value { get; set; }

    public uint RemainingTime { get; set; }

    public RefObjItem? Item => GameStateRuntimeProvider.Instance?.GetReference("RefItem", ItemId) as RefObjItem;

    #endregion Properties
}
