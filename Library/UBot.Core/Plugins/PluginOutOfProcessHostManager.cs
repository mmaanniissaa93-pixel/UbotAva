using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace UBot.Core.Plugins;

internal sealed class PluginOutOfProcessHostManager : IDisposable
{
    private readonly object _sync = new();
    private readonly Dictionary<string, OutOfProcHostEntry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private bool _disposed;

    internal void Register(string pluginName, string pluginPath, PluginContractManifest manifest, bool enabled)
    {
        if (string.IsNullOrWhiteSpace(pluginName) || string.IsNullOrWhiteSpace(pluginPath))
            return;

        lock (_sync)
        {
            if (_entries.TryGetValue(pluginName, out var existing))
            {
                existing.PluginPath = pluginPath;
                existing.Manifest = manifest;
                existing.DesiredEnabled = enabled;
                return;
            }

            _entries[pluginName] = new OutOfProcHostEntry
            {
                PluginName = pluginName,
                PluginPath = pluginPath,
                Manifest = manifest,
                DesiredEnabled = enabled
            };
        }
    }

    internal bool Enable(string pluginName)
    {
        OutOfProcHostEntry entry;
        lock (_sync)
        {
            if (!_entries.TryGetValue(pluginName, out entry))
                return false;

            entry.DesiredEnabled = true;
        }

        return StartHost(entry, ignoreAlreadyRunning: true);
    }

    internal bool Disable(string pluginName)
    {
        OutOfProcHostEntry entry;
        lock (_sync)
        {
            if (!_entries.TryGetValue(pluginName, out entry))
                return false;

            entry.DesiredEnabled = false;
        }

        return StopHost(entry);
    }

    internal bool StartRegisteredEnabledHosts(out string[] failedPlugins)
    {
        List<OutOfProcHostEntry> entries;
        lock (_sync)
            entries = new List<OutOfProcHostEntry>(_entries.Values);

        var failures = new List<string>();
        foreach (var entry in entries)
        {
            if (entry.DesiredEnabled)
            {
                if (!StartHost(entry, ignoreAlreadyRunning: true))
                    failures.Add(entry.PluginName);
            }
        }

        failedPlugins = failures.ToArray();
        return failedPlugins.Length == 0;
    }

    internal void Unregister(string pluginName)
    {
        OutOfProcHostEntry entry = null;
        lock (_sync)
        {
            if (_entries.TryGetValue(pluginName, out entry))
                _entries.Remove(pluginName);
        }

        if (entry != null)
            StopHost(entry);
    }

    internal object GetSnapshot()
    {
        lock (_sync)
        {
            var items = new List<object>(_entries.Count);
            foreach (var entry in _entries.Values)
            {
                items.Add(new
                {
                    plugin = entry.PluginName,
                    enabled = entry.DesiredEnabled,
                    running = entry.Process != null && !entry.Process.HasExited,
                    restartCount = entry.RestartCount,
                    lastExitCode = entry.LastExitCode,
                    lastExitAt = entry.LastExitAt
                });
            }

            return items.ToArray();
        }
    }

