using System;
using System.Collections.Generic;
using System.Threading;

namespace UBot.Core.Plugins;

internal sealed class PluginFaultIsolationManager
{
    private readonly object _sync = new();
    private readonly Dictionary<string, PluginFaultState> _states = new(StringComparer.OrdinalIgnoreCase);

    internal bool TryExecute(string pluginName, string actionName, PluginRestartPolicyManifest policy, Action action, out Exception failure)
    {
        failure = null;
        if (action == null)
        {
            failure = new InvalidOperationException($"Plugin [{pluginName}] action [{actionName}] is null.");
            return false;
        }

        var effectivePolicy = policy ?? new PluginRestartPolicyManifest();
        var maxRestarts = Math.Max(0, effectivePolicy.MaxRestarts);
        var maxAttempts = effectivePolicy.Enabled ? maxRestarts + 1 : 1;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                action();
                TrackSuccess(pluginName);
                return true;
            }
            catch (Exception ex)
            {
                failure = ex;
                TrackFailure(pluginName, actionName, ex);

                if (attempt >= maxAttempts)
                    return false;

                var delay = CalculateDelayMs(effectivePolicy, attempt);
                if (delay > 0)
                    Thread.Sleep(delay);
            }
        }

        return false;
    }

    internal object GetSnapshot()
    {
        lock (_sync)
        {
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var items = new List<object>(_states.Count);
            foreach (var entry in _states)
            {
                items.Add(new
                {
                    plugin = entry.Key,
                    lastError = entry.Value.LastError ?? string.Empty,
                    lastAction = entry.Value.LastAction ?? string.Empty,
                    failureCount = entry.Value.FailureCount,
                    successCount = entry.Value.SuccessCount,
                    lastFailureAt = entry.Value.LastFailureAt,
                    lastSuccessAt = entry.Value.LastSuccessAt,
                    msSinceLastFailure = entry.Value.LastFailureAt <= 0 ? -1 : now - entry.Value.LastFailureAt
                });
            }

            return items.ToArray();
        }
    }

    private void TrackSuccess(string pluginName)
    {
        lock (_sync)
        {
            var state = GetOrCreateState(pluginName);
            state.SuccessCount++;
            state.LastSuccessAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }
    }

    private void TrackFailure(string pluginName, string actionName, Exception ex)
    {
        lock (_sync)
        {
            var state = GetOrCreateState(pluginName);
            state.FailureCount++;
            state.LastFailureAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            state.LastAction = actionName ?? string.Empty;
            state.LastError = ex?.Message ?? "unknown_error";
        }
    }

    private PluginFaultState GetOrCreateState(string pluginName)
    {
        if (!_states.TryGetValue(pluginName ?? string.Empty, out var state))
        {
            state = new PluginFaultState();
            _states[pluginName ?? string.Empty] = state;
        }

        return state;
    }

    private static int CalculateDelayMs(PluginRestartPolicyManifest policy, int attempt)
    {
        var baseDelay = Math.Max(0, policy?.BaseDelayMs ?? 0);
        var maxDelay = Math.Max(baseDelay, policy?.MaxDelayMs ?? baseDelay);
        if (baseDelay == 0)
            return 0;

        var exp = Math.Min(maxDelay, baseDelay * (int)Math.Pow(2, Math.Max(0, attempt - 1)));
        return exp;
    }

    private sealed class PluginFaultState
    {
        public int FailureCount;
        public int SuccessCount;
        public long LastFailureAt;
        public long LastSuccessAt;
        public string LastError;
        public string LastAction;
    }
}
