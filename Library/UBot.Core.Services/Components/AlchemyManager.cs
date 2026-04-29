#nullable enable annotations

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UBot.Core.Abstractions.Services;
using UBot.Core.Client.ReferenceObjects;
using UBot.Core.Network;
using UBot.Core.Objects;
using UBot.Core.Services;

namespace UBot.Core.Components;

/// <summary>
///     Coordinates elixir and stone alchemy without depending on UI or Core statics.
/// </summary>
public static class AlchemyManager
{
    private static IAlchemyService _service = new AlchemyService();

    public static List<InventoryItem>? ActiveAlchemyItems
    {
        get => _service.ActiveAlchemyItems?.OfType<InventoryItem>().ToList();
        set
        {
            if (_service is AlchemyService service)
                service.SetActiveItems(value);
        }
    }

    public static bool IsFusing
    {
        get => _service.IsFusing;
        set
        {
            if (_service is AlchemyService service)
                service.SetFusing(value);
        }
    }

    public static AlchemyOperationState State => _service.State;

    public static void Initialize()
    {
        Initialize(new AlchemyService());
    }

    public static void Initialize(IAlchemyService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        ServiceRuntime.Alchemy = _service;
        ServiceRuntime.Log?.Debug("Initialized [AlchemyManager]!");
    }

    public static void CancelPending() => _service.CancelPending();

    public static bool TryFuseElixir(InventoryItem item, InventoryItem elixir, InventoryItem? powder)
    {
        return _service.TryFuseElixir(item, elixir, powder);
    }

    public static bool TryFuseMagicStone(InventoryItem item, InventoryItem magicStone)
    {
        return _service.TryFuseMagicStone(item, magicStone);
    }

    public static bool TryFuseAttributeStone(InventoryItem item, InventoryItem attributeStone)
    {
        return _service.TryFuseAttributeStone(item, attributeStone);
    }

    public static void BeginFuseRequest(AlchemyAction action, AlchemyType type, IReadOnlyList<InventoryItem> items)
    {
        _service.BeginFuseRequest(items?.Cast<object>().ToList() ?? new List<object>(), action, type);
    }

    public static void MarkCanceled(AlchemyType type) => _service.MarkCanceled(type);

    public static void MarkDestroyed(InventoryItem oldItem, AlchemyType type) => _service.MarkDestroyed(oldItem, type);

    public static void MarkError(ushort errorCode, AlchemyType type) => _service.MarkError(errorCode, type);

    public static void MarkResult(bool success, InventoryItem oldItem, InventoryItem newItem, AlchemyType type)
    {
        _service.MarkResult(success, oldItem, newItem, type);
    }

    public static void ClearActiveItems() => _service.ClearActiveItems();
}

public sealed class AlchemyService : IAlchemyService
{
    private const int FuseTimeoutMilliseconds = 10_000;
    private readonly object _sync = new();
    private CancellationTokenSource? _fusingTimeout;
    private List<object>? _activeAlchemyItems;

    public IReadOnlyList<object> ActiveAlchemyItems => _activeAlchemyItems;

    public bool IsFusing { get; private set; }

    public AlchemyOperationState State { get; private set; } = AlchemyOperationState.Idle;

    public void CancelPending()
    {
        var packet = new Packet(0x7150);
        packet.WriteByte(0x01);
        packet.Lock();

        Report(AlchemyOperationState.Canceled, 10, "[Alchemy] Canceling pending alchemy operation.");
        Runtime?.SendToServer(packet);
    }