    private bool StartHost(OutOfProcHostEntry entry, bool ignoreAlreadyRunning)
    {
        lock (_sync)
        {
            if (_disposed)
                return false;

            if (!entry.DesiredEnabled)
                return false;

            if (entry.Process != null && !entry.Process.HasExited)
                return ignoreAlreadyRunning;

            var executablePath = Path.Combine(UBot.Core.RuntimeAccess.Core.BasePath, "UBot.exe");
            if (!File.Exists(executablePath))
            {
                Log.Error($"Out-of-proc plugin host executable not found: {executablePath}");
                return false;
            }

            if (!File.Exists(entry.PluginPath))
            {
                Log.Error($"Out-of-proc plugin assembly not found: {entry.PluginPath}");
                return false;
            }

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = executablePath,
                    Arguments =
                        $"--plugin-host --plugin-name \"{entry.PluginName}\" --plugin-path \"{entry.PluginPath}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = UBot.Core.RuntimeAccess.Core.BasePath
                };

                var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                process.Exited += (_, _) => HandleHostExit(entry.PluginName);
                if (!process.Start())
                    return false;

                entry.Process = process;
                Log.Notify($"Out-of-proc plugin host started [{entry.PluginName}] pid={process.Id}");
                return true;
            }
            catch (Exception ex)
            {
                Log.Error($"Failed to start out-of-proc host for plugin [{entry.PluginName}]: {ex.Message}");
                return false;
            }
        }
    }

    private bool StopHost(OutOfProcHostEntry entry)
    {
        Process process = null;
        lock (_sync)
        {
            if (entry.Process == null)
                return true;

            process = entry.Process;
            entry.Process = null;
        }

        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
            process.Dispose();
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to stop out-of-proc host [{entry.PluginName}]: {ex.Message}");
            return false;
        }
    }

    private void HandleHostExit(string pluginName)
    {
        OutOfProcHostEntry entry;
        int exitCode = 0;
        bool shouldRestart = false;
        PluginRestartPolicyManifest policy = null;

        lock (_sync)
        {
            if (!_entries.TryGetValue(pluginName, out entry))
                return;

            if (entry.Process != null)
            {
                try
                {
                    exitCode = entry.Process.ExitCode;
                }
                catch
                {
                    exitCode = -1;
                }

                try
                {
                    entry.Process.Dispose();
                }
                catch
                {
                    // ignored
                }
            }

            entry.Process = null;
            entry.LastExitCode = exitCode;
            entry.LastExitAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            policy = entry.Manifest?.Isolation?.RestartPolicy ?? new PluginRestartPolicyManifest();
            shouldRestart = entry.DesiredEnabled && policy.Enabled && ShouldAllowRestart(entry, policy);
        }

        if (!shouldRestart)
            return;

        var delay = CalculateRestartDelay(policy, entry.RestartCount);
        Task.Run(async () =>
        {
            if (delay > 0)
                await Task.Delay(delay);

            StartHost(entry, ignoreAlreadyRunning: false);
        });
    }

    private static int CalculateRestartDelay(PluginRestartPolicyManifest policy, int restartCount)
    {
        if (policy == null)
            return 0;

        var baseDelay = Math.Max(0, policy.BaseDelayMs);
        if (baseDelay == 0)
            return 0;

        var maxDelay = Math.Max(baseDelay, policy.MaxDelayMs);
        var exponent = Math.Max(0, restartCount - 1);
        var value = baseDelay * (int)Math.Pow(2, exponent);
        return Math.Min(value, maxDelay);
    }

    private static bool ShouldAllowRestart(OutOfProcHostEntry entry, PluginRestartPolicyManifest policy)
    {
        if (entry == null)
            return false;

        entry.CleanupRestartWindow(policy.WindowSeconds);

        if (entry.RestartsInWindow.Count >= Math.Max(0, policy.MaxRestarts))
            return false;

        entry.RestartsInWindow.Enqueue(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        entry.RestartCount++;
        return true;
    }

    public void Dispose()
    {
        List<OutOfProcHostEntry> entries;
        lock (_sync)
        {
            if (_disposed)
                return;

            _disposed = true;
            entries = new List<OutOfProcHostEntry>(_entries.Values);
            _entries.Clear();
        }

        foreach (var entry in entries)
            StopHost(entry);
    }

    private sealed class OutOfProcHostEntry
    {
        public string PluginName { get; set; } = string.Empty;
        public string PluginPath { get; set; } = string.Empty;
        public PluginContractManifest Manifest { get; set; }
        public bool DesiredEnabled { get; set; }
        public Process Process { get; set; }
        public int RestartCount { get; set; }
        public int LastExitCode { get; set; }
        public long LastExitAt { get; set; }
        public Queue<long> RestartsInWindow { get; } = new();

        public void CleanupRestartWindow(int windowSeconds)
        {
            var effectiveWindowMs = Math.Max(1, windowSeconds) * 1000L;
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            while (RestartsInWindow.Count > 0 && now - RestartsInWindow.Peek() > effectiveWindowMs)
                RestartsInWindow.Dequeue();
        }
    }
}
