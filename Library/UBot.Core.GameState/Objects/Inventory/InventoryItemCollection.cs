using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace UBot.Core.Objects;

public class InventoryItemCollection : ICollection<InventoryItem>
{
    protected List<InventoryItem> _collection;

    public InventoryItemCollection(byte size)
    {
        _collection = new List<InventoryItem>(size + 1);
    }

    public byte FreeSlots => (byte)(Capacity - Count);

    public byte Capacity
    {
        get => (byte)(_collection.Capacity - 1);
        set => _collection.Capacity = value + 1;
    }

    public virtual bool Full => Count >= Capacity;

    public InventoryItem this[int index]
    {
        get => _collection[index];
        set => _collection[index] = value;
    }

    public int Count => _collection.Count;
    public bool IsReadOnly => false;

    public void Add(InventoryItem newItem)
    {
        _collection.Add(newItem);
    }

    public bool Remove(InventoryItem item)
    {
        return _collection.Remove(item);
    }

    public bool Contains(InventoryItem item)
    {
        return _collection.Contains(item);
    }

    public void CopyTo(InventoryItem[] array, int arrayIndex)
    {
        _collection.CopyTo(array, arrayIndex);
    }

    public void Clear()
    {
        _collection.Clear();
    }

    public IEnumerator<InventoryItem> GetEnumerator()
    {
        return _collection.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _collection.GetEnumerator();
    }

    public void RemoveAt(byte slot)
    {
        _collection.RemoveAll(p => p.Slot == slot);
    }

    public InventoryItem GetItemAt(byte slot)
    {
        return GetItem(item => item?.Slot == slot);
    }

    public bool Contains(byte slot)
    {
        return GetItem(item => item.Slot == slot) != null;
    }

    public void UpdateItemSlot(byte slot, byte newSlot)
    {
        if (GetItemAt(slot) is InventoryItem itemToUpdate)
            itemToUpdate.Slot = newSlot;
    }

    public void UpdateItemAmount(byte slot, ushort newAmount)
    {
        if (newAmount <= 0)
        {
            RemoveAt(slot);
            return;
        }

        if (GetItemAt(slot) is InventoryItem itemToUpdate)
            itemToUpdate.Amount = newAmount;
    }

    protected void OrderBySlot()
    {
        _collection.Sort((a, b) => a.Slot.CompareTo(b.Slot));
    }

    public IList<InventoryItem> GetItems(Predicate<InventoryItem> predicate)
    {
        return _collection.FindAll(predicate);
    }

    public ICollection<InventoryItem> GetItems(uint objId)
    {
        return GetItems(item => item.Record.ID == objId);
    }

    public ICollection<InventoryItem> GetItems(string recordCodeName)
    {
        return GetItems(item => item.Record.CodeName == recordCodeName);
    }

    public ICollection<InventoryItem> GetItems(TypeIdFilter filter)
    {
        return GetItems(item => filter.EqualsRefItem(item.Record));
    }

    public ICollection<InventoryItem> GetItems(TypeIdFilter filter, Predicate<InventoryItem> action)
    {
        return GetItems(item => filter.EqualsRefItem(item.Record) && action(item));
    }

    public InventoryItem GetItem(Predicate<InventoryItem> predicate)
    {
        return GetItems(predicate).FirstOrDefault();
    }

    public InventoryItem GetItem(uint objId)
    {
        return GetItem(item => item.Record.ID == objId);
    }

    public InventoryItem GetItem(TypeIdFilter filter)
    {
        return GetItem(item => filter.EqualsRefItem(item.Record));
    }

    public InventoryItem GetItem(TypeIdFilter filter, Predicate<InventoryItem> action)
    {
        return GetItem(item => filter.EqualsRefItem(item.Record) && action(item));
    }

    public InventoryItem GetItem(string recordCodeName)
    {
        return GetItem(item => item.Record.CodeName == recordCodeName);
    }

    public int GetSumAmount(string recordCodeName)
    {
        return GetItems(recordCodeName).Aggregate(0, (current, item) => current + item.Amount);
    }

    public int GetSumAmount(TypeIdFilter filter)
    {
        var sum = 0;
        foreach (var item in GetItems(filter))
            sum += item.Amount;

        return sum;
    }

    public InventoryItem GetItemBest(TypeIdFilter filter)
    {
        InventoryItem nearestItem = null;
        foreach (var item in GetItems(filter))
            if (nearestItem == null)
            {
                nearestItem = item;
            }
            else
            {
                if (
                    item.Record.ReqLevel1 > nearestItem.Record.ReqLevel1
                    && item.OptLevel >= nearestItem.OptLevel
                    && item.CanBeEquipped()
                )
                    nearestItem = item;
            }

        return nearestItem;
    }

    public virtual byte GetFreeSlot()
    {
        for (byte slot = 0; slot < Capacity; slot++)
            if (GetItemAt(slot) == null)
                return slot;

        return 0xFF;
    }

    public void Move(byte sourceSlot, byte destinationSlot, ushort amount)
    {
        var itemAtSource = GetItemAt(sourceSlot);
        if (itemAtSource == null)
            return;

        var itemAtDestination = GetItemAt(destinationSlot);
        if (itemAtDestination == null)
        {
            if (itemAtSource.Amount != amount && amount != 0)
            {
                itemAtSource.Amount -= amount;
                var newInventoryItem = itemAtSource.Clone();
                newInventoryItem.Slot = destinationSlot;
                newInventoryItem.Amount = amount;
                Add(newInventoryItem);
                return;
            }

            itemAtSource.Slot = destinationSlot;
            return;
        }

        if (itemAtDestination.ItemId == itemAtSource.ItemId)
        {
            var newItemAmount = itemAtDestination.Amount + amount;
            if (newItemAmount > itemAtDestination.Record.MaxStack)
            {
                itemAtDestination.Slot = sourceSlot;
                itemAtSource.Slot = destinationSlot;
            }
            else
            {
                itemAtDestination.Amount = (ushort)newItemAmount;
                itemAtSource.Amount -= amount;

                if (itemAtSource.Amount <= 0)
                    RemoveAt(sourceSlot);
            }
        }
        else
        {
            itemAtDestination.Slot = sourceSlot;
            itemAtSource.Slot = destinationSlot;
        }
    }

    public void MoveTo(InventoryItemCollection inventory, byte sourceSlot, byte destinationSlot)
    {
        var sourceItem = GetItemAt(sourceSlot);
        if (sourceItem == null)
            return;

        RemoveAt(sourceSlot);
        sourceItem.Slot = destinationSlot;
        inventory.Add(sourceItem);
    }
}
