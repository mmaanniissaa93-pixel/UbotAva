#nullable enable annotations

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UBot.Core.Abstractions;
using UBot.Core.Client.ReferenceObjects;
using UBot.GameData.ReferenceObjects;
using UBot.Core.Network;
using UBot.Core.Objects.Inventory;
using UBot.Core.Objects.Item;

namespace UBot.Core.Objects;

public class InventoryItem
{
    private readonly IGameStateRuntimeContext _context;
    private byte _optLevel;

    public InventoryItem(IGameStateRuntimeContext context = null)
    {
        _context = context ?? GameStateRuntimeProvider.Instance;
    }

    /// <summary>
    ///     Gets or sets the state.
    /// </summary>
    /// <value>
    ///     The state.
    /// </value>
    public InventoryItemCosInfo Cos;

    /// <summary>
    ///     Gets or sets the item identifier.
    /// </summary>
    /// <value>
    ///     The item identifier.
    /// </value>
    public uint ItemId { get; set; }

    /// <summary>
    ///     Gets or sets the slot.
    /// </summary>
    /// <value>
    ///     The slot.
    /// </value>
    public byte Slot { get; set; }

    /// <summary>
    ///     Gets or sets the rental.
    /// </summary>
    /// <value>
    ///     The rental.
    /// </value>
    public RentInfo Rental { get; set; }

    /// <summary>
    ///     Gets or sets the record.
    /// </summary>
    /// <value>
    ///     The record.
    /// </value>
    public RefObjItem Record => _context.GetReference("RefItem", ItemId) as RefObjItem;

    /// <summary>
    ///     Gets or sets the opt level.
    /// </summary>
    /// <value>
    ///     The opt level.
    /// </value>
    public byte OptLevel
    {
        get
        {
            if (BindingOptions == null)
                return _optLevel;

            var advancedElixirOptLevel = BindingOptions
                .Where(b => b.Type == BindingOptionType.AdvancedElixir)
                .Sum(b => b.Value);

            if (_optLevel + advancedElixirOptLevel > byte.MaxValue)
                return byte.MaxValue;

            return (byte)(_optLevel + advancedElixirOptLevel);
        }
        set => _optLevel = value;
    }

    /// <summary>
    ///     Gets or sets the variance.
    /// </summary>
    /// <value>
    ///     The variance.
    /// </value>
    public ItemAttributesInfo Attributes { get; set; }

    /// <summary>
    ///     Gets or sets the data.
    /// </summary>
    /// <value>
    ///     The data.
    /// </value>
    public uint Durability { get; set; }

    /// <summary>
    ///     Gets or sets the magic options.
    /// </summary>
    /// <value>
    ///     The magic options.
    /// </value>
    public List<MagicOptionInfo> MagicOptions { get; set; }

    /// <summary>
    ///     Gets or sets the options.
    /// </summary>
    /// <value>
    ///     The options.
    /// </value>
    public List<BindingOption> BindingOptions { get; set; }

    /// <summary>
    ///     Gets or sets the amount.
    /// </summary>
    /// <value>
    ///     The amount.
    /// </value>
    public ushort Amount { get; set; }

    /// <summary>
    ///     Gets or sets the state.
    /// </summary>
    /// <value>
    ///     The state.
    /// </value>
    public InventoryItemState State { get; set; }

    /// <summary>
    ///     Gets a value indicating whether [item skill in use].
    /// </summary>
    /// <value>
    ///     <c>true</c> if [item skill in use]; otherwise, <c>false</c>.
    /// </value>
    public bool ItemSkillInUse
    {
        get
        {
            //ToDo: Refine this whole check to act 1:1 like the client. I think Action_Overlap is a bitmap and not an actual value to work with.
            var refSkill = GetRefSkill();

            if (_context.Player is not Player player)
                return false;

            if (refSkill != null)
                return player.State.ActiveBuffs.FirstOrDefault(b =>
                        b.Record.ID == refSkill.ID || b.Record.Action_Overlap == refSkill.Action_Overlap
                    ) != null;

            var perk = player.State.ActiveItemPerks.Values.FirstOrDefault(p =>
                p.ItemId == Record.ID || (Record.Param1 > 0 && p.Item.Param1 == Record.Param1)
            );

            return perk != null;
        }
    }

