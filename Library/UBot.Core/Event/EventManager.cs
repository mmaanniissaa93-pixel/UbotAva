using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace UBot.Core.Event;

public class EventManager
{
    private const int DispatchQueueMaxBacklog = 2500;
    private static readonly HashSet<string> DroppableNetworkEvents = new(StringComparer.OrdinalIgnoreCase)
    {
        "OnPlayerMove",
        "OnPlayerMoveAngle",
        "OnAddLog",
        "OnChangeStatusText"
    };

    private static readonly List<(string name, Delegate handler)> _listeners = new();
    private static readonly object _listenersLock = new();
    private static readonly ConcurrentQueue<QueuedInvocation> _dispatchQueue = new();
    private static readonly ConcurrentDictionary<object, List<(string name, Delegate handler)>> _ownerListeners = new();
    private static readonly SemaphoreSlim _dispatchSignal = new(0, int.MaxValue);
    private static int _dispatchQueueCount;
    private static int _droppedInvocationCount;
    private static int _lastLoggedDropBucket;

    static EventManager()
    {
        _ = Task.Run(ProcessDispatchQueueAsync);
    }

    /// <summary>
    ///     Registers the event.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="handler">The handler.</param>
    public static void SubscribeEvent(string name, Delegate handler)
    {
        if (handler == null)
            return;

        lock (_listenersLock)
        {
            var count = _listeners.Count;
            for (var i = 0; i < count; i++)
            {
                var listener = _listeners[i];
                if (listener.name == name && listener.handler.Equals(handler))
                    return;
            }

            _listeners.Add((name, handler));
        }
    }

    /// <summary>
    ///     Registers the event.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="handler">The handler.</param>
    public static void SubscribeEvent(string name, Action handler)
    {
        if (handler == null)
            return;

        lock (_listenersLock)
        {
            var count = _listeners.Count;
            for (var i = 0; i < count; i++)
            {
                var listener = _listeners[i];
                if (listener.name == name && listener.handler.Equals(handler))
                    return;
            }

            _listeners.Add((name, handler));
        }
    }

    /// <summary>
    ///     Registers the event with owner tracking for later cleanup.
    /// </summary>
    /// <param name="name">The event name.</param>
    /// <param name="handler">The handler delegate.</param>
    /// <param name="owner">The owner object for cleanup tracking.</param>
    public static void SubscribeEvent(string name, Delegate handler, object owner)
    {
        if (string.IsNullOrWhiteSpace(name) || handler == null || owner == null)
            return;

        lock (_listenersLock)
        {
            var count = _listeners.Count;
            for (var i = 0; i < count; i++)
            {
                var listener = _listeners[i];
                if (listener.name == name && listener.handler.Equals(handler))
                    return;
            }

            _listeners.Add((name, handler));

            if (!_ownerListeners.TryGetValue(owner, out var ownerList))
            {
                ownerList = new List<(string name, Delegate handler)>();
                _ownerListeners[owner] = ownerList;
            }

            ownerList.Add((name, handler));
        }
    }

    /// <summary>
    ///     Registers the event with owner tracking for later cleanup.
    /// </summary>
    /// <param name="name">The event name.</param>
    /// <param name="handler">The action handler.</param>
    /// <param name="owner">The owner object for cleanup tracking.</param>
    public static void SubscribeEvent(string name, Action handler, object owner)
    {
        if (string.IsNullOrWhiteSpace(name) || handler == null || owner == null)
            return;

        lock (_listenersLock)
        {
            var count = _listeners.Count;
            for (var i = 0; i < count; i++)
            {
                var listener = _listeners[i];
                if (listener.name == name && listener.handler.Equals(handler))
                    return;
            }

            _listeners.Add((name, handler));

            if (!_ownerListeners.TryGetValue(owner, out var ownerList))
            {
                ownerList = new List<(string name, Delegate handler)>();
                _ownerListeners[owner] = ownerList;
            }

            ownerList.Add((name, handler));
        }
    }

    /// <summary>
    ///     Unsubscribes the event.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="handler">The handler.</param>
    public static void UnsubscribeEvent(string name, Delegate handler)
    {
        if (string.IsNullOrWhiteSpace(name) || handler == null)
            return;

        lock (_listenersLock)
        {
            _listeners.RemoveAll(l => l.name == name && l.handler.Equals(handler));
        }
    }

    /// <summary>
    ///     Unsubscribes the event.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="handler">The handler.</param>
    public static void UnsubscribeEvent(string name, Action handler)
    {
        if (string.IsNullOrWhiteSpace(name) || handler == null)
            return;

        lock (_listenersLock)
        {
            _listeners.RemoveAll(l => l.name == name && l.handler.Equals(handler));
        }
    }

    /// <summary>
    ///     Unsubscribes all events owned by the specified owner.
    /// </summary>
    /// <param name="owner">The owner object to unsubscribe all events for.</param>
    public static void UnsubscribeOwner(object owner)
    {
        if (owner == null)
            return;

        if (!_ownerListeners.TryGetValue(owner, out var ownerList) || ownerList == null || ownerList.Count == 0)
            return;

        lock (_listenersLock)
        {
            foreach (var (eventName, handler) in ownerList.ToList())
            {
                _listeners.RemoveAll(l => l.name == eventName && l.handler.Equals(handler));
            }
        }

        _ownerListeners.TryRemove(owner, out _);
    }

