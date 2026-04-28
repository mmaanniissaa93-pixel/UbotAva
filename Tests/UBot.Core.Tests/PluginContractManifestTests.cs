using System;
using System.IO;
using System.Windows.Forms;
using UBot.Core.Plugins;
using Xunit;

namespace UBot.Core.Tests;

public class PluginContractManifestTests
{
    [Fact]
    public void LoadForPlugin_ShouldThrow_WhenManifestIsMissing()
    {
        using var scope = TempPluginScope.Create();

        var ex = Assert.Throws<PluginContractException>(() =>
            PluginContractManifestLoader.LoadForPlugin(scope.AssemblyPath, new TestPlugin("UBot.Test", "1.0.0"))
        );

        Assert.Contains("missing manifest", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadForPlugin_ShouldThrow_WhenPluginNameDoesNotMatchRuntimeName()
    {
        using var scope = TempPluginScope.Create();
        scope.WriteManifest("""
        {
          "pluginName": "UBot.Other",
          "pluginVersion": "1.0.0",
          "isolation": { "mode": "inproc" }
        }
        """);

        var ex = Assert.Throws<PluginContractException>(() =>
            PluginContractManifestLoader.LoadForPlugin(scope.AssemblyPath, new TestPlugin("UBot.Test", "1.0.0"))
        );

        Assert.Contains("pluginName mismatch", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadForPlugin_ShouldThrow_WhenVersionDoesNotMatchRuntimeVersion()
    {
        using var scope = TempPluginScope.Create();
        scope.WriteManifest("""
        {
          "pluginName": "UBot.Test",
          "pluginVersion": "2.0.0",
          "isolation": { "mode": "inproc" }
        }
        """);

        var ex = Assert.Throws<PluginContractException>(() =>
            PluginContractManifestLoader.LoadForPlugin(scope.AssemblyPath, new TestPlugin("UBot.Test", "1.0.0"))
        );

        Assert.Contains("does not match runtime version", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadForPlugin_ShouldThrow_WhenIsolationModeIsUnsupported()
    {
        using var scope = TempPluginScope.Create();
        scope.WriteManifest("""
        {
          "pluginName": "UBot.Test",
          "pluginVersion": "1.0.0",
          "isolation": { "mode": "sidecar" }
        }
        """);

        var ex = Assert.Throws<PluginContractException>(() =>
            PluginContractManifestLoader.LoadForPlugin(scope.AssemblyPath, new TestPlugin("UBot.Test", "1.0.0"))
        );

        Assert.Contains("unsupported isolation mode", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LoadForPlugin_ShouldNormalizeCapabilitiesDependenciesAndIsolationMode()
    {
        using var scope = TempPluginScope.Create();
        scope.WriteManifest("""
        {
          "pluginName": " UBot.Test ",
          "pluginVersion": " 1.0.0 ",
          "capabilities": [ " packets ", "PACKETS", "", "ui" ],
          "dependencies": [
            {
              "pluginName": " UBot.Dependency ",
              "minVersion": " 1.2 ",
              "maxVersionExclusive": " 2.0 ",
              "requiredCapabilities": [ " map ", "MAP", "" ]
            }
          ],
          "isolation": { "mode": " OUTPROC ", "tier": "" }
        }
        """);

        var manifest = PluginContractManifestLoader.LoadForPlugin(
            scope.AssemblyPath,
            new TestPlugin("UBot.Test", "1.0.0")
        );

        Assert.Equal("UBot.Test", manifest.PluginName);
        Assert.Equal("1.0.0", manifest.PluginVersion);
        Assert.Equal(new[] { "packets", "ui" }, manifest.Capabilities);
        Assert.Equal("UBot.Dependency", manifest.Dependencies[0].PluginName);
        Assert.Equal("1.2", manifest.Dependencies[0].MinVersion);
        Assert.Equal("2.0", manifest.Dependencies[0].MaxVersionExclusive);
        Assert.Equal(new[] { "map" }, manifest.Dependencies[0].RequiredCapabilities);
        Assert.Equal("outproc", manifest.Isolation.Mode);
        Assert.Equal("standard", manifest.Isolation.Tier);
    }

    [Theory]
    [InlineData("1", "1.0", "2.0", true)]
    [InlineData("1.5.0", "1.0", "2.0", true)]
    [InlineData("2.0", "1.0", "2.0", false)]
    [InlineData("0.9", "1.0", "2.0", false)]
    [InlineData("bad", "1.0", "2.0", false)]
    public void VersionRange_ShouldEvaluateInclusiveMinAndExclusiveMax(
        string version,
        string minVersion,
        string maxVersionExclusive,
        bool expected
    )
    {
        Assert.Equal(expected, PluginVersionRange.Satisfies(version, minVersion, maxVersionExclusive));
    }

    private sealed class TempPluginScope : IDisposable
    {
        private readonly string _directory;

        private TempPluginScope(string directory)
        {
            _directory = directory;
            AssemblyPath = Path.Combine(directory, "UBot.Test.dll");
            ManifestPath = Path.Combine(directory, "UBot.Test.manifest.json");
        }

        public string AssemblyPath { get; }
        private string ManifestPath { get; }

        public static TempPluginScope Create()
        {
            var directory = Path.Combine(Path.GetTempPath(), "UBot.Core.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            return new TempPluginScope(directory);
        }

        public void WriteManifest(string json)
        {
            File.WriteAllText(ManifestPath, json);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }
    }

    private sealed class TestPlugin : IPlugin
    {
        public TestPlugin(string name, string version)
        {
            Name = name;
            Version = version;
        }

        public string Author => "Test";
        public string Description => "Test";
        public string Name { get; }
        public string Title => Name;
        public string Version { get; }
        public bool Enabled { get; set; }
        public Control View => null;
        public bool DisplayAsTab => false;
        public int Index => 0;
        public bool RequireIngame => false;

        public void Initialize()
        {
        }

        public void Translate()
        {
        }

        public void Enable()
        {
        }

        public void Disable()
        {
        }

        public void OnLoadCharacter()
        {
        }
    }
}
