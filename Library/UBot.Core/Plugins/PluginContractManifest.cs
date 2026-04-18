using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace UBot.Core.Plugins;

public sealed class PluginContractException : Exception
{
    public PluginContractException(string message) : base(message)
    {
    }

    public PluginContractException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public sealed class PluginContractManifest
{
    public int SchemaVersion { get; set; } = 1;
    public string PluginName { get; set; } = string.Empty;
    public string PluginVersion { get; set; } = string.Empty;
    public string[] Capabilities { get; set; } = [];
    public PluginDependencyManifest[] Dependencies { get; set; } = [];
    public PluginHostCompatibilityManifest HostCompatibility { get; set; } = new();
    public PluginIsolationManifest Isolation { get; set; } = new();
    public string ManifestPath { get; set; } = string.Empty;
}

public sealed class PluginDependencyManifest
{
    public string PluginName { get; set; } = string.Empty;
    public bool Required { get; set; } = true;
    public string MinVersion { get; set; } = string.Empty;
    public string MaxVersionExclusive { get; set; } = string.Empty;
    public string[] RequiredCapabilities { get; set; } = [];
}

public sealed class PluginHostCompatibilityManifest
{
    public string MinVersion { get; set; } = string.Empty;
    public string MaxVersionExclusive { get; set; } = string.Empty;
}

public sealed class PluginIsolationManifest
{
    public string Mode { get; set; } = "inproc";
    public string Tier { get; set; } = "standard";
    public PluginRestartPolicyManifest RestartPolicy { get; set; } = new();
}

public sealed class PluginRestartPolicyManifest
{
    public bool Enabled { get; set; } = true;
    public int MaxRestarts { get; set; } = 2;
    public int WindowSeconds { get; set; } = 60;
    public int BaseDelayMs { get; set; } = 250;
    public int MaxDelayMs { get; set; } = 3000;
}

internal static class PluginContractManifestLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static PluginContractManifest LoadForPlugin(string assemblyPath, IPlugin plugin)
    {
        if (plugin == null)
            throw new PluginContractException("Plugin instance is null.");
        if (string.IsNullOrWhiteSpace(assemblyPath))
            throw new PluginContractException($"Plugin [{plugin.Name}] assembly path is empty.");

        var manifestPath = ResolveManifestPath(assemblyPath, plugin.Name);
        if (string.IsNullOrWhiteSpace(manifestPath) || !File.Exists(manifestPath))
            throw new PluginContractException($"Plugin [{plugin.Name}] missing manifest file next to assembly [{Path.GetFileName(assemblyPath)}].");

        var json = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<PluginContractManifest>(json, JsonOptions);
        if (manifest == null)
            throw new PluginContractException($"Plugin [{plugin.Name}] manifest could not be parsed.");

        manifest.ManifestPath = manifestPath;
        manifest.PluginName = manifest.PluginName?.Trim() ?? string.Empty;
        manifest.PluginVersion = manifest.PluginVersion?.Trim() ?? string.Empty;
        manifest.Capabilities = (manifest.Capabilities ?? []).Where(capability => !string.IsNullOrWhiteSpace(capability)).Select(capability => capability.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        manifest.Dependencies ??= [];
        foreach (var dependency in manifest.Dependencies)
        {
            dependency.PluginName = dependency.PluginName?.Trim() ?? string.Empty;
            dependency.MinVersion = dependency.MinVersion?.Trim() ?? string.Empty;
            dependency.MaxVersionExclusive = dependency.MaxVersionExclusive?.Trim() ?? string.Empty;
            dependency.RequiredCapabilities = (dependency.RequiredCapabilities ?? [])
                .Where(capability => !string.IsNullOrWhiteSpace(capability))
                .Select(capability => capability.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        manifest.HostCompatibility ??= new PluginHostCompatibilityManifest();
        manifest.HostCompatibility.MinVersion = manifest.HostCompatibility.MinVersion?.Trim() ?? string.Empty;
        manifest.HostCompatibility.MaxVersionExclusive = manifest.HostCompatibility.MaxVersionExclusive?.Trim() ?? string.Empty;

        manifest.Isolation ??= new PluginIsolationManifest();
        manifest.Isolation.Mode = string.IsNullOrWhiteSpace(manifest.Isolation.Mode)
            ? "inproc"
            : manifest.Isolation.Mode.Trim().ToLowerInvariant();
        manifest.Isolation.Tier = string.IsNullOrWhiteSpace(manifest.Isolation.Tier)
            ? "standard"
            : manifest.Isolation.Tier.Trim().ToLowerInvariant();
        manifest.Isolation.RestartPolicy ??= new PluginRestartPolicyManifest();

        if (string.IsNullOrWhiteSpace(manifest.PluginName))
            throw new PluginContractException($"Plugin [{plugin.Name}] manifest does not define pluginName.");

        if (!manifest.PluginName.Equals(plugin.Name, StringComparison.OrdinalIgnoreCase))
            throw new PluginContractException($"Plugin [{plugin.Name}] manifest pluginName mismatch [{manifest.PluginName}].");

        if (!string.IsNullOrWhiteSpace(manifest.PluginVersion) &&
            !manifest.PluginVersion.Equals(plugin.Version ?? string.Empty, StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginContractException(
                $"Plugin [{plugin.Name}] manifest version [{manifest.PluginVersion}] does not match runtime version [{plugin.Version}]."
            );
        }

        if (!manifest.Isolation.Mode.Equals("inproc", StringComparison.OrdinalIgnoreCase) &&
            !manifest.Isolation.Mode.Equals("outproc", StringComparison.OrdinalIgnoreCase))
        {
            throw new PluginContractException(
                $"Plugin [{plugin.Name}] has unsupported isolation mode [{manifest.Isolation.Mode}]. Expected inproc or outproc."
            );
        }

        return manifest;
    }

    private static string ResolveManifestPath(string assemblyPath, string pluginName)
    {
        var directory = Path.GetDirectoryName(assemblyPath) ?? string.Empty;
        var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);
        var candidates = new List<string>
        {
            Path.ChangeExtension(assemblyPath, ".manifest.json"),
            Path.Combine(directory, $"{assemblyName}.manifest.json"),
            Path.Combine(directory, "plugin.manifest.json")
        };

        if (!string.IsNullOrWhiteSpace(pluginName))
            candidates.Add(Path.Combine(directory, $"{pluginName}.manifest.json"));

        return candidates.FirstOrDefault(File.Exists) ?? string.Empty;
    }
}

internal static class PluginVersionRange
{
    internal static bool Satisfies(string version, string minVersion, string maxVersionExclusive)
    {
        if (!TryParse(version, out var parsedVersion))
            return false;

        if (!string.IsNullOrWhiteSpace(minVersion) && TryParse(minVersion, out var parsedMin) && parsedVersion < parsedMin)
            return false;

        if (!string.IsNullOrWhiteSpace(maxVersionExclusive) && TryParse(maxVersionExclusive, out var parsedMax) && parsedVersion >= parsedMax)
            return false;

        return true;
    }

    internal static bool TryParse(string version, out Version parsedVersion)
    {
        parsedVersion = null;
        if (string.IsNullOrWhiteSpace(version))
            return false;

        var value = version.Trim();
        if (Version.TryParse(value, out parsedVersion))
            return true;

        var normalized = value;
        if (!normalized.Contains('.'))
            normalized = $"{normalized}.0";

        return Version.TryParse(normalized, out parsedVersion);
    }
}
