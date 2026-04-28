using System;
using System.Globalization;
using System.IO;
using System.Threading;
using UBot.Core;
using Xunit;

namespace UBot.Core.Tests;

public class ConfigTests
{
    [Fact]
    public void Config_ShouldRoundTripValueContainingBrace()
    {
        using var scope = TempConfigScope.Create();
        var config = new Config(scope.Path);

        config.Set("UBot.Sounds.Path", @"C:\sounds\boss{name}.wav}");
        config.Save();

        var reloaded = new Config(scope.Path);

        Assert.Equal(@"C:\sounds\boss{name}.wav}", reloaded.Get("UBot.Sounds.Path", string.Empty));
    }

    [Fact]
    public void Config_ShouldUseInvariantCulture_ForScalarAndArrayValues()
    {
        using var scope = TempConfigScope.Create();
        var previousCulture = Thread.CurrentThread.CurrentCulture;
        var previousUICulture = Thread.CurrentThread.CurrentUICulture;

        try
        {
            var turkishCulture = CultureInfo.GetCultureInfo("tr-TR");
            Thread.CurrentThread.CurrentCulture = turkishCulture;
            Thread.CurrentThread.CurrentUICulture = turkishCulture;

            var config = new Config(scope.Path);
            config.Set("UBot.Test.Double", 1.5d);
            config.SetArray("UBot.Test.Array", new[] { 1.5d, 2.25d });
            config.Save();

            var reloaded = new Config(scope.Path);

            Assert.Equal(1.5d, reloaded.Get("UBot.Test.Double", 0d));
            Assert.Equal(new[] { 1.5d, 2.25d }, reloaded.GetArray<double>("UBot.Test.Array"));
            Assert.Contains("UBot.Test.Double{1.5}", File.ReadAllLines(scope.Path));
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previousCulture;
            Thread.CurrentThread.CurrentUICulture = previousUICulture;
        }
    }

    [Fact]
    public void Config_ShouldParseEnumsCaseInsensitively()
    {
        using var scope = TempConfigScope.Create("UBot.Test.Mode{agent}\r\nUBot.Test.Modes{client,SERVER}");
        var config = new Config(scope.Path);

        Assert.Equal(TestPacketMode.Agent, config.GetEnum("UBot.Test.Mode", TestPacketMode.Client));
        Assert.Equal(
            new[] { TestPacketMode.Client, TestPacketMode.Server },
            config.GetEnums<TestPacketMode>("UBot.Test.Modes")
        );
    }

    [Fact]
    public void Config_ShouldIgnoreMalformedLines()
    {
        using var scope = TempConfigScope.Create("broken\r\n{missingkey}\r\nUBot.Valid{ok}\r\nUBot.Invalid{missing-end");
        var config = new Config(scope.Path);

        Assert.False(config.Exists("broken"));
        Assert.False(config.Exists(string.Empty));
        Assert.Equal("ok", config.Get("UBot.Valid", string.Empty));
        Assert.Equal("fallback", config.Get("UBot.Invalid", "fallback"));
    }

    private enum TestPacketMode
    {
        Client,
        Server,
        Agent
    }

    private sealed class TempConfigScope : IDisposable
    {
        private readonly string _directory;

        private TempConfigScope(string directory, string path)
        {
            _directory = directory;
            Path = path;
        }

        public string Path { get; }

        public static TempConfigScope Create(string content = "")
        {
            var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "UBot.Core.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var path = System.IO.Path.Combine(directory, "config.rs");
            File.WriteAllText(path, content);
            return new TempConfigScope(directory, path);
        }

        public void Dispose()
        {
            if (Directory.Exists(_directory))
                Directory.Delete(_directory, true);
        }
    }
}