    public bool TryFuseElixir(object item, object elixir, object powder = null)
    {
        if (item is not InventoryItem targetItem || elixir is not InventoryItem elixirItem)
            return false;

        var powderItem = powder as InventoryItem;
        var itemInInventory = GetInventoryItem(targetItem.Slot);
        var elixirInInventory = GetInventoryItem(elixirItem.Slot);
        var powderInInventory = powderItem != null ? GetInventoryItem(powderItem.Slot) : null;
        var isProofItem = powderInInventory != null
            && new TypeIdFilter(3, 3, 10, 8).EqualsRefItem(powderInInventory.Record);
        var alchemyType = isProofItem ? AlchemyType.EnhancerElixir : AlchemyType.Elixir;

        Report(AlchemyOperationState.Validating, 5, "[Alchemy] Validating elixir fusion request.", alchemyType);

        if (
            itemInInventory?.ItemId != targetItem.ItemId
            || elixirInInventory?.ItemId != elixirItem.ItemId
            || (powderItem != null && powderInInventory?.ItemId != powderItem.ItemId)
        )
        {
            WarnMismatch();
            return false;
        }

        var message = powderItem == null
            ? $"[Alchemy] Fusing elixir {elixirItem.Record.GetRealName()} to {targetItem.Record.GetRealName()}"
            : $"[Alchemy] Fusing elixir {elixirItem.Record.GetRealName()} to {targetItem.Record.GetRealName()} using powder {powderItem.Record.GetRealName()}";
        ServiceRuntime.Log?.Notify(message);
        Report(AlchemyOperationState.PacketPrepared, 25, message, alchemyType);

        var packet = new Packet(0x7150);
        packet.WriteByte(AlchemyAction.Fuse);
        packet.WriteByte(alchemyType);
        packet.WriteByte(powderItem != null ? (byte)3 : (byte)2);
        packet.WriteByte(targetItem.Slot);
        packet.WriteByte(elixirItem.Slot);

        if (powderItem != null)
            packet.WriteByte(powderItem.Slot);

        packet.Lock();
        Runtime?.SendToServer(packet);
        BeginFuseRequest(BuildItems(targetItem, elixirItem, powderItem), AlchemyAction.Fuse, alchemyType);

        return true;
    }

    public bool TryFuseMagicStone(object item, object magicStone)
    {
        return TryFuseStone(item, magicStone, AlchemyType.MagicStone, "magic stone");
    }

    public bool TryFuseAttributeStone(object item, object attributeStone)
    {
        return TryFuseStone(item, attributeStone, AlchemyType.AttributeStone, "attribute stone");
    }

    public void BeginFuseRequest(IReadOnlyList<object> items, AlchemyAction action, AlchemyType type)
    {
        lock (_sync)
        {
            _activeAlchemyItems = items?.Where(item => item != null).ToList() ?? new List<object>();
            IsFusing = true;
            State = AlchemyOperationState.WaitingForAck;
            RestartTimeout(type);
        }

        Runtime?.FireEvent("OnFuseRequest", action, type);
        Report(AlchemyOperationState.WaitingForAck, 45, "[Alchemy] Fuse request sent, waiting for result.", type);
    }

    public void MarkCanceled(AlchemyType type)
    {
        Finish(AlchemyOperationState.Canceled, 0, "[Alchemy] Alchemy operation canceled.", type, true);
    }

    public void MarkDestroyed(object oldItem, AlchemyType type)
    {
        var name = GetItemName(oldItem);
        Finish(AlchemyOperationState.Destroyed, 100, $"[Alchemy] Item destroyed: {name}", type, true);
    }

    public void MarkError(ushort errorCode, AlchemyType type)
    {
        Finish(AlchemyOperationState.Error, 0, $"[Alchemy] Alchemy fusion error: {errorCode:X}", type, true);
    }

    public void MarkResult(bool success, object oldItem, object newItem, AlchemyType type)
    {
        var state = success ? AlchemyOperationState.Succeeded : AlchemyOperationState.Failed;
        var message = success
            ? $"[Alchemy] Successful: {GetItemName(newItem)}"
            : $"[Alchemy] Failed: {GetItemName(oldItem)}";
        Finish(state, 100, message, type, true);
    }

    public void ClearActiveItems()
    {
        lock (_sync)
        {
            _activeAlchemyItems = null;
        }
    }

    internal void SetActiveItems(IEnumerable<InventoryItem>? items)
    {
        lock (_sync)
        {
            _activeAlchemyItems = items?.Cast<object>().ToList();
        }
    }

