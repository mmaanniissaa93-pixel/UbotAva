using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UBot.Core.Network;

namespace UBot.Protocol.Legacy;

public enum AwaitCallbackResult
{
    ConditionFailed = 0,
    Success,
    Fail,
}

public delegate AwaitCallbackResult AwaitCallbackPredicate(Packet packet);

public sealed class AwaitCallback
{
    private const int TimeoutDefault = 5_000;
    private readonly TaskCompletionSource<bool> _completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly AwaitCallbackPredicate _predicate;
    private volatile bool _invoked;
    private volatile bool _succeeded;
    private volatile bool _timeout;
    private int _waited;

    public AwaitCallback(AwaitCallbackPredicate predicate, ushort responseOpcode)
    {
        _predicate = predicate;
        ResponseOpcode = responseOpcode;
    }

    public ushort ResponseOpcode { get; }
    public bool IsCompleted => !_timeout && _invoked && _succeeded;
    public bool IsClosed => _timeout || _invoked;

    internal void Invoke(Packet packet)
    {
        if (_predicate == null)
        {
            Complete(true);
            return;
        }

        AwaitCallbackResult result;
        try
        {
            result = _predicate(packet);
        }
        catch (Exception ex)
        {
            Log.Debug($"Callback predicate threw an exception: {ex.Message}\n{ex.StackTrace}");
            result = AwaitCallbackResult.Fail;
        }

        switch (result)
        {
            case AwaitCallbackResult.Success:
                Complete(true);
                break;
            case AwaitCallbackResult.Fail:
                Complete(false);
                break;
        }
    }

    public void AwaitResponse(int milliseconds = TimeoutDefault)
    {
        if (Interlocked.CompareExchange(ref _waited, 1, 0) != 0)
            return;

        if (!_completionSource.Task.Wait(Math.Max(1, milliseconds)))
        {
            _timeout = true;
            PacketManager.RemoveCallback(this);
            Log.Debug($"Callback timeout, ResponseOpcode: 0x{ResponseOpcode:X}");
        }
    }

    public async Task AwaitResponseAsync(int milliseconds = TimeoutDefault, CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref _waited, 1, 0) != 0)
            return;

        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(Math.Max(1, milliseconds));
            await _completionSource.Task.WaitAsync(timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                _timeout = true;
                PacketManager.RemoveCallback(this);
                Log.Debug($"Callback timeout, ResponseOpcode: 0x{ResponseOpcode:X}");
            }
        }
    }

    private void Complete(bool succeeded)
    {
        _succeeded = succeeded;
        _invoked = true;
        _completionSource.TrySetResult(succeeded);
        PacketManager.RemoveCallback(this);
    }
}

public static class PacketManager
{
    private static readonly object Lock = new();
    private static readonly List<AwaitCallback> Callbacks = new();

    public static void SendPacket(Packet packet, PacketDestination destination, params AwaitCallback[] callbacks)
    {
        if (callbacks != null && callbacks.Length > 0)
        {
            lock (Lock)
            {
                Callbacks.RemoveAll(callback => callback == null || callback.IsClosed);
                foreach (var callback in callbacks.Where(callback => callback != null && !callback.IsClosed && !Callbacks.Contains(callback)))
                    Callbacks.Add(callback);
            }
        }

        ProtocolRuntime.Dispatch(packet, destination);
    }

    public static void CallCallback(Packet packet)
    {
        if (packet == null)
            return;

        AwaitCallback[] callbacks;
        lock (Lock)
        {
            Callbacks.RemoveAll(callback => callback == null || callback.IsClosed);
            callbacks = Callbacks.Where(callback => callback.ResponseOpcode == packet.Opcode).ToArray();
        }

        foreach (var callback in callbacks)
        {
            packet.SeekRead(0, System.IO.SeekOrigin.Begin);
            callback.Invoke(packet);
        }

        lock (Lock)
        {
            Callbacks.RemoveAll(callback => callback == null || callback.IsClosed);
        }
    }

    internal static void RemoveCallback(AwaitCallback callback)
    {
        lock (Lock)
        {
            Callbacks.Remove(callback);
        }
    }
}
