using System;

namespace UBot.Protocol;

/// <summary>
/// Thin logging proxy for UBot.Protocol and its sub-namespaces.
/// Delegates to ProtocolRuntime.LogService which is wired up by UBot.Core.Bootstrap.
/// </summary>
internal static class Log
{
    public static void Debug(object msg) =>
        ProtocolRuntime.LogService?.Debug(msg?.ToString() ?? string.Empty);

    public static void Notify(object msg) =>
        ProtocolRuntime.LogService?.Notify(msg?.ToString() ?? string.Empty);

    public static void Warn(object msg) =>
        ProtocolRuntime.LogService?.Warn(msg?.ToString() ?? string.Empty);

    public static void Error(object msg) =>
        ProtocolRuntime.LogService?.Error(msg?.ToString() ?? string.Empty);

    public static void Error(string message, Exception ex) =>
        ProtocolRuntime.LogService?.Error(message + " | " + ex?.Message);
}