    internal void SetFusing(bool value)
    {
        lock (_sync)
        {
            IsFusing = value;
            if (!value)
                StopTimeout();
            else if (State == AlchemyOperationState.Idle)
                State = AlchemyOperationState.WaitingForAck;
        }
    }

    private bool TryFuseStone(object item, object stone, AlchemyType type, string label)
    {
        if (item is not InventoryItem targetItem || stone is not InventoryItem stoneItem)
            return false;

        var itemInInventory = GetInventoryItem(targetItem.Slot);
        var stoneInInventory = GetInventoryItem(stoneItem.Slot);

        Report(AlchemyOperationState.Validating, 5, $"[Alchemy] Validating {label} fusion request.", type);

        if (itemInInventory?.ItemId != targetItem.ItemId || stoneInInventory?.ItemId != stoneItem.ItemId)
        {
            WarnMismatch();
            return false;
        }

        var message = $"[Alchemy] Fusing {label} {stoneItem.Record.GetRealName()} to item {targetItem.Record.GetRealName()}";
        ServiceRuntime.Log?.Notify(message);
        Report(AlchemyOperationState.PacketPrepared, 25, message, type);

        var packet = new Packet(0x7151);
        packet.WriteByte(AlchemyAction.Fuse);
        packet.WriteByte(type);
        packet.WriteByte(2);
        packet.WriteByte(targetItem.Slot);
        packet.WriteByte(stoneItem.Slot);
        packet.Lock();

        Runtime?.SendToServer(packet);
        BeginFuseRequest(BuildItems(targetItem, stoneItem), AlchemyAction.Fuse, type);

        return true;
    }

    private void Finish(AlchemyOperationState state, int percent, string message, AlchemyType type, bool clearItems)
    {
        lock (_sync)
        {
            StopTimeout();
            IsFusing = false;
            State = state;
            if (clearItems)
                _activeAlchemyItems = null;
        }

        Report(state, percent, message, type);
    }

    private void RestartTimeout(AlchemyType type)
    {
        StopTimeout();
        _fusingTimeout = new CancellationTokenSource();
        _ = WatchFuseTimeoutAsync(_fusingTimeout.Token, type);
    }

    private void StopTimeout()
    {
        _fusingTimeout?.Cancel();
        _fusingTimeout?.Dispose();
        _fusingTimeout = null;
    }

    private async Task WatchFuseTimeoutAsync(CancellationToken token, AlchemyType type)
    {
        try
        {
            await Task.Delay(FuseTimeoutMilliseconds, token).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        lock (_sync)
        {
            if (!IsFusing)
                return;

            IsFusing = false;
            State = AlchemyOperationState.TimedOut;
        }

        Report(AlchemyOperationState.TimedOut, 0, "[Alchemy] Fusion timed out.", type);
    }

    private InventoryItem? GetInventoryItem(byte slot)
    {
        return Runtime?.GetInventoryItemAt(slot) as InventoryItem;
    }

    private static List<object> BuildItems(params InventoryItem?[] items)
    {
        return items.Where(item => item != null).Cast<object>().ToList();
    }

    private static string GetItemName(object item)
    {
        return item is InventoryItem inventoryItem
            ? inventoryItem.Record?.GetRealName() ?? inventoryItem.Record?.CodeName ?? inventoryItem.ItemId.ToString()
            : item?.ToString() ?? "unknown";
    }

    private static void WarnMismatch()
    {
        const string message = "[Alchemy] Requested to fuse an item that does not match the current item at the specified slot.";
        ServiceRuntime.Log?.Warn(message);
        Report(AlchemyOperationState.Failed, 0, message);
    }

    private static void Report(AlchemyOperationState state, int percent, string message, AlchemyType? type = null)
    {
        ServiceRuntime.AlchemyProgress?.Report(new AlchemyProgressUpdate(state, percent, message, type));
    }

    private static IAlchemyRuntime? Runtime => ServiceRuntime.AlchemyRuntime;
}
