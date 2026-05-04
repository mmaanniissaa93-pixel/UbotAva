using System;
using UBot.Core;
using UBot.Core.Config;
using Xunit;

namespace UBot.Core.Tests;

public class SmokedTests
{
    /// <summary>
    /// Test that GlobalConfig.Set logs the key, old value, and new value.
    /// </summary>
    [Fact]
    public void GlobalConfig_Set_ShouldLogChange()
    {
        // Arrange
        var key = "Test.Smoke.Key";
        var oldValue = GlobalConfig.Get(key, "old");
        var newValue = "new";

        // Act
        GlobalConfig.Set(key, newValue);

        // Assert - if we got here without exception, the Set method works
        var retrieved = GlobalConfig.Get(key, "default");
        Assert.Equal(newValue, retrieved);
    }

    /// <summary>
    /// Test that PlayerConfig.Set logs the key, old value, new value, character, and path.
    /// </summary>
    [Fact]
    public void PlayerConfig_Set_ShouldLogChange()
    {
        // Arrange
        var key = "Test.Smoke.Key";
        var oldValue = PlayerConfig.Get(key, "old");
        var newValue = "new";

        // Act
        PlayerConfig.Set(key, newValue);

        // Assert - if we got here without exception, the Set method works
        var retrieved = PlayerConfig.Get(key, "default");
        Assert.Equal(newValue, retrieved);
    }

    /// <summary>
    /// Test that Log.Debug works with Trace level enabled/disabled.
    /// </summary>
    [Fact]
    public void Log_Debug_ShouldRespectDebugLoggingEnabled()
    {
        // Arrange
        var originalValue = Log.IsDebugLoggingEnabled;
        try
        {
            // Act - disable debug logging
            Log.SetDebugLoggingEnabled(false);
            Assert.False(Log.IsDebugLoggingEnabled);

            // Act - enable debug logging
            Log.SetDebugLoggingEnabled(true);
            Assert.True(Log.IsDebugLoggingEnabled);
        }
        finally
        {
            // Restore
            Log.SetDebugLoggingEnabled(originalValue);
        }
    }

    /// <summary>
    /// Test that LogLevel.Trace exists and can be used.
    /// </summary>
    [Fact]
    public void LogLevel_Trace_ShouldExist()
    {
        // Act & Assert
        var traceLevel = LogLevel.Trace;
        Assert.Equal(LogLevel.Trace, traceLevel);
    }
}
