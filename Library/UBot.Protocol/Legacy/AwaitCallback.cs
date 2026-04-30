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
            UBot.Protocol.ProtocolRuntime.RemoveCallback(this);
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
                UBot.Protocol.ProtocolRuntime.RemoveCallback(this);
                Log.Debug($"Callback timeout, ResponseOpcode: 0x{ResponseOpcode:X}");
            }
        }
    }

    private void Complete(bool succeeded)
    {
        _succeeded = succeeded;
        _invoked = true;
        _completionSource.TrySetResult(succeeded);
        UBot.Protocol.ProtocolRuntime.RemoveCallback(this);
    }
}