    /// <summary>
    ///     Uses the item
    /// </summary>
    /// <returns></returns>
    public bool Use()
    {
        Log.Debug($"Using item tid: 0x{Record.Tid:x2} {Record.CodeName} {Record}");

        return _context.SendUseInventoryItem(Slot, Record.Tid);
    }

    /// <summary>
    ///     Use the item for destination item
    /// </summary>
    /// <param name="destinationSlot">The destination item slot</param>
    public bool UseTo(byte destinationSlot, int mapId = -1)
    {
        return _context.SendUseInventoryItemTo(Slot, Record.Tid, destinationSlot, mapId);
    }

    /// <summary>
    ///     Use the item for destination item
    /// </summary>
    /// <param name="destinationSlot">The destination item slot</param>
    public void UseFor(uint uniqueId)
    {
        _context.SendUseInventoryItemFor(Slot, Record.Tid, uniqueId);
    }

    /// <summary>
    ///     Equip the item
    /// </summary>
    /// <param name="slot">The slot</param>
    public bool Equip(byte slot)
    {
        var attempt = 0;
        while (
            _context.Player is Player player
            && !player.Inventory.MoveItem(Slot, slot)
            && _context.IsBotRunning
            && player.State.ScrollState == ScrollState.Cancel
        )
        {
            if (attempt++ > 5)
                return false;

            Thread.Sleep(250);
        }

        return true;
    }

    /// <summary>
    ///     Drop the item
    /// </summary>
    public bool Drop(bool cos = false, uint? cosUniqueId = 0)
    {
        if (Record.CanDrop == ObjectDropType.No)
            return false;

        return _context.SendDropInventoryItem(Slot, cos, cosUniqueId);
    }

    /// <summary>
    ///     Gets the filter.
    /// </summary>
    /// <returns></returns>
    public TypeIdFilter GetFilter()
    {
        return new TypeIdFilter(Record.TypeID1, Record.TypeID2, Record.TypeID3, Record.TypeID4);
    }

    public override string ToString()
    {
        return Record.CodeName;
    }

    public bool CanBeEquipped()
    {
        if (_context.Player is not Player player)
            return false;

        if (Record.IsAmmunition)
            return true;
        if (!Record.IsEquip)
            return false;
        if (Record.ReqLevel1 > player.Level)
            return false;
        if (Record.ReqGender != 2 && Record.ReqGender != (byte)player.Gender)
            return false;
        if (Record.Country != player.Record.Country)
            return false;

        return true;
    }

    public bool HasAbility(out RefAbilityByItemOptLevel abilityItem)
    {
        abilityItem = _context.GetReference("AbilityItem", (ItemId, OptLevel)) as RefAbilityByItemOptLevel;

        return abilityItem != null;
    }

    public bool HasExtraAbility(out IEnumerable<RefExtraAbilityByEquipItemOptLevel> abilityItems)
    {
        abilityItems =
            _context.GetReference("ExtraAbilityItems", (ItemId, OptLevel))
            as IEnumerable<RefExtraAbilityByEquipItemOptLevel>;

        return abilityItems != null;
    }

    public override bool Equals(object obj)
    {
        if (obj is TypeIdFilter filter)
            return filter.EqualsRefItem(Record);

        return false;
    }

    public RefSkill? GetRefSkill()
    {
        if (string.IsNullOrEmpty(Record.Desc1))
            return null;

        return _context.GetReference("RefSkill", Record.Desc1) as RefSkill;
    }

    public InventoryItem Clone()
    {
        return (InventoryItem)MemberwiseClone();
    }

    public override int GetHashCode()
    {
        return Record.GetHashCode();
    }
}
