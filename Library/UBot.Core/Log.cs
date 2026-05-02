using System;
using System.IO;
using UBot.Core.Components;
using UBot.Core.Event;

namespace UBot.Core;

public class Log
{
    /// <summary>
    ///     Replaces the format item in a specified string with the string
    ///     representation of a corresponding object in a specified array
    /// </summary>
    /// <param name="logLevel">The message level</param>
    /// <param name="format">The format</param>
    /// <param name="args">The args</param>
    public static void AppendFormat(LogLevel logLevel, string format, params object[] args)
    {
        UBot.Core.RuntimeAccess.Events.FireEvent("OnAddLog", string.Format(format, args), logLevel);
    }

    /// <summary>
    ///     Appends the given message to the log using the provided log level.
    /// </summary>
    /// <param name="logLevel"></param>
    /// <param name="message"></param>
    public static void Append(LogLevel logLevel, string message)
    {
        UBot.Core.RuntimeAccess.Events.FireEvent("OnAddLog", message, logLevel);
    }

    /// <summary>
    ///     Appends the specified message.
    /// </summary>
    /// <param name="obj">The message.</param>
    /// <param name="level">The level.</param>
    public static void Notify(object obj)
    {
        UBot.Core.RuntimeAccess.Events.FireEvent("OnAddLog", obj.ToString(), LogLevel.Notify);
    }

    /// <summary>
    ///     Appends the specified language key.
    /// </summary>
    /// <param name="obj">The message.</param>
    /// <param name="level">The level.</param>
    public static void NotifyLang(string key, params object[] args)
    {
        UBot.Core.RuntimeAccess.Events.FireEvent("OnAddLog", LanguageManager.GetLang(key, args), LogLevel.Notify);
    }

    /// <summary>
    ///     Append specified debug message
    /// </summary>
    /// <param name="obj">The message</param>
    public static void Debug(object obj)
    {
        UBot.Core.RuntimeAccess.Events.FireEvent("OnAddLog", obj.ToString(), LogLevel.Debug);
    }

    /// <summary>
    ///     Append specified Warning message
    /// </summary>
    /// <param name="obj">The message</param>
    public static void Warn(object obj)
    {
        UBot.Core.RuntimeAccess.Events.FireEvent("OnAddLog", obj.ToString(), LogLevel.Warning);
    }

    /// <summary>
    ///     Append specified Warning message with exception details
    /// </summary>
    public static void Warn(string message, Exception ex)
    {
        var fullMessage = FormatException(message, ex, includeStackTrace: false);
        UBot.Core.RuntimeAccess.Events.FireEvent("OnAddLog", fullMessage, LogLevel.Warning);
    }

    /// <summary>
    ///     Appends the specified language key.
    /// </summary>
    /// <param name="obj">The message.</param>
    /// <param name="level">The level.</param>
    public static void WarnLang(string key, params object[] args)
    {
        UBot.Core.RuntimeAccess.Events.FireEvent("OnAddLog", LanguageManager.GetLang(key, args), LogLevel.Warning);
    }

    /// <summary>
    ///     Append specified Error message
    /// </summary>
    /// <param name="obj">The message</param>
    public static void Error(object obj)
    {
        UBot.Core.RuntimeAccess.Events.FireEvent("OnAddLog", obj.ToString(), LogLevel.Error);
    }

    /// <summary>
    ///     Append specified Error message with exception details (includes stack trace)
    /// </summary>
    public static void Error(string message, Exception ex)
    {
        var fullMessage = FormatException(message, ex, includeStackTrace: true);
        UBot.Core.RuntimeAccess.Events.FireEvent("OnAddLog", fullMessage, LogLevel.Error);
    }

    /// <summary>
    ///     Change status message on ui
    /// </summary>
    /// <param name="obj">The message</param>
    public static void Status(object obj)
    {
        UBot.Core.RuntimeAccess.Events.FireEvent("OnChangeStatusText", obj.ToString());
    }

    /// <summary>
    ///     Change status message on ui by language key.
    /// </summary>
    /// <param name="obj">The message.</param>
    /// <param name="level">The level.</param>
    public static void StatusLang(string key, params object[] args)
    {
        UBot.Core.RuntimeAccess.Events.FireEvent("OnChangeStatusText", LanguageManager.GetLang(key, args));
    }

    /// <summary>
    ///     Append specified fatal message
    /// </summary>
    /// <param name="obj">The message</param>
    public static void Fatal(Exception obj)
    {
        Fatal(obj.Message ?? "Unknown error", obj);
    }

    /// <summary>
    ///     Append specified fatal message with exception (includes stack trace + file logging)
    /// </summary>
    public static void Fatal(string message, Exception ex)
    {
        var fullMessage = FormatException(message, ex, includeStackTrace: true);
        UBot.Core.RuntimeAccess.Events.FireEvent("OnAddLog", fullMessage, LogLevel.Fatal);

        var filePath = Path.Combine(UBot.Core.RuntimeAccess.Core.BasePath, "Data", "Logs", "Exceptions", $"{DateTime.Now:dd-MM-yyyy_HH-mm-ss}.txt");
        var directory = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        using (var stream = File.AppendText(filePath))
        {
            stream.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}");
            stream.WriteLine(ex.ToString());
            stream.WriteLine();
        }
    }

    private static string FormatException(string message, Exception ex, bool includeStackTrace)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(message);

        sb.Append("Type: ").AppendLine(ex.GetType().FullName);
        sb.Append("Message: ").AppendLine(ex.Message);

        if (includeStackTrace && !string.IsNullOrWhiteSpace(ex.StackTrace))
        {
            sb.AppendLine("StackTrace:");
            sb.AppendLine(ex.StackTrace);
        }

        var inner = ex.InnerException;
        while (inner != null)
        {
            sb.AppendLine("Inner:");
            sb.Append("Type: ").AppendLine(inner.GetType().FullName);
            sb.Append("Message: ").AppendLine(inner.Message);
            if (includeStackTrace && !string.IsNullOrWhiteSpace(inner.StackTrace))
            {
                sb.AppendLine("StackTrace:");
                sb.AppendLine(inner.StackTrace);
            }
            inner = inner.InnerException;
        }

        return sb.ToString();
    }
}
