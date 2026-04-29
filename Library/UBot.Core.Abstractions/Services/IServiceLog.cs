using System;

namespace UBot.Core.Abstractions.Services;

public interface IServiceLog
{
    void Debug(string message);
    void Notify(string message);
    void Warn(string message);
    void Fatal(Exception exception);
}