    /// <summary>
    ///     Fires the event.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="parameters">The parameters.</param>
    public static void FireEvent(string name, params object[] parameters)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        Delegate[] targets;
        lock (_listenersLock)
        {
            var paramCount = parameters.Length;
            var count = _listeners.Count;
            var matches = new List<Delegate>(count);

            for (var i = 0; i < count; i++)
            {
                var listener = _listeners[i];
                if (listener.name == name && listener.handler.Method.GetParameters().Length == paramCount)
                    matches.Add(listener.handler);
            }

            targets = matches.Count > 0 ? matches.ToArray() : null;
        }

        if (targets == null)
            return;

        var isNetworkThread = Thread.CurrentThread.Name == "Network.PacketProcessor";

        foreach (var target in targets)
        {
            try
            {
                if (isNetworkThread)
                    EnqueueInvocation(name, target, parameters);
                else
                    target.DynamicInvoke(parameters);
            }
            catch (Exception e)
            {
                Log.Fatal(e);
            }
        }
    }

    private static void SafeInvoke(Delegate target, object[] parameters)
    {
        try
        {
            target.DynamicInvoke(parameters);
        }
        catch (Exception e)
        {
            Log.Fatal(e);
        }
    }

    private static void EnqueueInvocation(string eventName, Delegate target, object[] parameters)
    {
        var queueCount = Volatile.Read(ref _dispatchQueueCount);
        if (queueCount >= DispatchQueueMaxBacklog)
        {
            if (IsDroppableEvent(eventName))
            {
                Interlocked.Increment(ref _droppedInvocationCount);
                MaybeLogDroppedCount();
                return;
            }

            if (_dispatchQueue.TryDequeue(out _))
            {
                Interlocked.Decrement(ref _dispatchQueueCount);
                Interlocked.Increment(ref _droppedInvocationCount);
                MaybeLogDroppedCount();
            }
        }

        _dispatchQueue.Enqueue(new QueuedInvocation(eventName, target, parameters));
        Interlocked.Increment(ref _dispatchQueueCount);

        try
        {
            _dispatchSignal.Release();
        }
        catch (SemaphoreFullException)
        {
            // ignored
        }
    }

    private static async Task ProcessDispatchQueueAsync()
    {
        while (true)
        {
            try
            {
                await _dispatchSignal.WaitAsync().ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception e)
            {
                Log.Error($"EventManager dispatch signal error: {e.Message}");
                Thread.Sleep(100);
                continue;
            }

            while (_dispatchQueue.TryDequeue(out var invocation))
            {
                Interlocked.Decrement(ref _dispatchQueueCount);
                SafeInvoke(invocation.Target, invocation.Parameters);
            }
        }
    }

    private static bool IsDroppableEvent(string eventName)
    {
        return !string.IsNullOrWhiteSpace(eventName) && DroppableNetworkEvents.Contains(eventName);
    }

    private static void MaybeLogDroppedCount()
    {
        var dropped = Volatile.Read(ref _droppedInvocationCount);
        if (dropped <= 0)
            return;

        var bucket = dropped / 100;
        if (bucket <= 0 || bucket == Volatile.Read(ref _lastLoggedDropBucket))
            return;

        Interlocked.Exchange(ref _lastLoggedDropBucket, bucket);
        Log.Warn($"EventManager dropped {dropped} queued network callbacks due to queue pressure.");
    }

    /// <summary>
    ///     Gets the total number of event listeners currently subscribed.
    /// </summary>
    /// <returns>Total listener count.</returns>
    public static int GetListenerCount()
    {
        lock (_listenersLock)
        {
            return _listeners.Count;
        }
    }

    /// <summary>
    ///     Gets the number of listeners for a specific event name.
    /// </summary>
    /// <param name="eventName">The event name to count listeners for.</param>
    /// <returns>Listener count for the event.</returns>
    public static int GetListenerCount(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
            return 0;

        lock (_listenersLock)
        {
            var count = 0;
            var listenerCount = _listeners.Count;
            for (var i = 0; i < listenerCount; i++)
            {
                if (_listeners[i].name == eventName)
                    count++;
            }
            return count;
        }
    }

    /// <summary>
    ///     Gets the number of tracked owner subscriptions.
    /// </summary>
    /// <returns>Owner subscription count.</returns>
    public static int GetOwnerCount()
    {
        return _ownerListeners.Count;
    }

    /// <summary>
    ///     Gets all registered event names.
    /// </summary>
    /// <returns>Array of unique event names.</returns>
    public static string[] GetEventNames()
    {
        lock (_listenersLock)
        {
            var uniqueNames = new HashSet<string>();
            var count = _listeners.Count;
            for (var i = 0; i < count; i++)
            {
                uniqueNames.Add(_listeners[i].name);
            }
            var result = new string[uniqueNames.Count];
            uniqueNames.CopyTo(result);
            return result;
        }
    }

    private sealed class QueuedInvocation
    {
        public QueuedInvocation(string eventName, Delegate target, object[] parameters)
        {
            EventName = eventName ?? string.Empty;
            Target = target;
            Parameters = parameters ?? Array.Empty<object>();
        }

        public string EventName { get; }
        public Delegate Target { get; }
        public object[] Parameters { get; }
    }
}
